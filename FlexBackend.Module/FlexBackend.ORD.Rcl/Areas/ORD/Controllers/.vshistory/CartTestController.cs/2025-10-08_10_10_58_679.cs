using FlexBackend.Infra.Models;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
using FlexBackend.Services.ORD;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
    [Area("ORD")]
    public class CartTestController : Controller
    {
        private readonly tHerdDBContext _db;
        private readonly OrderService _orderService;

        public CartTestController(tHerdDBContext db, OrderService orderService)
        {
            _db = db;
            _orderService = orderService;
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
                         group s by i into g
                         select new CartItemVM
                         {
                             ProductId = g.Key.ProductId,
                             ProductName = g.Key.Product?.ProductName ?? "",
                             SpecCode = g.FirstOrDefault()?.SpecCode ?? "",
                             Qty = g.Key.Qty,
                             UnitPrice = g.Key.UnitPrice,
                             Subtotal = g.Key.UnitPrice * g.Key.Qty
                         }).ToList();

            return View(items);
        }

        // ======================
        // 更新商品數量
        // ======================
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int delta, int userNumberId = 1000)
        {
            try
            {
                // 1. 取得購物車
                var cart = await _db.OrdShoppingCarts
                    .Include(c => c.OrdShoppingCartItems)
                    .FirstOrDefaultAsync(c => c.UserNumberId == userNumberId);

                if (cart == null)
                {
                    return Json(new { success = false, message = "找不到購物車" });
                }

                // 2. 找到對應的商品項目
                var item = cart.OrdShoppingCartItems
                    .FirstOrDefault(i => i.ProductId == productId);

                if (item == null)
                {
                    return Json(new { success = false, message = "購物車中找不到此商品" });
                }

                // 3. 計算新數量
                int newQty = item.Qty + delta;

                // 4. 驗證數量 (必須 > 0)
                if (newQty <= 0)
                {
                    // 數量歸零則移除該商品
                    _db.OrdShoppingCartItems.Remove(item);
                    await _db.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "已移除商品",
                        removed = true,
                        newQty = 0
                    });
                }

                // 5. 更新數量
                item.Qty = newQty;
                await _db.SaveChangesAsync();

                // 6. 回傳新的小計
                decimal newSubtotal = item.UnitPrice * item.Qty;

                return Json(new
                {
                    success = true,
                    newQty = newQty,
                    newSubtotal = newSubtotal,
                    removed = false
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"更新失敗: {ex.Message}" });
            }
        }

        // ======================
        // 移除商品
        // ======================
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int productId, int userNumberId = 1000)
        {
            try
            {
                var cart = await _db.OrdShoppingCarts
                    .Include(c => c.OrdShoppingCartItems)
                    .FirstOrDefaultAsync(c => c.UserNumberId == userNumberId);

                if (cart == null)
                {
                    return Json(new { success = false, message = "找不到購物車" });
                }

                var item = cart.OrdShoppingCartItems
                    .FirstOrDefault(i => i.ProductId == productId);

                if (item == null)
                {
                    return Json(new { success = false, message = "商品不存在" });
                }

                _db.OrdShoppingCartItems.Remove(item);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "已移除商品" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"移除失敗: {ex.Message}" });
            }
        }

        // ======================
        // 購物車結帳
        // ======================
        [HttpPost]
        public async Task<IActionResult> CheckoutFromCart(int userNumberId = 1000)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
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
                    OrderStatusId = "pending",      // 對應 SysCode (ORD/07 待成立)
                    PaymentStatus = "pending",      // 對應 SysCode (ORD/04 待授權)
                    ShippingStatusId = "pending",   // 對應 SysCode (ORD/05 待出貨)

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
                                       select new OrdOrderItem
                                       {
                                           ProductId = i.ProductId,
                                           SkuId = s.SkuId,
                                           UnitPrice = i.UnitPrice,
                                           Qty = i.Qty
                                       }).ToList();

                // 4. 儲存訂單到 DB (取得 OrderId 和 OrderItemId)
                _db.OrdOrders.Add(order);
                await _db.SaveChangesAsync();

                // 5. ✅ 呼叫 OrderService 扣庫存
                var (success, message) = await _orderService.ProcessStockDeductionAsync(
                    order.OrderId,
                    userNumberId);

                if (!success)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message });
                }

                // 6. 清空購物車
                _db.OrdShoppingCartItems.RemoveRange(cart.OrdShoppingCartItems);
                _db.OrdShoppingCarts.Remove(cart);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                // 7. 回傳成功訊息
                return Json(new
                {
                    success = true,
                    orderId = order.OrderId,
                    message = "結帳成功,庫存已扣減"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"結帳失敗: {ex.Message}" });
            }
        }
    }
}