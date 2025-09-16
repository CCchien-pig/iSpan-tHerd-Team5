using FlexBackend.Infra.Models;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels.Returns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
    [Area("ORD")]
    public class RmaController : Controller
    {
        private readonly tHerdDBContext _db;

        public RmaController(tHerdDBContext db)
        {
            _db = db;
        }

        // ======================
        // RMA 列表
        // ======================
        public async Task<IActionResult> Index(string? group, string? keyword, int page = 1, int pageSize = 10)
        {
            var query = _db.OrdReturnRequests
                .Include(r => r.Order)
                .AsQueryable();

            // group 篩選
            if (!string.IsNullOrEmpty(group))
            {
                query = group.ToLower() switch
                {
                    "pending" => query.Where(r => r.Status == "pending" || r.Status == "review"),
                    "approved" => query.Where(r => r.Status == "reshipping" || r.Status == "refunding"),
                    "done" => query.Where(r => r.Status == "done"),
                    "rejected" => query.Where(r => r.Status == "rejected"),
                    _ => query
                };
            }

            // keyword 篩選
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(r =>
                    r.RmaId!.Contains(keyword) ||
                    r.Order.OrderNo.Contains(keyword));
            }

            var total = await query.CountAsync();

            var list = await query
                .OrderByDescending(r => r.ReturnRequestId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReturnListItemVM
                {
                    ReturnRequestId = r.ReturnRequestId,
                    RmaId = r.RmaId ?? "",
                    OrderId = r.OrderId,
                    OrderNo = r.Order.OrderNo,
                    Status = r.Status,
                    CreatedDate = r.CreatedDate,
                    ReasonText = r.ReasonText
                })
                .ToListAsync();

            var vm = new ReturnListPageVM
            {
                Group = group,
                Keyword = keyword,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = list
            };

            return View("Index", vm);
        }

        // ======================
        // RMA 明細
        // ======================
        [HttpGet]
        public async Task<IActionResult> GetReturnDetail(int id)
        {
            var r = await _db.OrdReturnRequests
                .Include(x => x.Order)
                .Include(x => x.OrdReturnItems)
                    .ThenInclude(i => i.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Include(x => x.OrdReturnItems)
                    .ThenInclude(i => i.OrderItem)
                    .ThenInclude(oi => oi.Sku)
                .FirstOrDefaultAsync(x => x.ReturnRequestId == id);

            if (r == null) return NotFound();

            var vm = new ReturnDetailVM
            {
                ReturnRequestId = r.ReturnRequestId,
                RmaId = r.RmaId ?? "",
                OrderId = r.OrderId,
                OrderNo = r.Order.OrderNo,
                StatusName = r.Status,
                CreatedDate = r.CreatedDate,
                ReasonText = r.ReasonText,
                Items = r.OrdReturnItems.Select(i => new ReturnDetailItemVM
                {
                    RmaItemId = i.RmaItemId,
                    OrderItemId = i.OrderItemId,
                    OrderQty = i.OrderItem.Qty,
                    RequestQty = i.Qty,
                    ApprovedQty = i.ApprovedQty,
                    RefundQty = i.RefundQty,
                    ReshipQty = i.ReshipQty,
                    RefundUnitAmount = i.RefundUnitAmount,
                    ProductName = i.OrderItem.Product.ProductName,
                    SkuSpec = i.OrderItem.Sku.SpecCode
                }).ToList()
            };

            return Json(new { ok = true, rma = vm, items = vm.Items });
        }

        // ======================
        // Approve (批准)
        // ======================
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string nextStatus)
        {
            var rr = await _db.OrdReturnRequests.FirstOrDefaultAsync(r => r.ReturnRequestId == id);
            if (rr == null) return Json(new { ok = false, message = "找不到退貨申請" });

            if (!(rr.Status == "pending" || rr.Status == "review"))
                return Json(new { ok = false, message = $"目前狀態為 {rr.Status} ，不可批准" });

            if (nextStatus != "refunding" && nextStatus != "reshipping")
                return Json(new { ok = false, message = "nextStatus 僅能為 refunding 或 reshipping" });

            rr.Status = nextStatus;

            if (string.IsNullOrEmpty(rr.RmaId))
            {
                rr.RmaId = $"RMA{DateTime.Now:yyyyMMdd}{Guid.NewGuid().ToString()[..4]}";
            }

            await _db.SaveChangesAsync();
            return Json(new { ok = true, message = "批准成功", rr.ReturnRequestId, rr.RmaId, rr.Status });
        }

        // ======================
        // Reject (駁回)
        // ======================
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var rr = await _db.OrdReturnRequests.FirstOrDefaultAsync(r => r.ReturnRequestId == id);
            if (rr == null) return Json(new { ok = false, message = "找不到退貨申請" });

            if (!(rr.Status == "pending" || rr.Status == "review"))
                return Json(new { ok = false, message = $"目前狀態為 {rr.Status} ，不可駁回" });

            rr.Status = "rejected";
            if (!string.IsNullOrWhiteSpace(reason))
                rr.ReasonText = reason;

            await _db.SaveChangesAsync();
            return Json(new { ok = true, message = "已駁回", rr.ReturnRequestId, rr.Status });
        }

        // ======================
        // Complete (完成退貨流程)
        // ======================
        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            var rr = await _db.OrdReturnRequests.FirstOrDefaultAsync(r => r.ReturnRequestId == id);
            if (rr == null) return Json(new { ok = false, message = "找不到退貨申請" });

            if (!(rr.Status == "refunding" || rr.Status == "reshipping"))
                return Json(new { ok = false, message = $"目前狀態為 {rr.Status} ，不可結單" });

            rr.Status = "done";
            await _db.SaveChangesAsync();

            return Json(new { ok = true, message = "已結單", rr.ReturnRequestId, rr.Status });
        }
    }
}
