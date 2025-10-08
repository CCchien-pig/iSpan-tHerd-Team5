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

        // ========================================
        // 購物車頁面
        // ========================================
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
                             Qty = g.Key.Qty,
                             UnitPrice = g.Key.UnitPrice,
                             Subtotal = g.Key.UnitPrice * g.Key.Qty,
                             SpecCode = g.FirstOrDefault()?.SpecCode ?? ""
                         }).ToList();

            return View(items);
        }

        // ========================================
        // 更新商品數量
        // ========================================
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int delta, int userNumberId = 1000)
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
                    return Json(new { success = false, message = "購物車中找不到此商品" });
                }

                int newQty = item.Qty + delta;

                if (newQty <= 0)
                {
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

                item.Qty = newQty;
                await _db.SaveChangesAsync();

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

        // ========================================
        // 移除商品
        // ========================================
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

        // ========================================
        // 購物車結帳
        // ========================================
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

                // 2. 生成訂單編號
                string orderNo = $"{DateTime.Now:yyyyMMdd}{new Random().Next(10000, 9999999)}";

                // 3. 建立訂單
                var order = new OrdOrder
                {
                    OrderNo = orderNo,
                    UserNumberId = userNumberId,
                    CreatedDate = DateTime.Now,

                    OrderStatusId = "pending",
                    PaymentStatus = "pending",
                    ShippingStatusId = "pending",

                    Subtotal = cart.OrdShoppingCartItems.Sum(i => i.UnitPrice * i.Qty),
                    DiscountTotal = 0,
                    ShippingFee = 0,
                    PaymentConfigId = 1000,

                    ReceiverName = "測試收件人",
                    ReceiverPhone = "0900-000-000",
                    ReceiverAddress = "測試地址",

                    HasShippingLabel = false,
                    IsVisibleToMember = true
                };

                // 4. 建立訂單明細
                order.OrdOrderItems = (from i in cart.OrdShoppingCartItems
                                       join s in _db.ProdProductSkus on i.ProductId equals s.ProductId
                                       select new OrdOrderItem
                                       {
                                           ProductId = i.ProductId,
                                           SkuId = s.SkuId,
                                           UnitPrice = i.UnitPrice,
                                           Qty = i.Qty
                                       }).ToList();

                // 5. 儲存訂單
                _db.OrdOrders.Add(order);
                await _db.SaveChangesAsync();

                // 6. 扣庫存
                var (success, message) = await _orderService.ProcessStockDeductionAsync(
                    order.OrderId,
                    userNumberId);

                if (!success)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message });
                }

                // 7. 清空購物車
                _db.OrdShoppingCartItems.RemoveRange(cart.OrdShoppingCartItems);
                _db.OrdShoppingCarts.Remove(cart);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    orderId = order.OrderId,
                    orderNo = order.OrderNo,
                    message = "結帳成功,庫存已扣減"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // 回傳完整錯誤訊息
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | 內部錯誤: {ex.InnerException.Message}";

                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += $" | 詳細錯誤: {ex.InnerException.InnerException.Message}";
                    }
                }

                return Json(new
                {
                    success = false,
                    message = $"結帳失敗: {errorMessage}"
                });
            }
        }
    }
}