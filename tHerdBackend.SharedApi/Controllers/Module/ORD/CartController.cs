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
            var sessionId = HttpContext?.Session?.Id ?? string.Empty;
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

        /// <summary>取得購物車（ORD 模組，即時驗證庫存）</summary>
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

        /// <summary>更新數量（含所有權保護）</summary>
        [HttpPut("update/{cartItemId}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] UpdateQtyDto dto)
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
                    auth.UserNumberId,
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

            var profile = await GetReceiverFromProfileAsync();
            string receiverName = profile?.Name ?? string.Empty;
            string receiverPhone = profile?.Phone ?? string.Empty;
            string receiverAddr = profile?.Address ?? string.Empty;

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
                // 1) 驗證 SKU/庫存
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
                        errorList.Add($"商品不存在: SKU {item.SkuId}");
                        continue;
                    }
                    if (sku.Product == null || !sku.Product.IsPublished)
                        errorList.Add($"商品已下架: {sku.Product?.ProductName ?? "Unknown"}");
                    else if (!sku.IsActive)
                        errorList.Add($"此規格已停售: {sku.Product.ProductName}");
                    else if (sku.StockQty < item.Quantity)
                        errorList.Add($"庫存不足: {sku.Product.ProductName}（剩餘 {sku.StockQty}）");
                }

                if (errorList.Any())
                {
                    await transaction.RollbackAsync();
                    return Ok(new { success = false, errors = errorList });
                }

                // 2) 計算金額
                var items = request.CartItems.Select(x => new OrderItemDto
                {
                    SkuId = x.SkuId,
                    SalePrice = x.SalePrice,
                    Quantity = x.Quantity
                }).ToList();

                var calculation = await _calculationService.CalculateAsync(auth.UserNumberId, items, request.CouponCode);

                // 3) 取 PaymentConfig
                var paymentConfigId = await _context.OrdPaymentConfigs
                    .Select(p => (int?)p.PaymentConfigId)
                    .FirstOrDefaultAsync();

                if (paymentConfigId is null)
                {
                    await transaction.RollbackAsync();
                    return Ok(new { success = false, message = "尚未設定金流參數（OrdPaymentConfigs）" });
                }

                // 4) 產生訂單
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
                    ShippingFee = request.ShippingFee,      
                    LogisticsId = request.LogisticsId > 0 ? request.LogisticsId : 1000,
                    PaymentConfigId = 1001,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    ReceiverAddress = receiverAddr,
                    HasShippingLabel = false,
                    IsVisibleToMember = true,
                    CreatedDate = DateTime.Now
                };

                _context.OrdOrders.Add(order);
                await _context.SaveChangesAsync();

                // 5) 明細
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

                // 6) 優惠
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

                // 7) 清空會員購物車
                await _context.Database.ExecuteSqlRawAsync(@"
DELETE ci FROM ORD_ShoppingCartItem ci
INNER JOIN ORD_ShoppingCart c ON ci.CartId = c.CartId
WHERE c.UserNumberId = {0}", auth.UserNumberId);

                await transaction.CommitAsync();

                // 8) 綠界表單
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
                        shippingFee = request.ShippingFee,
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

    // ========================================
    // 🔥 DTO 定義
    // ========================================

    /// <summary>結帳請求</summary>
    public class CheckoutRequest
    {
        public List<CheckoutCartItemDto> CartItems { get; set; } = new();
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ReceiverAddress { get; set; }
        public string? CouponCode { get; set; }
        public int LogisticsId { get; set; }
        public decimal ShippingFee { get; set; }
    }

    /// <summary>結帳購物車項目</summary>
    public class CheckoutCartItemDto
    {
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>更新數量</summary>
    public class UpdateQtyDto
    {
        public int Qty { get; set; }
    }

    /// <summary>計算訂單金額請求</summary>
    public class CalculateOrderRequest
    {
        public List<OrderItemDto> CartItems { get; set; } = new();
        public string? CouponCode { get; set; }
    }

    /// <summary>收件地址</summary>
    public class ShippingAddressDto
    {
        public string ReceiverName { get; set; } = "";
        public string ReceiverPhone { get; set; } = "";
        public string ReceiverAddress { get; set; } = "";
    }
}