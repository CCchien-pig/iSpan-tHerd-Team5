using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlexBackend.Infra.Models;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;


namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
    [Area("ORD")]
    public class RmaController : Controller
    {
        private readonly tHerdDBContext _db;
        public RmaController(tHerdDBContext db) => _db = db;

        // 狀態中文轉換
        private string GetStatusText(string? status) => (status ?? "").ToLowerInvariant() switch
        {
            "pending" => "待審核",
            "review" => "審核中",
            "refunding" => "退款中",
            "reshipping" => "補寄中",
            "done" => "處理完成",
            "rejected" => "駁回",
            _ => status ?? ""
        };

        // 退貨列表
        [HttpGet]
        public IActionResult Index(string? group = "all", int page = 1, int pageSize = 10, string? keyword = "")
        {
            var q = _db.OrdReturnRequests
                .Include(r => r.Order)
                .Include(r => r.OrdReturnItems).ThenInclude(ri => ri.OrderItem)
                .AsNoTracking()
                .AsQueryable();

            // 搜尋：RMA（RmaId）或訂單號
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(r => r.RmaId.Contains(keyword) || r.Order.OrderNo.Contains(keyword));
            }

            // 分組（依 Status）
            switch (group)
            {
                case "pending": q = q.Where(r => r.Status == "pending"); break;
                case "review": q = q.Where(r => r.Status == "review"); break;
                case "refunding": q = q.Where(r => r.Status == "refunding"); break;
                case "reshipping": q = q.Where(r => r.Status == "reshipping"); break;
                case "done": q = q.Where(r => r.Status == "done"); break;
                case "rejected": q = q.Where(r => r.Status == "rejected"); break;
                default: break;
            }

            var total = q.Count();

            var list = q.OrderByDescending(r => r.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReturnListVM
                {
                    ReturnRequestId = r.ReturnRequestId,
                    RmaNo = r.RmaId,
                    OrderNo = r.Order.OrderNo,
                    TypeName = r.RequestType,
                    ScopeName = r.RefundScope,
                    StatusName = r.Status,
                    CreatedDate = r.CreatedDate,
                    Reason = r.ReasonText,

                    // 為確保可編譯，先以 ID 顯示，有商品/規格名稱時可改為 oi.Product.XXX / oi.Sku.XXX
                    ProductName = r.OrdReturnItems
                                        .OrderBy(i => i.RmaItemId)
                                        .Select(i => "PID:" + i.OrderItem.ProductId)
                                        .FirstOrDefault() ?? "",
                    Spec = r.OrdReturnItems
                                        .OrderBy(i => i.RmaItemId)
                                        .Select(i => "SKU:" + i.OrderItem.SkuId)
                                        .FirstOrDefault() ?? "",
                    Qty = r.OrdReturnItems
                                        .OrderBy(i => i.RmaItemId)
                                        .Select(i => (int?)i.Qty)
                                        .FirstOrDefault() ?? 0
                })
                .ToList();

            var tabs = new Dictionary<string, int>
            {
                { "all",       _db.OrdReturnRequests.AsNoTracking().Count() },
                { "pending",   _db.OrdReturnRequests.AsNoTracking().Count(x => x.Status == "pending") },
                { "review",    _db.OrdReturnRequests.AsNoTracking().Count(x => x.Status == "review") },
                { "refunding", _db.OrdReturnRequests.AsNoTracking().Count(x => x.Status == "refunding") },
                { "reshipping", _db.OrdReturnRequests.AsNoTracking().Count(x => x.Status == "reshipping") },
                { "done",      _db.OrdReturnRequests.AsNoTracking().Count(x => x.Status == "done") },
                { "rejected",  _db.OrdReturnRequests.AsNoTracking().Count(x => x.Status == "rejected") }
            };

            var vm = new ReturnListPageVM
            {
                Keyword = keyword,
                Group = group ?? "all",
                Tabs = tabs,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Pages = (int)Math.Ceiling(total / (double)pageSize),
                RmaList = list
            };

            return View(vm);
        }

        // 明細（給 Modal）
        [HttpGet]
        public IActionResult GetReturnDetail(int id)
        {
            var r = _db.OrdReturnRequests
                .Include(x => x.Order)
                .Include(x => x.OrdReturnItems).ThenInclude(ri => ri.OrderItem)
                .AsNoTracking()
                .FirstOrDefault(x => x.ReturnRequestId == id);

            if (r == null) return Json(new { ok = false, message = "RMA not found." });

            // 訂單品項（用 OrderItem，若要顯示商品名/規格名，可改為 oi.Product.Name / oi.Sku.XXX）
            var orderItems = _db.OrdOrderItems
                .Where(oi => oi.OrderId == r.OrderId)
                .Select(oi => new
                {
                    productName = "PID:" + oi.ProductId, // TODO: 可改為商品名稱
                    skuSpec = "SKU:" + oi.SkuId,     // TODO: 可改為規格名稱
                    unitPrice = oi.UnitPrice,
                    qty = oi.Qty,
                    subTotal = oi.UnitPrice * oi.Qty
                })
                .ToList();

            var rmaItems = r.OrdReturnItems
                .OrderBy(i => i.RmaItemId)
                .Select(i => new
                {
                    productName = "PID:" + i.OrderItem.ProductId, // TODO: 可改為商品名稱
                    skuSpec = "SKU:" + i.OrderItem.SkuId,     // TODO: 可改為規格名稱
                    originalQty = i.OrderItem.Qty,
                    qty = i.Qty,
                    approvedQty = i.ApprovedQty,
                    refundQty = i.RefundQty,
                    reshipQty = i.ReshipQty,
                    refundUnitAmount = i.RefundUnitAmount
                })
                .ToList();

            var orderTotal = r.Order.Subtotal - r.Order.DiscountTotal + r.Order.ShippingFee;

            var dto = new
            {
                orderNo = r.Order.OrderNo,
                statusName = GetStatusText(r.Status), // 使用中文狀態
                originalStatus = r.Status, // 保留原始英文狀態供除錯
                reasonText = r.ReasonText,
                createdDate = r.CreatedDate.ToString("yyyy/MM/dd HH:mm"),
                orderSummary = new
                {
                    coupon = r.Order.CouponId.HasValue ? r.Order.CouponId.ToString() : "-",
                    discount = r.Order.DiscountTotal,
                    shippingFee = r.Order.ShippingFee,
                    total = orderTotal
                },
                orderItems = orderItems,
                rmaItems = rmaItems
            };

            return Json(new { ok = true, rma = dto });
        }

        // GET /ORD/Rma/DetailJson?id=123
        [HttpGet]
        public IActionResult DetailJson(int id)
        {
            var r = _db.OrdReturnRequests
                .AsNoTracking()
                .Include(x => x.Order)
                .Include(x => x.OrdReturnItems)
                    .ThenInclude(ri => ri.OrderItem)
                .FirstOrDefault(x => x.ReturnRequestId == id);

            if (r == null)
                return Json(new { ok = false, message = "RMA not found." });

            // 訂單品項：不要取 Product/Sku 導覽屬性的名稱，以 ID 組顯示字串即可
            var orderItems = _db.OrdOrderItems
                .AsNoTracking()
                .Where(oi => oi.OrderId == r.OrderId)
                .Select(oi => new
                {
                    product = "PID:" + oi.ProductId,
                    productId = oi.ProductId,
                    spec = "SKU:" + oi.SkuId,
                    skuId = oi.SkuId,
                    unitPrice = oi.UnitPrice,
                    qty = oi.Qty
                })
                .ToList();

            // RMA 明細：同樣只用 ID；退貨單價優先用 RefundUnitAmount，否則回退 OrderItem.UnitPrice
            var rmaItems = r.OrdReturnItems
                .OrderBy(i => i.RmaItemId)
                .Select(i => new
                {
                    product = "PID:" + i.OrderItem.ProductId,
                    productId = i.OrderItem.ProductId,
                    spec = "SKU:" + i.OrderItem.SkuId,
                    skuId = i.OrderItem.SkuId,
                    originQty = i.OrderItem.Qty,
                    applyQty = i.Qty,
                    returnUnitPrice = i.RefundUnitAmount ?? i.OrderItem.UnitPrice,
                    approvedQty = i.ApprovedQty,
                    refundQty = i.RefundQty,
                    reshipQty = i.ReshipQty
                })
                .ToList();

            // 訂單金額：避免用不存在的 CouponName；用 CouponId 或 '-' 顯示
            var subtotal = r.Order?.Subtotal ?? orderItems.Sum(x => x.unitPrice * x.qty);
            var discountTotal = r.Order?.DiscountTotal ?? 0m;
            var shippingFee = r.Order?.ShippingFee ?? 0m;
            var total = subtotal - discountTotal + shippingFee;

            var payload = new
            {
                order = new
                {
                    couponName = (r.Order?.CouponId != null) ? ("Coupon#" + r.Order.CouponId) : "-",
                    discountAmount = discountTotal,
                    shippingFee = shippingFee,
                    totalAmount = total,
                    items = orderItems
                },
                rma = new
                {
                    status = r.Status,
                    createdDate = r.CreatedDate,
                    reason = r.ReasonText ?? "-",
                    items = rmaItems
                }
            };

            return Json(payload);
        }



        // 核准（退款/補寄）
        [HttpPost]
        public IActionResult Approve(int id, string nextStatus)
        {
            var r = _db.OrdReturnRequests.FirstOrDefault(x => x.ReturnRequestId == id);
            if (r == null) return Json(new { ok = false, message = "RMA not found." });

            r.Status = nextStatus; // "refunding" / "reshipping"
            r.RevisedDate = DateTime.UtcNow;

            // 重要：批准時要更新 RMA 品項的核可數量
            var rmaItems = _db.OrdReturnItems.Where(x => x.ReturnRequestId == id).ToList();
            foreach (var item in rmaItems)
            {
                if (nextStatus == "refunding")
                {
                    item.ApprovedQty = item.Qty; // 將申請數量設為核可數量
                    item.RefundQty = item.Qty;   // 退款數量
                }
                else if (nextStatus == "reshipping")
                {
                    item.ApprovedQty = item.Qty; // 將申請數量設為核可數量
                    item.ReshipQty = item.Qty;   // 補寄數量
                }
            }

            _db.SaveChanges();
            return Json(new { ok = true, message = "已更新狀態。" });
        }

        // 駁回
        [HttpPost]
        public IActionResult Reject(int id, string? reason)
        {
            var r = _db.OrdReturnRequests.FirstOrDefault(x => x.ReturnRequestId == id);
            if (r == null) return Json(new { ok = false, message = "RMA not found." });

            r.Status = "rejected";
            if (!string.IsNullOrWhiteSpace(reason)) r.ReviewComment = reason;
            r.RevisedDate = DateTime.UtcNow;
            _db.SaveChanges();
            return Json(new { ok = true, message = "已駁回。" });
        }

        // 結單
        [HttpPost]
        public IActionResult Complete(int id)
        {
            var r = _db.OrdReturnRequests.FirstOrDefault(x => x.ReturnRequestId == id);
            if (r == null) return Json(new { ok = false, message = "RMA not found." });

            r.Status = "done";
            r.RevisedDate = DateTime.UtcNow;
            _db.SaveChanges();
            return Json(new { ok = true, message = "已結單。" });
        }
    }
}