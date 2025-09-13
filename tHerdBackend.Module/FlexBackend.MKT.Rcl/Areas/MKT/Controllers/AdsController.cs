using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.MKT.Rcl.Areas.MKT.Controllers
{
    [Area("MKT")]
    public class AdsController : Controller
    {
        private readonly tHerdDBContext _context;

        public AdsController(tHerdDBContext context)
        {
            _context = context;
        }

        // GET: Ads
        public IActionResult Index()
        {
            return View();
        }

        // GET: 取得廣告事件給 FullCalendar
        public async Task<IActionResult> GetAdsEvents()
        {
            var ads = await _context.MktAds
                .Where(a => a.IsActive)
                .Select(a => new
                {
                    id = a.AdId,
                    title = a.Title,
                    start = a.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = a.EndDate.HasValue ? a.EndDate.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null
                })
                .ToListAsync();

            return Json(ads);
        }

        // GET: 新增廣告局部檢視
        public IActionResult Create()
        {
            return PartialView("_CreateAdModal");
        }

        // POST: 新增廣告
        [HttpPost]
        public async Task<IActionResult> Create(MktAd model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.Creator = 1; // 測試用，可改成登入使用者ID
            model.CreatedDate = DateTime.Now;
            _context.MktAds.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: 編輯廣告局部檢視
        public async Task<IActionResult> Edit(int id)
        {
            var ad = await _context.MktAds.FindAsync(id);
            if (ad == null) return NotFound();
            return PartialView("_EditAdModal", ad);
        }

        // POST: 編輯廣告
        [HttpPost]
        public async Task<IActionResult> Edit(MktAd model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ad = await _context.MktAds.FindAsync(model.AdId);
            if (ad == null) return NotFound();

            ad.Title = model.Title;
            ad.Content = model.Content;
            ad.StartDate = model.StartDate;
            ad.EndDate = model.EndDate;
            ad.Status = model.Status;
            ad.IsActive = model.IsActive;
            ad.Reviser = 1; // 測試用
            ad.RevisedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: 刪除廣告
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ad = await _context.MktAds.FindAsync(id);
            if (ad == null) return NotFound();

            _context.MktAds.Remove(ad);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
