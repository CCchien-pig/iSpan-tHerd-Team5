using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.DTOs.PROD.ord;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.Models;
using tHerdBackend.Infra.Models;
using tHerdBackend.Services.Common;
using tHerdBackend.Services.ORD;

// === 新增別名 (防止 AddToCartDto 命名衝突) ===
using OrdDto = tHerdBackend.Core.DTOs.ORD.AddToCartDto;
using ProdDto = tHerdBackend.Core.DTOs.PROD.ord.AddToCartDto;

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
	[ApiController]
	[Route("api/ord/cart")]
	public class CartController : ControllerBase
	{
		private readonly tHerdDBContext _context;
		private readonly IECPayService _ecpayService;
		private readonly IOrderCalculationService _calculationService;
		private readonly IShoppingCartRepository _cartRepository;

		// 這兩個保留，因你的專案已有使用
		private readonly ICurrentUser _currentUser;
		private readonly ICurrentUserService _current;

		private readonly UserManager<ApplicationUser>? _userManager;
		private readonly HttpClient _httpClient;

		public CartController(
			tHerdDBContext context,
			IECPayService ecpayService,
			IOrderCalculationService calculationService,
			IShoppingCartRepository cartRepository,
			ICurrentUser currentUser,
			UserManager<ApplicationUser>? userManager,
			IHttpClientFactory httpClientFactory,
			ICurrentUserService current)
		{
			_context = context;
			_ecpayService = ecpayService;
			_calculationService = calculationService;
			_cartRepository = cartRepository;
			_currentUser = currentUser;
			_userManager = userManager;
			_httpClient = httpClientFactory.CreateClient();
			_current = current;
		}

		// ---------------------------
		// ✅ 核心：統一取得登入/Session 資訊
		// ---------------------------
		private sealed class AuthContext
		{
			public bool IsAuthenticated { get; init; }
			public string? UserId { get; init; }
			public int? UserNumberId { get; init; }
			public string SessionId { get; init; } = "";
		}

		private async Task<(string Name, string Phone, string Address)?> GetReceiverFromProfileAsync()
		{
			if (_userManager == null) return null;

			var userId = _userManager.GetUserId(User);
			if (string.IsNullOrEmpty(userId)) return null;

			// 只抓需要的欄位，效能較佳
			var u = await _userManager.Users.AsNoTracking()
				.Where(x => x.Id == userId)
				.Select(x => new
				{
					Name = (x.LastName ?? "") + (x.FirstName ?? ""),
					x.PhoneNumber,
					x.Address
				})
				.FirstOrDefaultAsync();

			if (u == null) return null;

			var name = (u.Name ?? "").Trim();
			var phone = (u.PhoneNumber ?? "").Trim();
			var addr = (u.Address ?? "").Trim();

			return (name, phone, addr);
		}

		private async Task<AuthContext> GetAuthContextAsync()
		{
			// SessionId（即使未登入也會有；確保已啟用 Session 中介軟體）
			var sessionId = HttpContext?.Session?.Id ?? string.Empty;

			// 可能未標 [Authorize] 的 action，但前端仍夾帶 Bearer 時，
			// HttpContext.User 依然會是已驗證狀態。
			var isAuth = User?.Identity?.IsAuthenticated == true;

			string? userId = null;
			int? userNumberId = null;

			if (isAuth && _userManager != null)
			{
				userId = _userManager.GetUserId(User);
				if (!string.IsNullOrEmpty(userId))
				{
					userNumberId = await _userManager.Users.AsNoTracking()
						.Where(u => u.Id == userId)
						.Select(u => (int?)u.UserNumberId)
						.FirstOrDefaultAsync();
				}
			}

			return new AuthContext
			{
				IsAuthenticated = isAuth,
				UserId = userId,
				UserNumberId = userNumberId,
				SessionId = sessionId
			};
		}

        // ------------------------------------------------

        /// <summary>加入購物車（使用 PROD 的 Repository）</summary>
        [HttpPost("add")]
		[AllowAnonymous]
		public async Task<IActionResult> AddToCart([FromBody] ProdDto dto)
		{
			try
			{
				var cartId = await _cartRepository.AddShoppingCartAsync(dto);
				var auth = await GetAuthContextAsync();

				return Ok(new
				{
					success = true,
					message = "加入購物車成功",
					data = new
					{
						cartId,
						sessionId = auth.SessionId,
						isAuthenticated = auth.IsAuthenticated
					}
				});
			}
			catch (Exception ex)
			{
				return Ok(new { success = false, message = ex.Message });
			}
		}

        /// <summary>
        /// 取得購物車（ORD 模組，即時驗證庫存）
        /// </summary>
        [HttpGet("get")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetCart([FromQuery] string? sessionId)
        {
            try
            {
                var auth = await GetAuthContextAsync();
                if (!auth.IsAuthenticated || !auth.UserNumberId.HasValue)
                {
                    return Ok(new { success = false, message = "請先登入會員" });
                }

                int userNumberId = auth.UserNumberId.Value;
                string currentSessionId = sessionId ?? HttpContext.Session.Id;

                // 查詢購物車
                var cart = await _context.OrdShoppingCarts
                    .Include(c => c.OrdShoppingCartItems)
                    .ThenInclude(i => i.Sku)
                    .ThenInclude(s => s.Product)
                    .Where(c => c.UserNumberId == userNumberId || c.SessionId == currentSessionId)
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefaultAsync();

                if (cart == null || cart.OrdShoppingCartItems == null || !cart.OrdShoppingCartItems.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            items = new List<object>(),
                            total = 0,
                            canCheckout = true,
                            invalidCount = 0
                        }
                    });
                }

                // 即時驗證每個商品
                var items = cart.OrdShoppingCartItems.Select(i =>
                {
                    bool isAvailable = true;
                    bool isInStock = true;
                    string? disabledReason = null;

                    if (i.Sku == null || i.Sku.Product == null)
                    {
                        isAvailable = false;
                        disabledReason = "商品已下架";
                    }
                    else if (i.Sku.Product.IsPublished != true)
                    {
                        isAvailable = false;
                        disabledReason = "商品已下架";
                    }
                    else if (i.Sku.IsActive != true)
                    {
                        isAvailable = false;
                        disabledReason = "此規格已停售";
                    }
                    else if (i.Sku.StockQty < i.Qty)
                    {
                        isInStock = false;
                        disabledReason = $"庫存不足，剩餘 {i.Sku.StockQty} 件";
                    }

                    return new
                    {
                        cartItemId = i.CartItemId,
                        productId = i.ProductId,
                        skuId = i.SkuId,
                        productName = i.Sku?.Product?.ProductName ?? "商品已下架",
                        skuName = i.Sku?.SpecCode ?? i.Sku?.SkuCode ?? "",
                        unitPrice = i.Sku?.UnitPrice ?? i.UnitPrice,
                        salePrice = i.Sku?.SalePrice ?? i.UnitPrice,
                        quantity = i.Qty,
                        stockQty = i.Sku?.StockQty ?? 0,
                        isAvailable,
                        isInStock,
                        isValid = isAvailable && isInStock,
                        disabledReason
                    };
                }).ToList();

                var validItems = items.Where(i => i.isValid).ToList();
                var total = validItems.Sum(i => i.salePrice * i.quantity);
                var invalidCount = items.Count(i => !i.isValid);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total,
                        canCheckout = invalidCount == 0,
                        invalidCount
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }


        /// <summary>取得購物車（含商品驗證）</summary>
        //[HttpGet("get")]
        //[AllowAnonymous]
        //public async Task<IActionResult> GetCart([FromQuery] string? sessionId)
        //{
        //	try
        //	{
        //		var auth = await GetAuthContextAsync();
        //		var sid = sessionId ?? auth.SessionId;

        //		var cart = await _context.OrdShoppingCarts
        //			.Include(c => c.OrdShoppingCartItems)
        //				.ThenInclude(i => i.Sku)
        //					.ThenInclude(s => s.Product)
        //			.Where(c =>
        //				(auth.UserNumberId.HasValue && c.UserNumberId == auth.UserNumberId) ||
        //				(!auth.UserNumberId.HasValue && c.SessionId == sid))
        //			.FirstOrDefaultAsync();

        //		if (cart == null)
        //			return Ok(new { success = true, data = new { items = new List<object>(), total = 0, isAuthenticated = auth.IsAuthenticated } });

        //		var items = cart.OrdShoppingCartItems.Select(i => new
        //		{
        //			cartItemId = i.CartItemId,
        //			productId = i.ProductId,
        //			skuId = i.SkuId,
        //			productName = i.Sku?.Product?.ProductName ?? "商品已下架",
        //			optionName = i.Sku?.SpecCode ?? i.Sku?.SkuCode ?? "",
        //			unitPrice = i.UnitPrice,
        //			qty = i.Qty,
        //			subtotal = i.UnitPrice * i.Qty,
        //			stockQty = i.Sku?.StockQty ?? 0,

        //			isAvailable = i.Sku != null &&
        //						  i.Sku.Product != null &&
        //						  i.Sku.Product.IsPublished &&
        //						  i.Sku.IsActive,
        //			isInStock = (i.Sku?.StockQty ?? 0) >= i.Qty,
        //			warningMessage = GetWarningMessage(i)
        //		}).ToList();

        //		var total = items.Where(i => i.isAvailable && i.isInStock).Sum(i => i.subtotal);

        //		return Ok(new
        //		{
        //			success = true,
        //			data = new
        //			{
        //				items,
        //				total,
        //				hasInvalidItems = items.Any(i => !i.isAvailable || !i.isInStock),
        //				isAuthenticated = auth.IsAuthenticated
        //			}
        //		});
        //	}
        //	catch (Exception ex)
        //	{
        //		return Ok(new { success = false, message = ex.Message });
        //	}
        //}

        //private string? GetWarningMessage(OrdShoppingCartItem item)
        //{
        //	if (item.Sku == null || item.Sku.Product == null) return "❌ 商品已下架";
        //	if (!item.Sku.Product.IsPublished) return "❌ 商品已下架";
        //	if (!item.Sku.IsActive) return "❌ 此規格已停售";
        //	if (item.Sku.StockQty < item.Qty) return $"⚠️ 庫存不足（剩餘 {item.Sku.StockQty}）";
        //	return null;
        //}

        /// <summary>更新數量（含所有權保護）</summary>
        [HttpPut("update/{cartItemId}")]
		[AllowAnonymous]
		public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] UpdateQtyDto dto)
		{
			try
			{
				var auth = await GetAuthContextAsync();

				// 確保只能改到自己的 cart item
				var item = await _context.OrdShoppingCartItems
					.Include(ci => ci.Cart) // 若你的模型沒有導覽屬性 Cart，請改為 Join（見下方註解）
					.FirstOrDefaultAsync(ci =>
						ci.CartItemId == cartItemId &&
						(
							(auth.UserNumberId.HasValue && ci.Cart.UserNumberId == auth.UserNumberId) ||
							(!auth.UserNumberId.HasValue && ci.Cart.SessionId == auth.SessionId)
						));

				// 若模型無 Cart 導覽，可用 join：
				// var item = await (from ci in _context.OrdShoppingCartItems
				//                   join c in _context.OrdShoppingCarts on ci.CartId equals c.CartId
				//                   where ci.CartItemId == cartItemId &&
				//                         ((auth.UserNumberId.HasValue && c.UserNumberId == auth.UserNumberId) ||
				//                          (!auth.UserNumberId.HasValue && c.SessionId == auth.SessionId))
				//                   select ci).FirstOrDefaultAsync();

				if (item == null)
					return Ok(new { success = false, message = "找不到商品（或無權存取）" });

				item.Qty = dto.Qty;
				await _context.SaveChangesAsync();

				return Ok(new { success = true, message = "更新成功" });
			}
			catch (Exception ex)
			{
				return Ok(new { success = false, message = ex.Message });
			}
		}

		/// <summary>刪除項目（含所有權保護）</summary>
		[HttpDelete("remove/{cartItemId}")]
		[AllowAnonymous]
		public async Task<IActionResult> RemoveItem(int cartItemId)
		{
			try
			{
				var auth = await GetAuthContextAsync();

				var item = await _context.OrdShoppingCartItems
					.Include(ci => ci.Cart)
					.FirstOrDefaultAsync(ci =>
						ci.CartItemId == cartItemId &&
						(
							(auth.UserNumberId.HasValue && ci.Cart.UserNumberId == auth.UserNumberId) ||
							(!auth.UserNumberId.HasValue && ci.Cart.SessionId == auth.SessionId)
						));

				if (item == null)
					return Ok(new { success = false, message = "找不到商品（或無權存取）" });

				_context.OrdShoppingCartItems.Remove(item);
				await _context.SaveChangesAsync();

				return Ok(new { success = true, message = "刪除成功" });
			}
			catch (Exception ex)
			{
				return Ok(new { success = false, message = ex.Message });
			}
		}

		/// <summary>計算金額（含運費、優惠券）</summary>
		[HttpPost("calculate")]
		[AllowAnonymous]
		public async Task<IActionResult> CalculateOrder([FromBody] CalculateOrderRequest request)
		{
			try
			{
				var auth = await GetAuthContextAsync();

				var result = await _calculationService.CalculateAsync(
					auth.UserNumberId,             // 登入者才會有，匿名則 null
					request.CartItems,
					request.CouponCode
				);

				return Ok(new { success = true, data = result });
			}
			catch (Exception ex)
			{
				return Ok(new { success = false, message = ex.Message });
			}
		}

		/// <summary>傳送地址給 SUP</summary>
		[HttpPost("send-address-to-sup")]
		[AllowAnonymous]
		public async Task<IActionResult> SendAddressToSUP([FromBody] ShippingAddressDto dto)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("/api/sup/shipping/address", new
				{
					receiverName = dto.ReceiverName,
					receiverPhone = dto.ReceiverPhone,
					receiverAddress = dto.ReceiverAddress
				});

				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<dynamic>();
					return Ok(new { success = true, message = "地址已傳送至物流系統", data = result });
				}

				return Ok(new { success = false, message = "物流系統連線失敗" });
			}
			catch (Exception ex)
			{
				return Ok(new { success = false, message = ex.Message });
			}
		}

		/// <summary>結帳（需登入）</summary>
		[HttpPost("checkout")]
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
		{
			var auth = await GetAuthContextAsync();
			if (!(auth.IsAuthenticated && auth.UserNumberId.GetValueOrDefault() > 0))
				return Ok(new { success = false, message = "請先登入會員" });

			if (request?.CartItems == null || !request.CartItems.Any())
				return Ok(new { success = false, message = "購物車是空的" });

			// 🔹 改為：預設從會員檔帶入三欄；若會員檔缺欄位，才允許用 request 覆蓋（前端可送暫時地址）
			var profile = await GetReceiverFromProfileAsync();
			string receiverName = profile?.Name ?? string.Empty;
			string receiverPhone = profile?.Phone ?? string.Empty;
			string receiverAddr = profile?.Address ?? string.Empty;

			// 若會員檔尚未填齊 → 允許用 request 暫時覆蓋
			if (string.IsNullOrWhiteSpace(receiverName)) receiverName = request?.ReceiverName ?? "";
			if (string.IsNullOrWhiteSpace(receiverPhone)) receiverPhone = request?.ReceiverPhone ?? "";
			if (string.IsNullOrWhiteSpace(receiverAddr)) receiverAddr = request?.ReceiverAddress ?? "";

			if (string.IsNullOrWhiteSpace(receiverName) ||
				string.IsNullOrWhiteSpace(receiverPhone) ||
				string.IsNullOrWhiteSpace(receiverAddr))
			{
				return Ok(new
				{
					success = false,
					message = "收件資料不完整，請先到『我的帳戶』補齊姓名／電話／地址，或本次於結帳請求中暫時提供。"
				});
			}

			using var transaction = await _context.Database.BeginTransactionAsync();

			try
			{
				// 1) 驗 SKU/庫存（原樣）
				var skuIds = request.CartItems.Select(x => x.SkuId).ToList();
				var skus = await _context.ProdProductSkus
					.Include(s => s.Product)
					.Where(s => skuIds.Contains(s.SkuId))
					.ToDictionaryAsync(s => s.SkuId);

				var errorList = new List<string>();
				foreach (var item in request.CartItems)
				{
					if (!skus.TryGetValue(item.SkuId, out var sku)) { errorList.Add($"商品不存在: {item.ProductName}"); continue; }
					if (sku.Product == null || !sku.Product.IsPublished) errorList.Add($"商品已下架: {item.ProductName}");
					else if (!sku.IsActive) errorList.Add($"此規格已停售: {item.ProductName}");
					else if (sku.StockQty < item.Quantity) errorList.Add($"庫存不足: {item.ProductName}（剩餘 {sku.StockQty}）");
				}
				if (errorList.Any())
				{
					await transaction.RollbackAsync();
					return Ok(new { success = false, errors = errorList });
				}

				// 2) 計算金額（原樣）
				var items = request.CartItems.Select(x => new OrderItemDto
				{
					SkuId = x.SkuId,
					SalePrice = x.SalePrice,
					Quantity = x.Quantity
				}).ToList();

				var calculation = await _calculationService.CalculateAsync(auth.UserNumberId, items, request.CouponCode);

				// 3) 取 PaymentConfig（原樣）
				var paymentConfigId = await _context.OrdPaymentConfigs
					.Select(p => (int?)p.PaymentConfigId)
					.FirstOrDefaultAsync();
				if (paymentConfigId is null)
				{
					await transaction.RollbackAsync();
					return Ok(new { success = false, message = "尚未設定金流參數（OrdPaymentConfigs）" });
				}

				// 4) 產生訂單（僅把三欄改成上面算出的 receiver*）
				string orderNo = DateTime.Now.ToString("yyyyMMdd") +
								 DateTime.Now.ToString("HHmmssfff")[^7..];

				var order = new OrdOrder
				{
					OrderNo = orderNo,
					UserNumberId = auth.UserNumberId!.Value,
					OrderStatusId = "pending",
					PaymentStatus = "pending",
					ShippingStatusId = "unshipped",
					Subtotal = calculation.Subtotal,
					DiscountTotal = calculation.Discount,
					ShippingFee = calculation.ShippingFee,
					PaymentConfigId = paymentConfigId.Value,
					ReceiverName = receiverName,
					ReceiverPhone = receiverPhone,
					ReceiverAddress = receiverAddr,
					HasShippingLabel = false,
					IsVisibleToMember = true,
					CreatedDate = DateTime.Now
				};

				_context.OrdOrders.Add(order);
				await _context.SaveChangesAsync();

				// 5) 明細（原樣）
				foreach (var it in request.CartItems)
				{
					_context.OrdOrderItems.Add(new OrdOrderItem
					{
						OrderId = order.OrderId,
						ProductId = it.ProductId,
						SkuId = it.SkuId,
						Qty = it.Quantity,
						UnitPrice = it.SalePrice
					});
				}

				// 6) 優惠（原樣）
				if (!string.IsNullOrEmpty(calculation.AppliedCouponCode))
				{
					_context.OrdOrderAdjustments.Add(new OrdOrderAdjustment
					{
						OrderId = order.OrderId,
						Kind = "coupon",
						Scope = "order",
						Code = calculation.AppliedCouponCode,
						Method = "fixed",
						AdjustmentAmount = -calculation.Discount,
						CreatedDate = DateTime.Now,
						RevisedDate = DateTime.Now
					});
				}

				await _context.SaveChangesAsync();

				// 7) 清空會員購物車（原樣）
				await _context.Database.ExecuteSqlRawAsync(@"
DELETE ci FROM ORD_ShoppingCartItem ci
INNER JOIN ORD_ShoppingCart c ON ci.CartId = c.CartId
WHERE c.UserNumberId = {0}", auth.UserNumberId);

				await transaction.CommitAsync();

				// 8) 綠界表單（原樣）
				var ecpayFormHtml = _ecpayService.CreatePaymentForm(
					orderNo,
					(int)Math.Round(calculation.Total),
					"tHerd商品"
				);

				return Ok(new
				{
					success = true,
					message = "訂單建立成功",
					data = new
					{
						orderId = order.OrderId,
						orderNo,
						subtotal = calculation.Subtotal,
						shippingFee = calculation.ShippingFee,
						discount = calculation.Discount,
						total = calculation.Total
					},
					ecpayFormHtml
				});
			}
			catch (DbUpdateException ex)
			{
				var root = ex.InnerException?.Message ?? ex.Message;
				await transaction.RollbackAsync();
				return Ok(new { success = false, message = "資料庫寫入失敗", detail = root });
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return Ok(new { success = false, message = ex.Message });
			}
		}

	}
}
