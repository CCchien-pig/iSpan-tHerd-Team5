using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
    [ApiController]
    [Route("api/ord/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class CartController : ControllerBase
    {
        private readonly tHerdDBContext _context;

        public CartController(tHerdDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 測試 API
        /// </summary>
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new
            {
                success = true,
                message = "Cart API is working!",
                timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 驗證優惠券
        /// </summary>
        [HttpPost("validate-coupon")]
        [AllowAnonymous]
        public IActionResult ValidateCoupon([FromBody] CouponValidateRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.CouponCode))
                    return Ok(ApiResponse<object>.Fail("請輸入優惠碼"));

                var code = request.CouponCode.Trim().ToUpper();

                var coupons = new Dictionary<string, decimal>
                {
                    { "TEST800", 100 },
                    { "SAVE200", 200 }
                };

                if (!coupons.ContainsKey(code))
                    return Ok(ApiResponse<object>.Fail("優惠券無效或已過期"));

                return Ok(ApiResponse<object>.Ok(new
                {
                    discountAmount = coupons[code],
                    message = $"優惠券套用成功！折扣 {coupons[code]} 元"
                }, "驗證成功"));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Fail($"驗證失敗: {ex.Message}"));
            }
        }

        /// <summary>
        /// 結帳 - 建立訂單
        /// </summary>
        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            try
            {
                if (request == null)
                    return Ok(ApiResponse<object>.Fail("未接收到資料"));

                if (request.CartItems == null || !request.CartItems.Any())
                    return Ok(ApiResponse<object>.Fail("購物車是空的"));

                using var tx = await _context.Database.BeginTransactionAsync();

                var errors = new List<string>();
                decimal subtotal = 0;

                foreach (var item in request.CartItems)
                {
                    var sku = await _context.ProdProductSkus
                        .Include(s => s.Product)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.SkuId == item.SkuId);

                    if (sku == null)
                    {
                        errors.Add($"找不到商品：{item.ProductName}");
                        continue;
                    }

                    if (!sku.IsActive)
                        errors.Add($"商品「{sku.Product.ProductName}」已下架");

                    if (sku.StockQty < item.Quantity)
                        errors.Add($"商品「{sku.Product.ProductName}」庫存不足（剩餘 {sku.StockQty}）");

                    subtotal += item.SalePrice * item.Quantity;
                }

                if (errors.Any())
                {
                    await tx.RollbackAsync();
                    return Ok(ApiResponse<object>.Fail(string.Join("；", errors)));
                }

                // 建立訂單
                string orderNo = await GenerateOrderNoAsync();

                var order = new OrdOrder
                {
                    OrderNo = orderNo,
                    UserNumberId = request.UserNumberId ?? 1056,
                    OrderStatusId = "pending",
                    PaymentStatus = "unpaid",
                    ShippingStatusId = "unshipped",
                    Subtotal = subtotal,
                    DiscountTotal = request.DiscountAmount ?? 0,
                    ShippingFee = 0,
                    PaymentConfigId = 1000,
                    CreatedDate = DateTime.Now,
                    HasShippingLabel = false,
                    IsVisibleToMember = true
                };

                _context.OrdOrders.Add(order);
                await _context.SaveChangesAsync();

                // 訂單明細
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
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return Ok(ApiResponse<object>.Ok(new
                {
                    orderId = order.OrderId,
                    orderNo = order.OrderNo,
                    total = subtotal - (request.DiscountAmount ?? 0)
                }, "訂單建立成功"));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Fail($"結帳失敗：{ex.Message}"));
            }
        }

        // 產生訂單編號
        private async Task<string> GenerateOrderNoAsync()
        {
            string prefix = DateTime.Now.ToString("yyyyMMdd");
            var last = await _context.OrdOrders
                .Where(o => o.OrderNo.StartsWith(prefix))
                .OrderByDescending(o => o.OrderNo)
                .FirstOrDefaultAsync();

            int next = 1;
            if (last != null && int.TryParse(last.OrderNo.Substring(8), out int n))
                next = n + 1;

            return $"{prefix}{next:D6}";
        }
    }

    // 統一回傳格式
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static ApiResponse<T> Ok(T data, string msg = null) => new() { Success = true, Data = data, Message = msg };
        public static ApiResponse<T> Fail(string msg) => new() { Success = false, Message = msg };
    }

    // Request Models
    public class CouponValidateRequest
    {
        public string CouponCode { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class CheckoutRequest
    {
        public string SessionId { get; set; }
        public int? UserNumberId { get; set; }
        public List<CartItemRequest> CartItems { get; set; }
        public string CouponCode { get; set; }
        public decimal? DiscountAmount { get; set; }
    }

    public class CartItemRequest
    {
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public string ProductName { get; set; }
        public string OptionName { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
    }
}
