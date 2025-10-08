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
        public async Task<IActionResult> Index(int userNumberId = 1000)
        {
            var cart = await _db.OrdShoppingCarts
                .Include(c => c.OrdShoppingCartItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserNumberId == userNumberId);

            if (cart == null || !cart.OrdShoppingCartItems.Any())
            {
                return View(new List<CartItemVM>());
            }

            var items = (from i in cart.OrdShoppingCartItems
                         join s in _db.ProdProductSkus on i.ProductId equals s.ProductId
                         // ⚠ 如果一個 Product 有多個 SKU，可以先取第一個
                         group s by i into g
                         select new CartItemVM
                         {
                             ProductId = g.Key.ProductId,
                             ProductName = g.Key.Product?.ProductName ?? "",
                             Qty = g.Key.Qty,
                             UnitPrice = g.Key.UnitPrice,
                             Subtotal = g.Key.UnitPrice * g.Key.Qty,
                             SpecCode = g.FirstOrDefault().SpecCode   // ← 加上規格
                         }).ToList();

            return View(items);
        }


        // ======================
        // 購物車結帳
        // ======================
        [HttpPost]
        public async Task<IActionResult> CheckoutFromCart(int userNumberId = 1000)
        {
            // 1. 取得購物車
            var cart = await _db.OrdShoppingCarts
                .Include(c => c.OrdShoppingCartItems)
                .FirstOrDefaultAsync(c => c.UserNumberId == userNumberId);

            if (cart == null || !cart.OrdShoppingCartItems.Any())
            {
                return Json(new { success = false, message = "購物車是空的" });
            }

            // 2. 建立訂單主檔
            var order = new OrdOrder
            {
                UserNumberId = userNumberId,
                CreatedDate = DateTime.Now,

                // 狀態預設
                OrderStatusId = "pending",      // → 對應 SysCode (ORD/07 待成立)
                PaymentStatus = "pending",      // → 對應 SysCode (ORD/04 待授權)
                ShippingStatusId = "pending",   // → 對應 SysCode (ORD/05 待出貨)

                // 金額欄位
                Subtotal = cart.OrdShoppingCartItems.Sum(i => i.UnitPrice * i.Qty),
                DiscountTotal = 0,
                ShippingFee = 0,

                ReceiverName = "測試收件人",
                ReceiverPhone = "0900-000-000",
                ReceiverAddress = "測試地址",

                HasShippingLabel = false,
                IsVisibleToMember = true
            };

            // 3. 建立訂單明細 (帶入 SkuId)
            order.OrdOrderItems = (from i in cart.OrdShoppingCartItems
                                   join s in _db.ProdProductSkus on i.ProductId equals s.ProductId
                                   // 這裡取第一個 SKU，如果一個商品有多個 SKU，可以讓使用者選
                                   select new OrdOrderItem
                                   {
                                       ProductId = i.ProductId,
                                       SkuId = s.SkuId,  // ✅ 正確帶入 SKU
                                       UnitPrice = i.UnitPrice,
                                       Qty = i.Qty
                                   }).ToList();

            // 4. 儲存到 DB
            _db.OrdOrders.Add(order);

            // 清空購物車
            _db.OrdShoppingCartItems.RemoveRange(cart.OrdShoppingCartItems);
            _db.OrdShoppingCarts.Remove(cart);

            await _db.SaveChangesAsync();

            // 5. 回傳成功訊息
            return Json(new { success = true, orderId = order.OrderId });
        }

    }
}
