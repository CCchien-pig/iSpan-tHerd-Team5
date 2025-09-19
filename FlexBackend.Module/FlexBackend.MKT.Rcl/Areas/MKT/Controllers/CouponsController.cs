using FlexBackend.Infra.Models;
using FlexBackend.MKT.Rcl.Areas.MKT.Utils;
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
                color = c.IsActive ? ColorHelper.RandomColor() : "#9e9e9e"
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
        public IActionResult GetActiveRules()
        {
            var rules = _context.MktCouponRules
                .Where(r => r.IsActive)
                .Select(r => new {
                    r.RuleId,
                    r.DefaultCondition,
                    r.CouponType
                })
                .ToList();

            return Json(rules);
        }


        // GET: 新增規則 Partial
        [HttpGet]
        public IActionResult CreateRule()
        {
            return PartialView("~/Areas/MKT/Views/Partial/_CouponRule.cshtml");
        }

        [HttpPost]
        public IActionResult CreateRule([FromBody] MktCouponRule model)
        {
            try
            {
                Console.WriteLine($"[DEBUG] 接收到的 model: {System.Text.Json.JsonSerializer.Serialize(model)}");

                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "資料驗證失敗" });

                model.CreatedDate = DateTime.Now;

                _context.MktCouponRules.Add(model);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] " + ex.ToString());
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }




        // GET: 修改規則 Partial
        [HttpGet]
        public IActionResult EditRule()
        {
            return PartialView("~/Areas/MKT/Views/Partial/_EditCouponRule.cshtml");
        }

        [HttpPost]
        public IActionResult EditRule([FromBody] MktCouponRule model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            var rule = _context.MktCouponRules.Find(model.RuleId);
            if (rule == null)
                return Json(new { success = false, message = "找不到規則" });

            // 更新欄位
            rule.CouponType = model.CouponType ?? rule.CouponType;  // ⚠️ 不能為 null
            rule.DefaultCondition = model.DefaultCondition;
            rule.Description = model.Description;
            rule.IsActive = model.IsActive;
            rule.Creator = model.Creator;
            rule.CreatedDate = DateTime.Now;

            _context.MktCouponRules.Update(rule);
            _context.SaveChanges();

            return Json(new { success = true });
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

        [HttpGet]
        public IActionResult GetRuleById(int id)
        {
            var rule = _context.MktCouponRules
                .Where(r => r.RuleId == id)
                .Select(r => new {
                    r.RuleId,
                    r.CouponType,
                    r.DefaultCondition,
                    r.Description,
                    r.IsActive,
                    r.Creator,
                    r.CreatedDate
                })
                .FirstOrDefault();

            if (rule == null)
                return Json(new { success = false, message = "找不到規則" });

            return Json(rule);
        }


    }
}
