using FlexBackend.Infra.Models;
using FlexBackend.MKT.Rcl.Areas.MKT.Utils;
using FlexBackend.MKT.Rcl.Areas.MKT.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.MKT.Rcl.Areas.MKT.Controllers
{
    [Area("MKT")]
    [Route("MKT/[controller]/[action]")]
    public class CouponsController : Controller
    {
        private readonly tHerdDBContext _context;
        public CouponsController(tHerdDBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var campaigns = _context.MktCampaigns
                .Select(c => new CampaignDTO { CampaignId = c.CampaignId, CampaignName = c.CampaignName })
                .ToList();

            ViewBag.Campaigns = campaigns;

            // 將一個空的 Coupon 傳給 Partial 避免 null
            var couponModel = new MktCoupon();
            return View(couponModel);
        }


        // 新增 GET 方法，給新增優惠券的 Modal 用
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // 取得所有活動列表
            var campaigns = await _context.MktCampaigns
                                          .Select(c => new { c.CampaignId, c.CampaignName })
                                          .ToListAsync();
            ViewBag.Campaigns = campaigns;

            return View(new MktCoupon());
        }

        // 取得日曆事件
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvents()
        {
            var coupons = await _context.MktCoupons.AsNoTracking().ToListAsync();
            var events = coupons.Select(c => new
            {
                id = c.CouponId,
                title = c.CouponName,
                start = c.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = c.EndDate.HasValue ? c.EndDate.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                color = ColorHelper.RandomColor()
            }).ToList();

            return Json(events);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCouponById(int id)
        {
            var c = await _context.MktCoupons.AsNoTracking().FirstOrDefaultAsync(x => x.CouponId == id);
            if (c == null) return NotFound();

            var result = new
            {
                c.CouponId,
                c.CampaignId,
                c.RuleId,
                c.CouponName,
                c.CouponCode,
                c.Status,
                StartDate = c.StartDate.ToString("yyyy-MM-ddTHH:mm"),
                EndDate = c.EndDate.HasValue ? c.EndDate.Value.ToString("yyyy-MM-ddTHH:mm") : null,
                c.DiscountAmount,
                c.DiscountPercent,
                c.TotQty,
                c.LeftQty,
                c.UserLimit,
                c.ValidHours,
                c.IsActive,
                c.Creator,
                CreatedDate = c.CreatedDate.ToString("yyyy-MM-ddTHH:mm")
            };

            return Json(result, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });
        }

        [HttpPost]
        public IActionResult CreateCoupon([FromBody] MktCoupon coupon)
        {
            try
            {
                // 新增 LeftQty = TotQty
                coupon.LeftQty = coupon.TotQty;
                coupon.CreatedDate = DateTime.Now;

                _context.MktCoupons.Add(coupon);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // 取得最內層 exception 訊息
                var inner = ex;
                while (inner.InnerException != null) inner = inner.InnerException;

                return Json(new { success = false, message = inner.Message });
            }
        }


        [HttpPost]
        public IActionResult UpdateCoupon([FromBody] MktCoupon model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            _context.MktCoupons.Update(model);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost("{id}")]
        public IActionResult DeleteCoupon(int id)
        {
            var c = _context.MktCoupons.Find(id);
            if (c == null) return Json(new { success = false, message = "找不到優惠券" });
            _context.MktCoupons.Remove(c);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetTotalCount()
        {
            var count = _context.MktCoupons.Count();
            return Json(new { count });
        }


        [HttpGet]
        public IActionResult CreateRulePartial()
        {
            try
            {
                var model = new MktCouponRule(); // 初始化模型
                return PartialView("~/Areas/MKT/Views/Partial/_CouponRule.cshtml", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }


        [HttpPost]
        public IActionResult CreateRule([FromBody] MktCouponRule model)
        {
            if (model == null)
                return Json(new { success = false, message = "接收到空資料" });

            // 基本驗證
            if (string.IsNullOrWhiteSpace(model.CouponType))
                return Json(new { success = false, message = "CouponType 必填" });

            if (string.IsNullOrWhiteSpace(model.DefaultCondition))
                model.DefaultCondition = "";

            if (string.IsNullOrWhiteSpace(model.Description))
                model.Description = "";

            // 設定建立時間
            model.CreatedDate = DateTime.Now;

            // 若前端沒有傳 IsActive/Creator，設定預設值
            model.IsActive = model.IsActive;
            model.Creator = model.Creator > 0 ? model.Creator : 1;

            try
            {
                _context.MktCouponRules.Add(model);
                _context.SaveChanges();
                return Json(new { success = true, message = "新增成功", ruleId = model.RuleId });
            }
            catch (Exception ex)
            {
                // 取 inner exception 最底層訊息
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = errorMessage });
            }
        }

        [HttpGet]
        public IActionResult EditRulePartial(int id = 1000)
        {
            var rule = _context.MktCouponRules.FirstOrDefault(r => r.RuleId == id);
            if (rule == null) return NotFound();

            // 把所有 DefaultCondition 抓出來給下拉選單用
            var allConditions = _context.MktCouponRules
                .Select(r => r.DefaultCondition)
                .Where(c => c != null && c != "")
                .Distinct()
                .ToList();

            ViewBag.DefaultConditions = allConditions;

            return PartialView("~/Areas/MKT/Views/Partial/_EditCouponRule.cshtml", rule);
        }


        [HttpGet]
        public IActionResult GetActiveRules()
        {
            var rules = _context.MktCouponRules
                .Select(r => new {
                    r.RuleId,
                    r.DefaultCondition,
                    r.IsActive,
                    r.Description
                })
                .ToList();

            return Json(rules);
        }


        // 取得單一規則詳細資料（選擇下拉用）
        [HttpGet]
        public IActionResult GetRuleById(int id)
        {
            var rule = _context.MktCouponRules
                .Where(r => r.RuleId == id)
                .Select(r => new {
                    r.RuleId,
                    r.DefaultCondition,
                    r.Description,
                    r.IsActive
                }).FirstOrDefault();

            if (rule == null) return NotFound();
            return Json(rule);
        }

        // 更新規則
        [HttpPost]
        public IActionResult UpdateRule([FromBody] MktCouponRule model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "資料驗證失敗" });

            var rule = _context.MktCouponRules.FirstOrDefault(r => r.RuleId == model.RuleId);
            if (rule == null) return Json(new { success = false, message = "找不到規則" });

            // 只更新可編輯欄位
            rule.Description = model.Description;
            rule.IsActive = model.IsActive;

            try
            {
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // 保留驗證
        public IActionResult DeleteRule(int id)
        {
            try
            {
                var rule = _context.MktCouponRules.FirstOrDefault(r => r.RuleId == id);
                if (rule == null)
                    return Json(new { success = false, message = "找不到此規則" });

                _context.MktCouponRules.Remove(rule);
                _context.SaveChanges();

                return Json(new { success = true, message = "刪除成功" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }

}