//using tHerdBackend.Infra.Models;
//using tHerdBackend.MKT.Rcl.Areas.MKT.Utils;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace tHerdBackend.MKT.Rcl.Areas.MKT.Controllers
//{
//    [Area("MKT")]
//    public class AdsController : Controller
//    {
//        private readonly tHerdDBContext _context;
//        public AdsController(tHerdDBContext context) { _context = context; }

//        [HttpGet]
//        public IActionResult Index() { return View(); }

//        [HttpGet]
//        public async Task<IActionResult> GetEvents()
//        {
//            var ads = await _context.MktAds.AsNoTracking().ToListAsync();
//            var events = ads.Select(a => new {
//                id = a.AdId,
//                title = a.Title,
//                start = a.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
//                end = a.EndDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
//                color = ColorHelper.RandomColor()
//            }).ToList();
//            return Json(events);
//        }

//        [HttpGet]
//        public IActionResult Create()
//            => PartialView("~/Areas/MKT/Views/Partial/_CreateAdModal.cshtml");

//        [HttpPost]
//        public async Task<IActionResult> Create(MktAd model)
//        {
//            if (!ModelState.IsValid)
//                return Json(new { success = false, message = "資料驗證失敗" });

//            if (model.Creator <= 0)
//                return Json(new { success = false, message = "建檔人員不可為空" });

//            if (model.Status == "aInactive") model.IsActive = false;
//            model.CreatedDate = DateTime.Now;

//            _context.MktAds.Add(model);
//            await _context.SaveChangesAsync();
//            return Json(new { success = true });
//        }

//        [HttpGet]
//        public async Task<IActionResult> Edit(int id)
//        {
//            var ad = await _context.MktAds.FindAsync(id);
//            if (ad == null) return NotFound();
//            return PartialView("~/Areas/MKT/Views/Partial/_EditAdModal.cshtml", ad);
//        }

//        [HttpPost]
//        public async Task<IActionResult> Edit(MktAd model)
//        {
//            var ad = await _context.MktAds.FindAsync(model.AdId);
//            if (ad == null) return Json(new { success = false, message = "找不到廣告" });

//            ad.Title = model.Title;
//            ad.Content = model.Content;
//            ad.StartDate = model.StartDate;
//            ad.EndDate = model.EndDate;
//            ad.Status = string.IsNullOrEmpty(model.Status) ? "aActive" : model.Status;
//            ad.IsActive = ad.Status == "aInactive" ? false : model.IsActive;
//            ad.Creator = model.Creator; // 可編輯
//            ad.Reviser = 1;
//            ad.RevisedDate = DateTime.Now;

//            await _context.SaveChangesAsync();
//            return Json(new { success = true });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var ad = await _context.MktAds.FindAsync(id);
//            if (ad == null) return Json(new { success = false, message = "找不到廣告" });

//            _context.MktAds.Remove(ad);
//            await _context.SaveChangesAsync();
//            return Json(new { success = true });
//        }

//        [HttpGet]
//        public IActionResult GetTotalCount()
//        {
//            var count = _context.MktAds.Count();
//            return Json(new { count });
//        }
//    }
//}
