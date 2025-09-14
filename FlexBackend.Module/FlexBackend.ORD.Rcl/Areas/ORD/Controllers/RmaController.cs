using FlexBackend.Infra.Models; // tHerdDBContext + Entities
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
    [Area("ORD")]
    public class RmaController : Controller
    {
        private readonly tHerdDBContext _db;
        public RmaController(tHerdDBContext db) => _db = db;

        // ===== SYS_CODE 對照（依你提供的表）=====
        private const string MOD_ORD = "ORD";
        private const string CID_PAYMENT = "04"; // Payment.Status
        private const string CID_RR_STATUS = "06"; // ReturnRequest.Status
        private const string CID_REQ_TYPE = "08"; // ReturnRequest.RequestType
        private const string CID_REASON = "09"; // ReturnRequest.ReasonCode
        private const string CID_REFUND_SCOPE = "10"; // ReturnRequest.RefundScope

        // ===== ReturnRequest 狀態值（CodeNo）=====
        private const string STATUS_PENDING = "pending";
        private const string STATUS_REJECTED = "rejected";

        public IActionResult Index() => View();

        // 分頁①：退貨申請（待審核）
        [HttpGet]
        public async Task<IActionResult> GetRequests()
        {
            var codeIds = new List<string> { CID_REQ_TYPE, CID_REFUND_SCOPE, CID_REASON, CID_RR_STATUS, CID_PAYMENT };
            var sys = await GetsysStatuses(MOD_ORD, codeIds);
            var dicReqType = StatusDictionary(CID_REQ_TYPE, sys);
            var dicScope = StatusDictionary(CID_REFUND_SCOPE, sys);
            var dicReason = StatusDictionary(CID_REASON, sys);
            var dicStatus = StatusDictionary(CID_RR_STATUS, sys);

            var list = await (
                from rr in _db.OrdReturnRequests.AsNoTracking()
                join o in _db.OrdOrders.AsNoTracking() on rr.OrderId equals o.OrderId
                where rr.Status == STATUS_PENDING
                select new RmaRequestRowVM
                {
                    ReturnRequestId = rr.ReturnRequestId,
                    RmaId = rr.RmaId,      // string?
                    OrderId = rr.OrderId,
                    OrderNo = o.OrderNo,

                    RequestType = rr.RequestType,
                    RefundScope = rr.RefundScope,
                    ReasonCode = rr.ReasonCode,
                    ReasonText = rr.ReasonText,
                    Status = rr.Status,

                    CreatorId = rr.Creator,    // int?
                    CreatedDate = rr.CreatedDate,
                    RevisedDate = rr.RevisedDate,

                    ItemCount = _db.OrdReturnItems.Count(x => x.ReturnRequestId == rr.ReturnRequestId),
                    HasRefundPayment = _db.OrdPayments.Any(p => p.ReturnRequestId == rr.ReturnRequestId),

                    RequestTypeName = GetSysText(rr.RequestType, dicReqType),
                    RefundScopeName = GetSysText(rr.RefundScope, dicScope),
                    ReasonName = GetSysText(rr.ReasonCode, dicReason),
                    StatusName = GetSysText(rr.Status, dicStatus)
                }
            ).ToListAsync();

            // （可選）CreatorName：之後若有會員表再做字典轉
            // var creatorIds = list.Where(x => x.CreatorId.HasValue).Select(x => x.CreatorId!.Value).Distinct().ToList();
            // var memberDict = await GetMemberNameDictAsync(creatorIds);
            // foreach (var r in list)
            //     r.CreatorName = (r.CreatorId.HasValue && memberDict.TryGetValue(r.CreatorId.Value, out var nm)) ? nm : r.CreatorId?.ToString();

            return Json(new { data = list });
        }

        // 分頁②：RMA 清單（已核准流程：非 pending / 非 rejected）
        [HttpGet]
        public async Task<IActionResult> GetRmaList()
        {
            var sys = await GetsysStatuses(MOD_ORD, new List<string> { CID_RR_STATUS, CID_PAYMENT });
            var dicStatus = StatusDictionary(CID_RR_STATUS, sys);
            var dicPay = StatusDictionary(CID_PAYMENT, sys);

            // 先撈主檔
            var rows = await (
                from rr in _db.OrdReturnRequests.AsNoTracking()
                join o in _db.OrdOrders.AsNoTracking() on rr.OrderId equals o.OrderId
                where rr.Status != STATUS_PENDING && rr.Status != STATUS_REJECTED
                select new RmaListRowVM
                {
                    ReturnRequestId = rr.ReturnRequestId,
                    RmaId = rr.RmaId,
                    OrderId = rr.OrderId,
                    OrderNo = o.OrderNo,

                    ReviewerId = rr.Reviewer,
                    ReviewedDate = rr.ReviewedDate,
                    ReviewComment = rr.ReviewedDate != null ? rr.ReviewComment : null,

                    Status = rr.Status,
                    StatusName = GetSysText(rr.Status, dicStatus),

                    ItemCount = _db.OrdReturnItems.Count(x => x.ReturnRequestId == rr.ReturnRequestId)
                }
            ).ToListAsync();

            // 再一次性撈退款紀錄並 Group
            var rrIds = rows.Select(r => r.ReturnRequestId).Distinct().ToList();
            var payList = await _db.OrdPayments.AsNoTracking()
                .Where(p => p.ReturnRequestId != null && rrIds.Contains(p.ReturnRequestId.Value))
                .Select(p => new {
                    p.ReturnRequestId,
                    VM = new RmaRefundPaymentVM
                    {
                        PaymentId = p.PaymentId,
                        OrderId = p.OrderId,
                        ReturnRequestId = p.ReturnRequestId,
                        PaymentConfigId = p.PaymentConfigId,
                        Amount = p.Amount,
                        Status = p.Status,
                        StatusName = "", // 先佔位，下面再補中文
                        TradeNo = p.TradeNo,
                        MerchantTradeNo = p.MerchantTradeNo,
                        TradeDate = p.TradeDate,
                        CreatedDate = p.CreatedDate
                    }
                })
                .ToListAsync();

            var payGrp = payList.GroupBy(x => x.ReturnRequestId!.Value).ToDictionary(g => g.Key, g => g.Select(x => x.VM).ToList());
            foreach (var r in rows)
            {
                if (payGrp.TryGetValue(r.ReturnRequestId, out var lst))
                {
                    foreach (var p in lst)
                        p.StatusName = GetSysText(p.Status, dicPay);
                    r.RefundPayments = lst;
                }
            }

            // ReviewerId -> ReviewerName（先保底以數字顯示；你之後換成人員表）
            var reviewerIds = rows.Where(x => x.ReviewerId.HasValue).Select(x => x.ReviewerId!.Value).Distinct().ToList();
            var reviewerDict = await GetReviewerNameDictAsync(reviewerIds);
            foreach (var r in rows)
            {
                if (r.ReviewerId.HasValue && reviewerDict.TryGetValue(r.ReviewerId.Value, out var nm))
                    r.ReviewerName = nm;
                else
                    r.ReviewerName = r.ReviewerId?.ToString() ?? "";
            }

            return Json(new { data = rows });
        }

        // 明細（ORD_ReturnItem）
        [HttpGet]
        public async Task<IActionResult> GetRequestItems(int requestId)
        {
            var items = await (
                from ri in _db.OrdReturnItems.AsNoTracking()
                where ri.ReturnRequestId == requestId
                select new RmaItemVM
                {
                    RmaItemId = ri.RmaItemId,
                    ReturnRequestId = ri.ReturnRequestId,
                    OrderId = ri.OrderId,
                    OrderItemId = ri.OrderItemId,
                    Qty = ri.Qty,
                    ApprovedQty = ri.ApprovedQty,
                    RefundQty = ri.RefundQty,
                    ReshipQty = ri.ReshipQty,
                    RefundUnitAmount = ri.RefundUnitAmount,
                    CreatedDate = ri.CreatedDate,
                    RevisedDate = ri.RevisedDate
                }
            ).ToListAsync();

            return Json(new { data = items });
        }

        // 審核（核准→進流程、駁回）
        [HttpPost]
        public async Task<IActionResult> Decide([FromForm] int requestId, [FromForm] bool approve, [FromForm] string? note)
        {
            var req = await _db.OrdReturnRequests.FirstOrDefaultAsync(x => x.ReturnRequestId == requestId);
            if (req == null) return Json(new { success = false, message = "找不到申請單" });
            if (req.Status != STATUS_PENDING) return Json(new { success = false, message = "此申請已處理" });

            try
            {
                // TODO: 若需要紀錄審核者，把登入者 int Id 填進去（保留為 null 亦可）
                // req.Reviewer = GetCurrentUserIdOrNull();

                req.ReviewedDate = DateTime.Now;
                req.ReviewComment = note ?? string.Empty;

                if (approve)
                {
                    req.Status = "review";
                    var items = await _db.OrdReturnItems
                                         .Where(x => x.ReturnRequestId == req.ReturnRequestId)
                                         .ToListAsync();

                    foreach (var it in items)
                        if (it.ApprovedQty == 0) it.ApprovedQty = it.Qty;
                }

                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== 共用：SysCode / 名稱字典 =====

        private class SelectOption { public string Value { get; set; } = ""; public string Text { get; set; } = ""; }

        private static string GetSysText(string? codeNo, IEnumerable<SelectOption> options)
        {
            if (string.IsNullOrWhiteSpace(codeNo)) return "";
            var m = options.FirstOrDefault(s => s.Value == codeNo);
            return m?.Text ?? codeNo;
        }

        private IEnumerable<SelectOption> StatusDictionary(string codeId, List<SysCode> sysStatuses)
        {
            return sysStatuses
                .Where(c => c.CodeId == codeId)
                .Select(x => new SelectOption { Value = x.CodeNo, Text = x.CodeDesc })
                .ToList();
        }

        private async Task<List<SysCode>> GetsysStatuses(string moduleId, List<string> codeIds)
        {
            return await _db.SysCodes
                .Where(c => c.ModuleId == moduleId && codeIds.Contains(c.CodeId) && c.IsActive)
                .ToListAsync();
        }

        // Reviewer 名稱（先給保底；之後換成你的使用者/員工表）
        private async Task<Dictionary<int, string>> GetReviewerNameDictAsync(IEnumerable<int> ids)
        {
            var dict = new Dictionary<int, string>();
            var list = ids?.Distinct().ToList() ?? new List<int>();
            if (list.Count == 0) return dict;

            // TODO: 這裡改為你的「使用者/員工」資料表，例如：
            // return await _db.AdmUsers.Where(u => list.Contains(u.UserId))
            //     .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            // 保底顯示（避免未指派變數）
            foreach (var id in list) dict[id] = id.ToString();
            await Task.CompletedTask;
            return dict;
        }

        // （可選）會員名稱字典
        private async Task<Dictionary<int, string>> GetMemberNameDictAsync(IEnumerable<int> ids)
        {
            var dict = new Dictionary<int, string>();
            var list = ids?.Distinct().ToList() ?? new List<int>();
            if (list.Count == 0) return dict;

            // TODO: 換成你的會員表（UserNumberId -> Name）
            foreach (var id in list) dict[id] = id.ToString();
            await Task.CompletedTask;
            return dict;
        }

        // （可選）把登入者 Id 轉成 int?
        // private int? GetCurrentUserIdOrNull()
        // {
        //     var candidates = new[] { System.Security.Claims.ClaimTypes.NameIdentifier, "uid", "sub" };
        //     foreach (var t in candidates)
        //         if (int.TryParse(User.FindFirst(t)?.Value, out var id)) return id;
        //     if (int.TryParse(User.Identity?.Name, out var nameId)) return nameId;
        //     return null;
        // }
    }
}
