using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.Models;
using tHerdBackend.MKT.Rcl.Areas.MKT.Utils;

namespace tHerdBackend.MKT.Rcl.Areas.MKT.Controllers
{
    [Area("MKT")]
    public class AdsController : Controller
    {
        private readonly tHerdDBContext _context;
        public AdsController(tHerdDBContext context) { _context = context; }

        // 🏠 主頁面
        [HttpGet]
        public IActionResult Index() => View();

        // 📊 取得 FullCalendar 所需的事件資料
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var ads = await _context.MktAds.AsNoTracking().ToListAsync();
            var events = ads.Select(a => new
            {
                id = a.AdId,
                title = a.Title,
                start = a.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = a.EndDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                adType = a.AdType,
                color = ColorHelper.RandomColor(),
                isActive = a.IsActive
            }).ToList();

            return Json(events);
        }

        // 📈 取得廣告總數（新增這段）
        [HttpGet]
        public async Task<IActionResult> GetTotalCount()
        {
            // 若只想算啟用中的可改成：CountAsync(a => a.IsActive)
            var count = await _context.MktAds.CountAsync();
            return Json(new { count });
        }

        // 🆕 建立廣告 Modal
        [HttpGet]
        public IActionResult Create()
            => PartialView("~/Areas/MKT/Views/Partial/_CreateAdModal.cshtml");

        // 🆕 建立廣告動作
        [HttpPost]
        public async Task<IActionResult> Create(MktAd model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            model.Status = "aActive";
            model.CreatedDate = DateTime.Now;

            // ✅ 儲存圖片（如果有上傳）
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // ✅ 儲存相對路徑到資料庫
                model.ImgPath = $"/uploads/ads/{fileName}";
            }

            _context.MktAds.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        // ✏️ 編輯廣告 Modal
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ad = await _context.MktAds.FindAsync(id);
            if (ad == null) return NotFound();
            return PartialView("~/Areas/MKT/Views/Partial/_EditAdModal.cshtml", ad);
        }

        // ✏️ 編輯廣告動作
        [HttpPost]
        public async Task<IActionResult> Edit(MktAd model, IFormFile? imageFile)
        {
            var ad = await _context.MktAds.FindAsync(model.AdId);
            if (ad == null)
                return Json(new { success = false, message = "找不到廣告" });

            ad.Title = model.Title;
            ad.Content = model.Content;
            ad.StartDate = model.StartDate;
            ad.EndDate = model.EndDate;
            ad.Status = string.IsNullOrEmpty(model.Status) ? "aActive" : model.Status;
            ad.IsActive = model.IsActive;
            ad.Creator = model.Creator;
            ad.AdType = model.AdType;
            ad.RevisedDate = DateTime.Now;
            ad.Reviser = 1;

            // ✅ 若有新圖片，替換路徑
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // ✅ 若原本有舊圖片，可考慮刪除（可選）
                if (!string.IsNullOrEmpty(ad.ImgPath))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", ad.ImgPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                ad.ImgPath = $"/uploads/ads/{fileName}";
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        // ❌ 刪除廣告
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ad = await _context.MktAds.FindAsync(id);
            if (ad == null)
                return Json(new { success = false, message = "找不到廣告" });

            _context.MktAds.Remove(ad);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // 🔄 啟用／停用切換
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var ad = await _context.MktAds.FindAsync(id);
            if (ad == null)
                return Json(new { success = false, message = "找不到廣告" });

            ad.IsActive = !ad.IsActive;
            ad.Status = ad.IsActive ? "aActive" : "aInactive";
            ad.RevisedDate = DateTime.Now;
            ad.Reviser = 1;

            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = ad.IsActive });
        }

        // 🧩 根據廣告類型補預設值
        private void ApplyDefaultValuesByAdType(MktAd model)
        {
            switch (model.AdType)
            {
                case "Popup":
                    model.ButtonText = "了解更多";
                    model.ButtonLink = "#";
                    model.Content = null;
                    break;

                case "Marquee":
                    model.ButtonText = "了解更多";
                    model.ButtonLink = "#";
                    model.ImgPath = null;
                    break;

                case "Carousel":
                default:
                    if (string.IsNullOrWhiteSpace(model.ButtonText))
                        model.ButtonText = "了解更多";
                    if (string.IsNullOrWhiteSpace(model.ButtonLink))
                        model.ButtonLink = "#";
                    break;
            }
        }
    }
}
