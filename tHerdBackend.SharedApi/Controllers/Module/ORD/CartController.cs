using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Models;
using tHerdBackend.Services.ORD;

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

        public CartController(tHerdDBContext context, IECPayService ecpayService)
        {
            _context = context;
            _ecpayService = ecpayService;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { success = true, message = "✅ Cart API is running normally." });
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (request?.CartItems == null || !request.CartItems.Any())
                return Ok(new { success = false, message = "❌ 購物車是空的" });

            _context.Database.SetCommandTimeout(120);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🚀 1. 批次查詢 SKU
                var skuIds = request.CartItems.Select(x => x.SkuId).ToList();
                var skus = await _context.ProdProductSkus
                    .AsNoTracking()
                    .Where(s => skuIds.Contains(s.SkuId))
                    .Select(s => new
                    {
                        s.SkuId,
                        s.ProductId,
                        s.StockQty,
                        ProductName = s.Product != null ? s.Product.ProductName : "未知商品"
                    })
                    .ToDictionaryAsync(s => s.SkuId);

                // 🚀 2. 驗證庫存
                var errorList = new List<string>();
                decimal subtotal = 0;

                foreach (var item in request.CartItems)
                {
                    if (!skus.TryGetValue(item.SkuId, out var sku))
                    {
                        errorList.Add($"找不到商品 SKU: {item.SkuId}");
                        continue;
                    }

                    if (sku.StockQty < item.Quantity)
                    {
                        errorList.Add($"{sku.ProductName} 庫存不足");
                        continue;
                    }

                    subtotal += item.SalePrice * item.Quantity;
                }

                if (errorList.Any())
                {
                    await transaction.RollbackAsync();
                    return Ok(new { success = false, message = "商品無法結帳", errors = errorList });
                }

                // 🚀 3. 產生唯一訂單編號 (符合規定: yyyyMMdd + 7位流水號)
                string orderNo = await GenerateUniqueOrderNoAsync();
                Console.WriteLine($"📝 產生訂單編號: {orderNo}");

                // 🚀 4. 取得 PaymentConfigId
                int paymentConfigId = await _context.OrdPaymentConfigs
                    .OrderBy(p => p.PaymentConfigId)
                    .Select(p => p.PaymentConfigId)
                    .FirstOrDefaultAsync();

                if (paymentConfigId == 0)
                {
                    await transaction.RollbackAsync();
                    return Ok(new
                    {
                        success = false,
                        message = "❌ 系統錯誤: 找不到付款方式設定"
                    });
                }

                // 🚀 5. 建立訂單
                var order = new OrdOrder
                {
                    OrderNo = orderNo,
                    UserNumberId = request.UserNumberId ?? 1056,
                    OrderStatusId = "pending",
                    PaymentStatus = "pending",
                    ShippingStatusId = "unshipped",
                    Subtotal = subtotal,
                    DiscountTotal = request.DiscountAmount ?? 0,
                    ShippingFee = 0,
                    PaymentConfigId = paymentConfigId,
                    ReceiverName = "測試收件人",
                    ReceiverPhone = "0912345678",
                    ReceiverAddress = "台北市中正區測試路 1 號",
                    HasShippingLabel = false,
                    IsVisibleToMember = true,
                    CreatedDate = DateTime.Now
                };

                _context.OrdOrders.Add(order);

                // ⚡ 關鍵: 先儲存訂單,讓 EF Core 產生 OrderId
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ 訂單建立成功: OrderId={order.OrderId}, OrderNo={orderNo}");

                // 🚀 6. 建立訂單明細 (現在 order.OrderId 已有值)
                foreach (var item in request.CartItems)
                {
                    var orderItem = new OrdOrderItem
                    {
                        OrderId = order.OrderId,  // ✅ 現在有值了
                        ProductId = item.ProductId,
                        SkuId = item.SkuId,
                        Qty = item.Quantity,
                        UnitPrice = item.SalePrice
                    };
                    _context.OrdOrderItems.Add(orderItem);
                }

                // 🚀 7. 折扣
                if (!string.IsNullOrEmpty(request.CouponCode) && (request.DiscountAmount ?? 0) > 0)
                {
                    _context.OrdOrderAdjustments.Add(new OrdOrderAdjustment
                    {
                        OrderId = order.OrderId,  // ✅ 現在有值了
                        Kind = "coupon",
                        Scope = "order",
                        Code = request.CouponCode,
                        Method = "fixed",
                        AdjustmentAmount = -request.DiscountAmount.Value,
                        CreatedDate = DateTime.Now,
                        RevisedDate = DateTime.Now
                    });
                }

                // ⚡ 第二次儲存: 訂單明細和折扣
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ 訂單明細建立成功: {request.CartItems.Count} 項商品");

                // 🚀 8. 清空購物車
                if (request.UserNumberId.HasValue || !string.IsNullOrEmpty(request.SessionId))
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        @"DELETE ci FROM ORD_ShoppingCartItem ci
                          INNER JOIN ORD_ShoppingCart c ON ci.CartId = c.CartId
                          WHERE (c.UserNumberId = {0} OR c.SessionId = {1})",
                        request.UserNumberId ?? (object)DBNull.Value,
                        request.SessionId ?? (object)DBNull.Value
                    );
                }

                await transaction.CommitAsync();

                // 🚀 9. 產生綠界付款表單
                decimal totalAmount = subtotal - (request.DiscountAmount ?? 0);

                string itemName = "tHerd商品";
                if (request.CartItems.Any())
                {
                    var firstProduct = request.CartItems.First().ProductName ?? "商品";

                    // 只保留中文、英文、數字、空格、連字號
                    var cleanName = new string(firstProduct
                        .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-')
                        .ToArray())
                        .Trim();  // 👈 確保有 Trim

                    if (cleanName.Length > 30)
                    {
                        itemName = cleanName.Substring(0, 30).Trim();  // 👈 截斷後也 Trim
                    }
                    else if (!string.IsNullOrEmpty(cleanName))
                    {
                        itemName = cleanName;
                    }

                    if (request.CartItems.Count > 1)
                    {
                        itemName += $" 等{request.CartItems.Count}項";
                    }
                }

                var ecpayFormHtml = _ecpayService.CreatePaymentForm(
                    orderId: orderNo,
                    totalAmount: (int)Math.Round(totalAmount),
                    itemName: itemName
                );

                return Ok(new
                {
                    success = true,
                    message = "✅ 訂單建立成功",
                    data = new
                    {
                        orderId = order.OrderId,
                        orderNo,
                        subtotal,
                        discount = request.DiscountAmount ?? 0,
                        total = totalAmount
                    },
                    ecpayFormHtml
                });
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();

                var innerMsg = dbEx.InnerException?.Message ?? "無詳細資訊";
                Console.WriteLine($"❌ DB錯誤: {dbEx.Message}");
                Console.WriteLine($"   Inner: {innerMsg}");

                return Ok(new
                {
                    success = false,
                    message = "❌ 資料庫錯誤",
                    error = dbEx.Message,
                    detail = innerMsg
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                Console.WriteLine($"❌ 結帳錯誤: {ex.Message}");
                Console.WriteLine($"   Inner: {ex.InnerException?.Message ?? "無"}");

                return Ok(new
                {
                    success = false,
                    message = $"❌ 結帳失敗: {ex.Message}",
                    detail = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// ⚡ 產生唯一訂單編號
        /// 格式: yyyyMMdd + 7位流水號 (例如: 202510270000001)
        /// 使用毫秒時間戳確保唯一性
        /// </summary>
        private async Task<string> GenerateUniqueOrderNoAsync()
        {
            string prefix = DateTime.Now.ToString("yyyyMMdd");

            // 使用時間戳作為流水號 (時分秒毫秒)
            // 例如: 14:30:55.123 → 1430551 (取後7位)
            string timestamp = DateTime.Now.ToString("HHmmssfff");
            string sequence = timestamp.Substring(timestamp.Length - 7);

            string orderNo = $"{prefix}{sequence}";

            // 檢查是否重複 (極小機率)
            bool exists = await _context.OrdOrders.AnyAsync(o => o.OrderNo == orderNo);

            if (exists)
            {
                // 如果重複,延遲1毫秒後重新產生
                await Task.Delay(1);
                return await GenerateUniqueOrderNoAsync();
            }

            return orderNo;
        }
    }

    public class CheckoutRequest
    {
        public string? SessionId { get; set; }
        public int? UserNumberId { get; set; }
        public List<CartItemRequest>? CartItems { get; set; }
        public string? CouponCode { get; set; }
        public decimal? DiscountAmount { get; set; }
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