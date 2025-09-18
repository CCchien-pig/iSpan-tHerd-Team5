using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.MKT.Rcl.Areas.MKT.Controllers
{
    [Area("MKT")]
    public class CouponsController : Controller
    {
        private readonly tHerdDBContext _context;
        public CouponsController(tHerdDBContext context)
        {
            _context = context;
        }

        // 頁面 Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var campaigns = await _context.MktCampaigns
                    .AsNoTracking()
                    .ToListAsync();

                ViewBag.Campaigns = campaigns;
                return View();
            }
            catch (Exception ex)
            {
                // 除錯時先這樣寫，正式環境請用 Log
                return Content("載入 Coupons 頁面時發生錯誤：" + ex.Message);
            }
        }

        // 取得日曆事件
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var coupons = await _context.MktCoupons
                .AsNoTracking()
                .Include(c => c.Campaign)
                .ToListAsync();

            var events = coupons.Select(c => new
            {
                id = c.CouponId,
                title = c.CouponName,
                start = c.StartDate,
                end = c.EndDate,
                color = c.IsActive ? "#6a1b9a" : "#9e9e9e"
            });

            return Json(events);
        }

        // 取得優惠券總數
        [HttpGet]
        public async Task<IActionResult> GetTotalCount()
        {
            var count = await _context.MktCoupons.CountAsync();
            return Json(new { count });
        }

        // 依 ID 取得優惠券
        [HttpGet]
        public async Task<IActionResult> GetCouponById(int id)
        {
            var coupon = await _context.MktCoupons
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CouponId == id);

            if (coupon == null)
                return Json(null);

            return Json(coupon);
        }

        // 取得有效規則
        [HttpGet]
        public async Task<IActionResult> GetActiveRules()
        {
            var rules = await _context.MktCouponRules
                .Where(r => r.IsActive)
                .Select(r => new
                {
                    ruleId = r.RuleId,
                    defaultCondition = r.DefaultCondition
                })
                .ToListAsync();

            return Json(rules);
        }

        // GET: 新增規則 Partial
        [HttpGet]
        public IActionResult CreateRulePartial()
        {
            return PartialView("~/Areas/MKT/Views/Partial/_CreateCouponRuleModal.cshtml");
        }

        // GET: 修改規則 Partial
        [HttpGet]
        public IActionResult EditRulePartial()
        {
            return PartialView("~/Areas/MKT/Views/Partial/_EditCouponRuleModal.cshtml");
        }

        [HttpPost]
        public IActionResult CreateCoupon([FromBody] MktCoupon model)
        {
            Console.WriteLine($"[DEBUG] CouponCode={model.CouponCode}, Raw Request={HttpContext.Request.Body}");
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            // 強制 Trim
            model.CouponCode = model.CouponCode?.Trim();

            if (string.IsNullOrWhiteSpace(model.CouponCode))
                return Json(new { success = false, message = "優惠券代碼 不可以為空" });

            var campaign = _context.MktCampaigns.Find(model.CampaignId);
            if (campaign == null)
                return Json(new { success = false, message = "找不到對應的活動" });

            // ===== 時間驗證 =====
            if (model.StartDate < campaign.StartDate)
                return Json(new { success = false, message = "優惠券開始日期 不可以早於 活動開始日期" });

            if (campaign.EndDate.HasValue && model.StartDate > campaign.EndDate.Value)
                return Json(new { success = false, message = "優惠券開始日期 不可以晚於 活動結束日期" });

            if (model.EndDate.HasValue && campaign.EndDate.HasValue && model.EndDate > campaign.EndDate.Value)
                return Json(new { success = false, message = "優惠券結束日期 不可以晚於 活動結束日期" });

            if (model.EndDate.HasValue && model.StartDate > model.EndDate.Value)
                return Json(new { success = false, message = "優惠券開始日期 不可以晚於 優惠券結束日期" });

            model.CreatedDate = DateTime.Now;
            model.LeftQty = model.TotQty;

            _context.MktCoupons.Add(model);
            _context.SaveChanges();

            return Json(new { success = true });
        }


        // POST: 修改優惠券
        [HttpPost]
        public IActionResult UpdateCoupon([FromBody] MktCoupon model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            var campaign = _context.MktCampaigns.Find(model.CampaignId);
            if (campaign == null)
                return Json(new { success = false, message = "找不到對應的活動" });

            // ===== 時間驗證 =====
            if (model.StartDate < campaign.StartDate)
                return Json(new { success = false, message = "優惠券開始日期 不可以早於 活動開始日期" });

            if (campaign.EndDate.HasValue && model.StartDate > campaign.EndDate.Value)
                return Json(new { success = false, message = "優惠券開始日期 不可以晚於 活動結束日期" });

            if (model.EndDate.HasValue && model.EndDate.Value < campaign.StartDate)
                return Json(new { success = false, message = "優惠券結束日期 不可以早於 活動開始日期" });

            if (model.EndDate.HasValue && campaign.EndDate.HasValue && model.EndDate > campaign.EndDate.Value)
                return Json(new { success = false, message = "優惠券結束日期 不可以晚於 活動結束日期" });

            if (model.EndDate.HasValue && model.StartDate > model.EndDate.Value)
                return Json(new { success = false, message = "優惠券開始日期 不可以晚於 優惠券結束日期" });

            // ===== 必填檢查 =====
            if (string.IsNullOrWhiteSpace(model.CouponCode))
                return Json(new { success = false, message = "優惠券代碼 不可以為空" });

            _context.MktCoupons.Update(model);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        // POST: 刪除優惠券
        [HttpPost]
        public IActionResult DeleteCoupon(int id)
        {
            var coupon = _context.MktCoupons.Find(id);
            if (coupon == null)
                return Json(new { success = false, message = "找不到優惠券" });

            _context.MktCoupons.Remove(coupon);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}
