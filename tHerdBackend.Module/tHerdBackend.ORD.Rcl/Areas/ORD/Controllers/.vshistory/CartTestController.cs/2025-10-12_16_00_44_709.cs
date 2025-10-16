using FlexBackend.Infra.Models;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
    [Area("ORD")]
    [Route("ORD/[controller]/[action]")]
    public class CartTestController : Controller
    {
        private readonly tHerdDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartTestController(
            tHerdDBContext context,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Index() => View();

        // ======================================================
        // GET: ORD/CartTest/GetInitialCart
        // 模擬購物車資料
        // ======================================================
        [HttpGet]
        public IActionResult GetInitialCart()
        {
            var cartItems = new List<CartItemVM>
            {
                new CartItemVM
                {
                    ProductId = 14246,
                    SkuId = 2680,
                    ProductName = "Lake Avenue Nutrition, Omega-3 魚油，30 粒魚明膠軟膠囊（每粒軟膠囊 1,250 毫克）",
                    OptionName = "30 單位",
                    UnitPrice = 500.00m,
                    SalePrice = 346.00m,
                    Quantity = 1
                },
                new CartItemVM
                {
                    ProductId = 14246,
                    SkuId = 3388,
                    ProductName = "Lake Avenue Nutrition, Omega-3 魚油，30 粒魚明膠軟膠囊（每粒軟膠囊 1,250 毫克）",
                    OptionName = "90 單位",
                    UnitPrice = 1000.00m,
                    SalePrice = 898.00m,
                    Quantity = 1
                },
                new CartItemVM
                {
                    ProductId = 14600,
                    SkuId = 2869,
                    ProductName = "Optimum Nutrition, Opti-Women®，針對活躍 女性的多維生素，60 粒膠囊",
                    OptionName = "60 粒",
                    UnitPrice = 800.00m,
                    SalePrice = 656.00m,
                    Quantity = 1
                },
                new CartItemVM
                {
                    ProductId = 14600,
                    SkuId = 3387,
                    ProductName = "Optimum Nutrition, Opti-Women®，針對活躍 女性的多維生素，60 粒膠囊",
                    OptionName = "120 粒",
                    UnitPrice = 1300.00m,
                    SalePrice = 1188.00m,
                    Quantity = 1
                }
            };

            return Json(cartItems);
        }

        // ======================================================
        // POST: ORD/CartTest/Checkout
        // Step 1~5 全流程（含呼叫 SUP 出庫）
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestVM request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Step 1: 驗證購物車
                if (request.CartItems == null || !request.CartItems.Any())
                    return Json(new CheckoutResponseVM { Success = false, Message = "購物車是空的" });

                var errorList = new List<(string Product, string Option, string Status)>();
                decimal subtotal = 0;

                // Step 2: 檢查商品與庫存
                foreach (var item in request.CartItems)
                {
                    var sku = await _context.ProdProductSkus
                        .AsNoTracking()
                        .Include(s => s.Product)
                        .FirstOrDefaultAsync(s => s.SkuId == item.SkuId && s.ProductId == item.ProductId);

                    if (sku == null)
                    {
                        errorList.Add(("未知商品", "無資料", "商品不存在"));
                        continue;
                    }

                    string optionNames = sku.SkuCode ?? "預設規格";
                    string status = "";

                    if (!sku.IsActive)
                        status = "已下架";

                    if (sku.StockQty <= 0 || sku.StockQty < item.Quantity)
                    {
                        if (!string.IsNullOrEmpty(status)) status += "、";
                        status += $"庫存不足 (庫存:{sku.StockQty}, 需求:{item.Quantity})";
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        errorList.Add((sku.Product?.ProductName ?? "未知商品", optionNames, status));
                        continue;
                    }

                    subtotal += item.SalePrice * item.Quantity;
                }

                if (errorList.Any())
                {
                    await transaction.RollbackAsync();
                    var formattedErrors = string.Join("|||", errorList.Select(e => $"{e.Product}||{e.Option}||{e.Status}"));
                    return Json(new CheckoutResponseVM { Success = false, Message = "以下商品庫存不足或無法結帳：" + formattedErrors });
                }

                // Step 3: 建立訂單主檔
                string orderNo = await GenerateOrderNoAsync();
                var order = new OrdOrder
                {
                    OrderNo = orderNo,
                    UserNumberId = 1056,
                    OrderStatusId = "pending",
                    PaymentStatus = "paid",
                    ShippingStatusId = "unshipped",
                    Subtotal = subtotal,
                    DiscountTotal = 0,
                    ShippingFee = 0,
                    PaymentConfigId = 1000,
                    ReceiverName = "測試收件人",
                    ReceiverPhone = "0900000000",
                    ReceiverAddress = "測試地址",
                    HasShippingLabel = false,
                    IsVisibleToMember = true,
                    CreatedDate = DateTime.Now
                };

                _context.OrdOrders.Add(order);
                await _context.SaveChangesAsync();

                // Step 4: 建立訂單明細
                var orderItems = request.CartItems.Select(i => new OrdOrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = i.ProductId,
                    SkuId = i.SkuId,
                    UnitPrice = i.SalePrice,
                    Qty = i.Quantity
                }).ToList();

                _context.OrdOrderItems.AddRange(orderItems);
                await _context.SaveChangesAsync();

                // Step 5: 呼叫 SUP API 扣庫存
                var stockResult = await CallSupSaleStockAsync(orderItems, orderNo);

                if (!stockResult.Success)
                {
                    await transaction.RollbackAsync();
                    return Json(new CheckoutResponseVM { Success = false, Message = "庫存扣減失敗：" + stockResult.Message });
                }

                // Step 6: 完成交易
                await transaction.CommitAsync();
                return Json(new CheckoutResponseVM
                {
                    Success = true,
                    Message = "訂單建立成功",
                    OrderNo = orderNo,
                    TotalAmount = subtotal
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new CheckoutResponseVM
                {
                    Success = false,
                    Message = $"結帳失敗：{ex.Message}"
                });
            }
        }

        // ======================================================
        // Step 5 子程序：呼叫 SUP 出庫 API
        // ======================================================
        private async Task<SupApiResult> CallSupSaleStockAsync(List<OrdOrderItem> orderItems, string orderNo)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var httpContext = _httpContextAccessor.HttpContext;
                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                client.BaseAddress = new Uri(baseUrl);

                // 攜帶登入 cookie
                if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookies))
                {
                    client.DefaultRequestHeaders.Add("Cookie", cookies.ToString());
                }

                // 先 GET SUP 的頁面拿到真實 AntiForgery token
                var tokenResponse = await client.GetAsync("/SUP/StockBatches/SaleStock");
                var html = await tokenResponse.Content.ReadAsStringAsync();
                var match = System.Text.RegularExpressions.Regex.Match(
                    html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(.+?)\"");
                string realToken = match.Success ? match.Groups[1].Value : "";
                Console.WriteLine($"[ORD→SUP] 抓到 token={realToken}");

                bool allSuccess = true;
                var errors = new List<string>();

                foreach (var item in orderItems)
                {
                    var form = new Dictionary<string, string>
                    {
                        { "SkuId", item.SkuId.ToString() },
                        { "ChangeQty", item.Qty.ToString() },
                        { "OrderItemId", item.OrderItemId.ToString() },
                        { "Remark", $"訂單編號：{orderNo}" },
                        { "ManufactureDate", DateTime.Now.ToString("yyyy-MM-dd") }, 
                        { "__RequestVerificationToken", realToken }
                    };


                    var content = new FormUrlEncodedContent(form);

                    var response = await client.PostAsync("/SUP/StockBatches/SaleStock", content);

                    var raw = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        allSuccess = false;
                        errors.Add($"SKU {item.SkuId} 扣庫失敗：HTTP {response.StatusCode}");
                        continue;
                    }

                    var result = JsonSerializer.Deserialize<SupApiResult>(
                        raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result == null || !result.Success)
                    {
                        allSuccess = false;
                        errors.Add($"SKU {item.SkuId}: {result?.Message ?? "未知錯誤"}");
                    }
                }

                return new SupApiResult
                {
                    Success = allSuccess,
                    Message = allSuccess ? "所有庫存扣減成功" : string.Join("; ", errors)
                };
            }
            catch (Exception ex)
            {
                return new SupApiResult
                {
                    Success = false,
                    Message = "呼叫 SUP API 發生錯誤：" + ex.Message
                };
            }
        }

        // ======================================================
        // 產生訂單編號 (yyyyMMdd + 7位流水號)
        // ======================================================
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

        // ======================================================
        // SUP API 回傳模型
        // ======================================================
        public class SupApiResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
