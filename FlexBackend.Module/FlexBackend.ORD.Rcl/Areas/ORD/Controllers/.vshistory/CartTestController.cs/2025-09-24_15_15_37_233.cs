using FlexBackend.Core.Interfaces.SUP;
using FlexBackend.Infra.Models;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
    [Area("ORD")]
    public class CartTestController : Controller
    {
        private readonly tHerdDBContext _db;

        public CartTestController(tHerdDBContext db)
        {
            _db = db;
        }

        // ======================
        // 購物車頁面
        // ======================
        [HttpGet]
        public async Task<IActionResult> CartIndex(int userNumberId = 1001)
        {
            var cart = await _db.OrdShoppingCarts
                .Include(c => c.OrdShoppingCartItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserNumberId == userNumberId);

            if (cart == null || !cart.OrdShoppingCartItems.Any())
                return View(new List<CartItemVM>());

            var items = cart.OrdShoppingCartItems.Select(i => new CartItemVM
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.ProductName ?? "",
                Qty = i.Qty,
                UnitPrice = i.UnitPrice,
                Subtotal = i.UnitPrice * i.Qty
            }).ToList();

            return View(items);
        }

        // ======================
        // 購物車結帳Action
        // ======================
        [HttpPost]
        public async Task<IActionResult> CheckoutFromCart(int userNumberId = 1001)
        {
            var cart = await _db.OrdShoppingCarts
                .Include(c => c.OrdShoppingCartItems)
                .FirstOrDefaultAsync(c => c.UserNumberId == userNumberId);

            if (cart == null || !cart.OrdShoppingCartItems.Any())
                return Json(new { success = false, message = "購物車是空的" });

            var order = new OrdOrder
            {
                UserNumberId = userNumberId,
                CreatedDate = DateTime.Now,
                OrderStatusId = "pending",
                PaymentStatus = "pending",
                ShippingStatusId = "picking",
                Subtotal = cart.OrdShoppingCartItems.Sum(i => i.UnitPrice * i.Qty),
                DiscountTotal = 0,
                ShippingFee = 0,
                ReceiverName = "測試收件人",
                ReceiverPhone = "0900-000-000",
                ReceiverAddress = "測試地址",
                HasShippingLabel = false,
                IsVisibleToMember = true
            };

            order.OrdOrderItems = cart.OrdShoppingCartItems.Select(i => new OrdOrderItem
            {
                ProductId = i.ProductId,
                SkuId = 0, // TODO: 之後接 SKU
                UnitPrice = i.UnitPrice,
                Qty = i.Qty
            }).ToList();

            _db.OrdOrders.Add(order);

            _db.OrdShoppingCartItems.RemoveRange(cart.OrdShoppingCartItems);
            _db.OrdShoppingCarts.Remove(cart);

            await _db.SaveChangesAsync();

            return Json(new { success = true, orderId = order.OrderId });
        }
    }
}
