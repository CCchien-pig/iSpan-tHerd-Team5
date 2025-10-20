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
    [Route("api/ord/cart")]
    [Produces("application/json")]
    [AllowAnonymous]         // 🔹 暫時關閉驗證
    public class CartController : ControllerBase
    {
        private readonly tHerdDBContext _context;

        public CartController(tHerdDBContext context)
        {
            _context = context;
        }

        // 測試 API
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                success = true,
                message = "Cart API is working!",
                timestamp = DateTime.Now
            });
        }

        // POST: api/ord/Cart/validate-coupon
        [HttpPost("validate-coupon")]
        public IActionResult ValidateCoupon([FromBody] CouponValidateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.CouponCode))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "請輸入優惠碼"
                    });
                }

                var couponCode = request.CouponCode.ToLower();

                // 定義可用的優惠券
                var validCoupons = new Dictionary<string, decimal>
                {
                    { "TEST800", 100 }
                };

                if (validCoupons.ContainsKey(couponCode))
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            discountAmount = validCoupons[couponCode],
                            discountType = "fixed",
                            message = $"優惠券套用成功！折扣 ${validCoupons[couponCode]}"
                        }
                    });
                }

                return Ok(new
                {
                    success = false,
                    message = "優惠券無效或已過期"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = $"驗證失敗: {ex.Message}"
                });
            }
        }

        // POST: api/ord/Cart/checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (request?.CartItems == null || !request.CartItems.Any())
                {
                    return Ok(new
                    {
                        success = false,
                        message = "購物車是空的"
                    });
                }

                var errorList = new List<CheckoutErrorItem>();
                decimal subtotal = 0;

                // 檢查商品與庫存
                foreach (var item in request.CartItems)
                {
                    var sku = await _context.ProdProductSkus
                        .AsNoTracking()
                        .Include(s => s.Product)
                        .FirstOrDefaultAsync(s => s.SkuId == item.SkuId && s.ProductId == item.ProductId);

                    if (sku == null)
                    {
                        errorList.Add(new CheckoutErrorItem
                        {
                            ProductName = item.ProductName ?? "未知商品",
                            OptionName = item.OptionName ?? "無資料",
                            Reason = "商品不存在",
                            CurrentStock = null
                        });
                        continue;
                    }

                    string optionNames = sku.SkuCode ?? "預設規格";
                    string status = "";

                    if (!sku.IsActive)
                        status = "已下架";

                    if (sku.StockQty <= 0 || sku.StockQty < item.Quantity)
                    {
                        if (!string.IsNullOrEmpty(status)) status += "、";
                        status += "庫存不足";
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        errorList.Add(new CheckoutErrorItem
                        {
                            ProductName = sku.Product?.ProductName ?? "未知商品",
                            OptionName = optionNames,
                            Reason = status,
                            CurrentStock = sku.StockQty
                        });
                        continue;
                    }

                    subtotal += item.SalePrice * item.Quantity;
                }

                if (errorList.Any())
                {
                    await transaction.RollbackAsync();
                    return Ok(new
                    {
                        success = false,
                        message = "以下商品庫存不足或無法結帳",
                        errors = errorList
                    });
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
                    ReceiverName = null,
                    ReceiverPhone = null,
                    ReceiverAddress = null,
                    HasShippingLabel = false,
                    IsVisibleToMember = true,
                    CreatedDate = DateTime.Now
                };

                _context.OrdOrders.Add(order);
                await _context.SaveChangesAsync();

                // 建立訂單明細
                foreach (var item in request.CartItems)
                {
                    var orderItem = new OrdOrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        SkuId = item.SkuId,
                        Qty = item.Quantity,
                        UnitPrice = item.SalePrice
                    };
                    _context.OrdOrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                // 記錄優惠券
                if (request.DiscountAmount.HasValue && request.DiscountAmount.Value > 0 && !string.IsNullOrEmpty(request.CouponCode))
                {
                    var adjustment = new OrdOrderAdjustment
                    {
                        OrderId = order.OrderId,
                        Kind = "coupon",
                        Scope = "order",
                        Code = request.CouponCode,
                        Method = "fixed",
                        DiscountRate = null,
                        AdjustmentAmount = -request.DiscountAmount.Value,
                        CreatedDate = DateTime.Now,
                        RevisedDate = DateTime.Now
                    };
                    _context.OrdOrderAdjustments.Add(adjustment);
                    await _context.SaveChangesAsync();
                }

                // 清空購物車
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

                decimal totalAmount = subtotal - (request.DiscountAmount ?? 0);

                return Ok(new
                {
                    success = true,
                    message = "訂單建立成功",
                    orderNo = orderNo,
                    orderId = order.OrderId,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Ok(new
                {
                    success = false,
                    message = $"結帳失敗: {ex.Message}"
                });
            }
        }

        // 生成訂單編號
        private async Task<string> GenerateOrderNoAsync()
        {
            string prefix = DateTime.Now.ToString("yyyyMMdd");
            var lastOrder = await _context.OrdOrders
                .Where(o => o.OrderNo.StartsWith(prefix))
                .OrderByDescending(o => o.OrderNo)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (lastOrder != null && int.TryParse(lastOrder.OrderNo.Substring(8), out int lastNum))
            {
                nextNum = lastNum + 1;
            }

            return $"{prefix}{nextNum:D7}";
        }
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

    public class CheckoutErrorItem
    {
        public string ProductName { get; set; }
        public string OptionName { get; set; }
        public string Reason { get; set; }
        public int? CurrentStock { get; set; }
    }
}