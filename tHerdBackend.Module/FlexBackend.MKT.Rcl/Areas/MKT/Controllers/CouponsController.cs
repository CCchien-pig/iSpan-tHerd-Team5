using FlexBackend.Infra.Models;
using FlexBackend.MKT.Rcl.Areas.MKT.Utils;
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

        [HttpGet]
        [AllowAnonymous] // 測試用，允許未登入也能進
        public IActionResult Index()
        {
            return View();
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

            // 這裡關鍵，避免自動轉成 camelCase
            return Json(result, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });
        }


        [HttpPost]
        public IActionResult CreateCoupon([FromBody] MktCoupon model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "資料驗證失敗" });

                model.LeftQty = model.TotQty;
                model.CreatedDate = DateTime.Now;

                _context.MktCoupons.Add(model);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
    }
}
