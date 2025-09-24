using FlexBackend.Core.Interfaces.SUP;
using FlexBackend.Infra.Models;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
    [Area("ORD")]
    public class CartTestController : Controller
    {
        private readonly tHerdDBContext _db;
        private readonly IStockService _stockService;

        public CartTestController(tHerdDBContext db, IStockService stockService)
        {
            _db = db;
            _stockService = stockService;
        }

        // 顯示測試購物車
        [HttpGet]
        public IActionResult Index()
        {
            var items = new List<CartItemVM>
        {
            new CartItemVM { ProductId = 1, ProductName = "測試商品 A", Qty = 2, UnitPrice = 100 },
            new CartItemVM { ProductId = 2, ProductName = "測試商品 B", Qty = 1, UnitPrice = 250 }
        };

            return View(items);
        }

        // 模擬結帳
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var order = new OrdOrder
            {
                UserNumberId = 1001,
                CreatedDate = DateTime.Now,
                OrderStatusId = "pending",
                PaymentStatus = "pending",
                ShippingStatusId = "picking",
                Subtotal = 450,
                DiscountTotal = 0,
                ShippingFee = 0,
                ReceiverName = "測試收件人",
                ReceiverPhone = "0900-000-000",
                ReceiverAddress = "測試地址",
                IsVisibleToMember = true
            };

            order.OrdOrderItems.Add(new OrdOrderItem { ProductId = 1, SkuId = 1, Qty = 2, UnitPrice = 100 });
            order.OrdOrderItems.Add(new OrdOrderItem { ProductId = 2, SkuId = 2, Qty = 1, UnitPrice = 250 });

            _db.OrdOrders.Add(order);
            await _db.SaveChangesAsync();

            return Json(new { success = true, orderId = order.OrderId });
        }
    }

}
