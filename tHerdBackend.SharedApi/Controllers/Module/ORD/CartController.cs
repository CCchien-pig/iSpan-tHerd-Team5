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
        private readonly ICurrentUser _currentUser;
        private readonly UserManager<ApplicationUser>? _userManager;
        private readonly HttpClient _httpClient;
        private readonly ICurrentUserService _current;

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

        /// <summary>
        /// 取得當前使用者資訊
        /// </summary>
        private async Task<(int? UserNumberId, string SessionId)> GetUserInfoAsync()
        {
            int? userNumberId = null;
            string sessionId = HttpContext.Session.Id; // 從 Session 取得

            // 檢查是否為登入會員
            if (_currentUser.IsAuthenticated && _userManager != null)
            {
                var user = await _userManager.Users.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == _currentUser.Id);

                if (user != null)
                {
                    userNumberId = user.UserNumberId;
                }
            }

            return (userNumberId, sessionId);
        }

        /// <summary>
        /// 加入購物車（使用 PROD 的 Repository）
        /// </summary>
        [HttpPost("add")]
        [AllowAnonymous]
        public async Task<IActionResult> AddToCart([FromBody] ProdDto dto)
        {
            try
            {
                var cartId = await _cartRepository.AddShoppingCartAsync(dto);

                var (userNumberId, sessionId) = await GetUserInfoAsync();

                return Ok(new
                {
                    success = true,
                    message = "加入購物車成功",
                    data = new
                    {
                        cartId,
                        sessionId,
                        isAuthenticated = userNumberId.HasValue
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 取得購物車（含商品驗證）
        /// </summary>
        [HttpGet("get")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCart([FromQuery] string? sessionId)
        {
            try
            {
                var (userNumberId, currentSessionId) = await GetUserInfoAsync();

                // 優先使用會員 ID，其次使用 SessionId
                var cart = await _context.OrdShoppingCarts
                    .Include(c => c.OrdShoppingCartItems)
                    .ThenInclude(i => i.Sku)
                    .ThenInclude(s => s.Product)
                    .Where(c => (userNumberId.HasValue && c.UserNumberId == userNumberId) ||
                               (!userNumberId.HasValue && c.SessionId == (sessionId ?? currentSessionId)))
                    .FirstOrDefaultAsync();

                if (cart == null)
                {
                    return Ok(new { success = true, data = new { items = new List<object>(), total = 0 } });
                }

                var items = cart.OrdShoppingCartItems.Select(i => new
                {
                    cartItemId = i.CartItemId,
                    productId = i.ProductId,
                    skuId = i.SkuId,
                    productName = i.Sku?.Product?.ProductName ?? "商品已下架",
                    optionName = i.Sku?.SpecCode ?? i.Sku?.SkuCode ?? "",
                    unitPrice = i.UnitPrice,
                    qty = i.Qty,
                    subtotal = i.UnitPrice * i.Qty,
                    stockQty = i.Sku?.StockQty ?? 0,

                    isAvailable = i.Sku != null &&
                                  i.Sku.Product != null &&
                                  i.Sku.Product.IsPublished &&
                                  i.Sku.IsActive,
                    isInStock = (i.Sku?.StockQty ?? 0) >= i.Qty,
                    warningMessage = GetWarningMessage(i)
                }).ToList();

                var total = items.Where(i => i.isAvailable && i.isInStock).Sum(i => i.subtotal);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total,
                        hasInvalidItems = items.Any(i => !i.isAvailable || !i.isInStock),
                        isAuthenticated = userNumberId.HasValue
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        private string? GetWarningMessage(OrdShoppingCartItem item)
        {
            if (item.Sku == null || item.Sku.Product == null)
                return "❌ 商品已下架";

            if (!item.Sku.Product.IsPublished)
                return "❌ 商品已下架";

            if (!item.Sku.IsActive)
                return "❌ 此規格已停售";

            if (item.Sku.StockQty < item.Qty)
                return $"⚠️ 庫存不足（剩餘 {item.Sku.StockQty}）";

            return null;
        }

        /// <summary>
        /// 更新數量
        /// </summary>
        [HttpPut("update/{cartItemId}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] UpdateQtyDto dto)
        {
            try
            {
                var item = await _context.OrdShoppingCartItems.FindAsync(cartItemId);
                if (item == null)
                    return Ok(new { success = false, message = "找不到商品" });

                item.Qty = dto.Qty;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "更新成功" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 刪除項目
        /// </summary>
        [HttpDelete("remove/{cartItemId}")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            try
            {
                var item = await _context.OrdShoppingCartItems.FindAsync(cartItemId);
                if (item == null)
                    return Ok(new { success = false, message = "找不到商品" });

                _context.OrdShoppingCartItems.Remove(item);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "刪除成功" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 計算金額（含運費、優惠券）
        /// </summary>
        [HttpPost("calculate")]
        [AllowAnonymous]
        public async Task<IActionResult> CalculateOrder([FromBody] CalculateOrderRequest request)
        {
            try
            {
                var (userNumberId, _) = await GetUserInfoAsync();

                var result = await _calculationService.CalculateAsync(
                    userNumberId,
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

        /// <summary>
        /// 傳送地址給 SUP
        /// </summary>
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

        /// <summary>
        /// 結帳（移除測試用戶）
        /// </summary>
        [HttpPost("checkout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            // ⚠️ 結帳必須登入
            var userNumberId = _current.GetRequiredUserNumberId();
            bool userNumberIdBool = userNumberId>0 ? true : false;

            if (userNumberIdBool)
            {
                return Ok(new { success = false, message = "請先登入會員" });
            }

            if (request?.CartItems == null || !request.CartItems.Any())
                return Ok(new { success = false, message = "購物車是空的" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. 驗證商品和庫存
                var skuIds = request.CartItems.Select(x => x.SkuId).ToList();
                var skus = await _context.ProdProductSkus
                    .Include(s => s.Product)
                    .Where(s => skuIds.Contains(s.SkuId))
                    .ToDictionaryAsync(s => s.SkuId);

                var errorList = new List<string>();

                foreach (var item in request.CartItems)
                {
                    if (!skus.TryGetValue(item.SkuId, out var sku))
                    {
                        errorList.Add($"商品不存在: {item.ProductName}");
                        continue;
                    }

                    if (sku.Product == null || !sku.Product.IsPublished)
                    {
                        errorList.Add($"商品已下架: {item.ProductName}");
                        continue;
                    }

                    if (!sku.IsActive)
                    {
                        errorList.Add($"此規格已停售: {item.ProductName}");
                        continue;
                    }

                    if (sku.StockQty < item.Quantity)
                    {
                        errorList.Add($"庫存不足: {item.ProductName}（剩餘 {sku.StockQty}）");
                        continue;
                    }
                }

                if (errorList.Any())
                {
                    await transaction.RollbackAsync();
                    return Ok(new { success = false, errors = errorList });
                }

                // 2. 計算金額
                var items = request.CartItems.Select(x => new OrderItemDto
                {
                    SkuId = x.SkuId,
                    SalePrice = x.SalePrice,
                    Quantity = x.Quantity
                }).ToList();

                var calculation = await _calculationService.CalculateAsync(userNumberId, items, request.CouponCode);

                // 3. 產生訂單編號
                string orderNo = DateTime.Now.ToString("yyyyMMdd") +
                                DateTime.Now.ToString("HHmmssfff").Substring(DateTime.Now.ToString("HHmmssfff").Length - 7);

                // 4. 建立訂單（使用真實會員 ID）
                var paymentConfigId = await _context.OrdPaymentConfigs
                    .Select(p => p.PaymentConfigId)
                    .FirstOrDefaultAsync();

                var order = new OrdOrder
                {
                    OrderNo = orderNo,
                    UserNumberId = userNumberId, // ✅ 使用真實會員 ID
                    OrderStatusId = "pending",
                    PaymentStatus = "pending",
                    ShippingStatusId = "unshipped",
                    Subtotal = calculation.Subtotal,
                    DiscountTotal = calculation.Discount,
                    ShippingFee = calculation.ShippingFee,
                    PaymentConfigId = paymentConfigId,
                    ReceiverName = request.ReceiverName,
                    ReceiverPhone = request.ReceiverPhone,
                    ReceiverAddress = request.ReceiverAddress,
                    HasShippingLabel = false,
                    IsVisibleToMember = true,
                    CreatedDate = DateTime.Now
                };

                _context.OrdOrders.Add(order);
                await _context.SaveChangesAsync();

                // 5. 建立訂單明細
                foreach (var item in request.CartItems)
                {
                    _context.OrdOrderItems.Add(new OrdOrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        SkuId = item.SkuId,
                        Qty = item.Quantity,
                        UnitPrice = item.SalePrice
                    });
                }

                // 6. 優惠券紀錄
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

                // 7. 清空購物車（會員）
                await _context.Database.ExecuteSqlRawAsync(
                    @"DELETE ci FROM ORD_ShoppingCartItem ci
                      INNER JOIN ORD_ShoppingCart c ON ci.CartId = c.CartId
                      WHERE c.UserNumberId = {0}",
                    userNumberId
                );

                await transaction.CommitAsync();

                // 8. 產生綠界付款表單
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
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Ok(new { success = false, message = ex.Message });
            }
        }
    }
}