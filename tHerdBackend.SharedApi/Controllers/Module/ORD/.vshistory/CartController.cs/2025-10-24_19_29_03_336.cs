using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tHerdBackend.Infra.Models;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Core.DTOs.ORD;

#nullable enable

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
    [ApiController]
    [Route("api/ord/cart")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class CartController : ControllerBase
    {
        private readonly tHerdDBContext _context;
        private readonly IECPayService _ecpayService;
        private readonly IPaymentRepository _paymentRepo;
        private readonly ILogger<CartController> _logger;

        public CartController(
            tHerdDBContext context,
            IECPayService ecpayService,
            IPaymentRepository paymentRepo,
            ILogger<CartController> logger)
        {
            _context = context;
            _ecpayService = ecpayService;
            _paymentRepo = paymentRepo;
            _logger = logger;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { success = true, message = "Cart API is running normally." });
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "請傳入有效 JSON" });

            if (request.CartItems == null || !request.CartItems.Any())
                return Ok(new { success = false, message = "購物車是空的" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. 檢查商品有效性與庫存
                var errorList = new List<string>();
                decimal subtotal = 0;

                foreach (var item in request.CartItems)
                {
                    var sku = await _context.ProdProductSkus
                        .AsNoTracking()
                        .Include(s => s.Product)
                        .FirstOrDefaultAsync(s => s.SkuId == item.SkuId);

                    if (sku == null)
                    {
                        errorList.Add($"找不到商品 SKU: {item.SkuId}");
                        continue;
                    }

                    if (sku.StockQty < item.Quantity)
                    {
                        errorList.Add($"{sku.Product?.ProductName ?? "未知商品"} 庫存不足，目前庫存 {sku.StockQty}");
                        continue;
                    }

                    subtotal += item.SalePrice * item.Quantity;
                }

                if (errorList.Any())
                {
                    await transaction.RollbackAsync();
                    return Ok(new { success = false, message = "以下商品無法結帳", errors = errorList });
                }

                // 2. 建立訂單主檔
                string orderNo = await GenerateOrderNoAsync();

                var order = new OrdOrder
                {
                    OrderNo = orderNo,
                    UserNumberId = request.UserNumberId ?? 1,
                    OrderStatusId = "pending",
                    PaymentStatus = "pending",
                    ShippingStatusId = "unshipped",
                    Subtotal = subtotal,
                    DiscountTotal = request.DiscountAmount ?? 0,
                    ShippingFee = 0,
                    PaymentConfigId = request.PaymentConfigId,
                    ReceiverName = "測試收件人",
                    ReceiverPhone = "0912345678",
                    ReceiverAddress = "台北市中正區測試路 1 號",
                    HasShippingLabel = false,
                    IsVisibleToMember = true,
                    CreatedDate = DateTime.Now
                };

                _context.OrdOrders.Add(order);
                await _context.SaveChangesAsync();

                // 3. 建立訂單明細
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

                // 4. 折扣處理
                if (!string.IsNullOrEmpty(request.CouponCode) && (request.DiscountAmount ?? 0) > 0)
                {
                    _context.OrdOrderAdjustments.Add(new OrdOrderAdjustment
                    {
                        OrderId = order.OrderId,
                        Kind = "coupon",
                        Scope = "order",
                        Code = request.CouponCode,
                        Method = "fixed",
                        AdjustmentAmount = -request.DiscountAmount.Value,
                        CreatedDate = DateTime.Now,
                        RevisedDate = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

                // 5. 建立付款記錄
                decimal finalTotal = subtotal - (request.DiscountAmount ?? 0);
                string merchantTradeNo = $"{orderNo}_{DateTime.Now:yyyyMMddHHmmss}";

                var paymentId = await _paymentRepo.CreatePaymentAsync(
                    order.OrderId,
                    request.PaymentConfigId,
                    (int)finalTotal,
                    "pending",
                    merchantTradeNo);

                // 6. 建立綠界付款表單
                string itemName = string.Join("#", request.CartItems.Select(i => i.ProductName ?? "商品"));
                if (itemName.Length > 200)
                    itemName = itemName.Substring(0, 200);

                string ecpayFormHtml = _ecpayService.CreatePaymentForm(orderNo, (int)finalTotal, itemName);

                // 7. 清空購物車
                if (!string.IsNullOrEmpty(request.SessionId) || request.UserNumberId.HasValue)
                {
                    var cart = await _context.OrdShoppingCarts
                        .Include(c => c.OrdShoppingCartItems)
                        .FirstOrDefaultAsync(c =>
                            (request.UserNumberId.HasValue && c.UserNumberId == request.UserNumberId) ||
                            (!request.UserNumberId.HasValue && c.SessionId == request.SessionId));

                    if (cart != null)
                    {
                        _context.OrdShoppingCartItems.RemoveRange(cart.OrdShoppingCartItems);
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                _logger.LogInformation($"訂單 {orderNo} 建立成功，準備跳轉綠界");

                return Ok(new
                {
                    success = true,
                    message = "訂單建立成功，正在跳轉至綠界",
                    data = new
                    {
                        orderId = order.OrderId,
                        orderNo = order.OrderNo,
                        paymentId,
                        subtotal,
                        discount = request.DiscountAmount ?? 0,
                        total = finalTotal,
                        ecpayFormHtml
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "結帳失敗");
                string inner = ex.InnerException?.Message ?? "(無內層例外)";
                return Ok(new
                {
                    success = false,
                    message = $"結帳失敗: {ex.Message} | Inner: {inner}"
                });
            }
        }

        private async Task<string> GenerateOrderNoAsync()
        {
            string prefix = DateTime.Now.ToString("yyyyMMdd");
            var last = await _context.OrdOrders
                .Where(o => o.OrderNo.StartsWith(prefix))
                .OrderByDescending(o => o.OrderNo)
                .FirstOrDefaultAsync();

            int next = 1;
            if (last != null && int.TryParse(last.OrderNo.Substring(8), out int lastNum))
                next = lastNum + 1;

            return $"{prefix}{next:D7}";
        }
    }

    public class CheckoutRequest
    {
        public string? SessionId { get; set; }
        public int? UserNumberId { get; set; }
        public List<CartItemRequest>? CartItems { get; set; }

        [BindNever]
        public string? CouponCode { get; set; }

        public decimal? DiscountAmount { get; set; }
        public int PaymentConfigId { get; set; } = 1000;
    }

    public class CartItemRequest
    {
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public string? ProductName { get; set; }
        public string? OptionName { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
    }
}
