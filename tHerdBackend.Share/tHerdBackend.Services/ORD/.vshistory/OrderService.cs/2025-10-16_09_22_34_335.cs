// tHerdBackend.Services/ORD/OrderService.cs
using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace tHerdBackend.Services.ORD
{
    public interface IOrderService
    {
        Task<OrderResult> CreateOrderAsync(OrderCreateDto dto);
        Task<OrderResult> CallSupSaleStockAsync(List<OrderItemDto> items, string orderNo);
    }

    public class OrderService : IOrderService
    {
        private readonly tHerdDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(
            tHerdDBContext context,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OrderResult> CreateOrderAsync(OrderCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 建立訂單邏輯
                var order = new OrdOrder
                {
                    OrderNo = await GenerateOrderNoAsync(),
                    UserNumberId = dto.UserNumberId,
                    OrderStatusId = "pending",
                    PaymentStatus = "paid",
                    ShippingStatusId = "unshipped",
                    Subtotal = dto.Subtotal,
                    // ... 其他欄位
                    CreatedDate = DateTime.Now
                };

                _context.Set<OrdOrder>().Add(order);
                await _context.SaveChangesAsync();

                // 建立訂單明細
                var orderItems = new List<OrdOrderItem>();
                foreach (var item in dto.Items)
                {
                    var orderItem = new OrdOrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        SkuId = item.SkuId,
                        UnitPrice = item.UnitPrice,
                        Qty = item.Quantity
                    };

                    _context.Set<OrdOrderItem>().Add(orderItem);
                    orderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                // 呼叫 SUP 扣庫存
                var stockResult = await CallSupSaleStockAsync(
                    orderItems.Select(x => new OrderItemDto
                    {
                        OrderItemId = x.OrderItemId,
                        SkuId = x.SkuId,
                        Quantity = x.Qty
                    }).ToList(),
                    order.OrderNo
                );

                if (!stockResult.Success)
                {
                    await transaction.RollbackAsync();
                    return new OrderResult
                    {
                        Success = false,
                        Message = "庫存扣減失敗：" + stockResult.Message
                    };
                }

                await transaction.CommitAsync();

                return new OrderResult
                {
                    Success = true,
                    Message = "訂單建立成功",
                    OrderNo = order.OrderNo,
                    OrderId = order.OrderId
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new OrderResult
                {
                    Success = false,
                    Message = "系統錯誤：" + ex.Message
                };
            }
        }

        public async Task<OrderResult> CallSupSaleStockAsync(List<OrderItemDto> items, string orderNo)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var httpContext = _httpContextAccessor.HttpContext;

                var allSuccess = true;
                var errorMessages = new List<string>();

                foreach (var item in items)
                {
                    var formData = new MultipartFormDataContent
                    {
                        { new StringContent(item.SkuId.ToString()), "SkuId" },
                        { new StringContent(item.Quantity.ToString()), "ChangeQty" },
                        { new StringContent(item.OrderItemId.ToString()), "OrderItemId" },
                        { new StringContent($"訂單編號：{orderNo}"), "Remark" }
                    };

                    // 防偽 token
                    var token = httpContext?.Request.Form["__RequestVerificationToken"].ToString();
                    if (!string.IsNullOrEmpty(token))
                    {
                        formData.Add(new StringContent(token), "__RequestVerificationToken");
                    }

                    var response = await client.PostAsync("/SUP/StockBatches/SaleStock", formData);

                    if (!response.IsSuccessStatusCode)
                    {
                        allSuccess = false;
                        errorMessages.Add($"SKU {item.SkuId} 扣庫失敗");
                        continue;
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = System.Text.Json.JsonSerializer.Deserialize<SupApiResult>(
                        jsonResponse,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (result == null || !result.Success)
                    {
                        allSuccess = false;
                        errorMessages.Add($"SKU {item.SkuId}: {result?.Message ?? "未知錯誤"}");
                    }
                }

                return new OrderResult
                {
                    Success = allSuccess,
                    Message = allSuccess ? "所有庫存扣減成功" : string.Join("; ", errorMessages)
                };
            }
            catch (Exception ex)
            {
                return new OrderResult
                {
                    Success = false,
                    Message = "呼叫 SUP API 錯誤：" + ex.Message
                };
            }
        }

        private async Task<string> GenerateOrderNoAsync()
        {
            string datePrefix = DateTime.Now.ToString("yyyyMMdd");

            var lastOrder = await _context.Set<OrdOrder>()
                .Where(o => o.OrderNo.StartsWith(datePrefix))
                .OrderByDescending(o => o.OrderNo)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastOrder != null)
            {
                string lastNumberStr = lastOrder.OrderNo.Substring(8);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{datePrefix}{nextNumber:D7}";
        }
    }

    // DTO
    public class OrderCreateDto
    {
        public int UserNumberId { get; set; }
        public decimal Subtotal { get; set; }
        public List<OrderItemDto> Items { get; set; }
    }

    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string OrderNo { get; set; }
        public int OrderId { get; set; }
    }

    public class SupApiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}