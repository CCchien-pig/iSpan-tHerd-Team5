using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.Models;
using tHerdBackend.MKT.Rcl.Areas.MKT.Utils;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace tHerdBackend.MKT.Rcl.Areas.MKT.Controllers
{
    [Area("MKT")]
    public class AdsController : Controller
    {
        private readonly tHerdDBContext _context;
        private readonly Cloudinary _cloudinary;

        public AdsController(tHerdDBContext context)
        {
            _context = context;

            // ✅ 初始化 Cloudinary 設定（請替換成你的帳號資訊）
            var account = new Account(
                "你的_cloud_name",
                "你的_api_key",
                "你的_api_secret"
            );
            _cloudinary = new Cloudinary(account);
        }

        // 🏠 主頁面
        [HttpGet]
        public IActionResult Index() => View();

        // 📊 FullCalendar 資料
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

        // 📢 取得啟用中的廣告資料（含雲端圖片）
        [HttpGet]
        public async Task<IActionResult> GetActiveAds()
        {
            var ads = await _context.MktAds
                .Include(a => a.Img)
                .AsNoTracking()
                .Where(a => a.IsActive && a.StartDate <= DateTime.Now && a.EndDate >= DateTime.Now)
                .Select(a => new
                {
                    a.AdId,
                    a.Title,
                    a.Content,
                    a.AdType,
                    a.ButtonText,
                    a.ButtonLink,
                    ImgUrl = a.Img != null ? a.Img.FileUrl : null
                })
                .ToListAsync();

            return Json(ads);
        }

        // 📈 取得廣告總數
        [HttpGet]
        public async Task<IActionResult> GetTotalCount()
        {
            var count = await _context.MktAds.CountAsync();
            return Json(new { count });
        }

        // 🆕 新增廣告畫面
        [HttpGet]
        public IActionResult Create()
            => PartialView("~/Areas/MKT/Views/Partial/_CreateAdModal.cshtml");

        // 🆕 新增廣告（已改成使用 ImgId）
        [HttpPost]
        public async Task<IActionResult> Create(MktAd model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            model.Status = "aActive";
            model.CreatedDate = DateTime.Now;

            if (model.ImgId == null)
                return Json(new { success = false, message = "請上傳圖片後再儲存" });

            _context.MktAds.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ✏️ 編輯廣告畫面
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ad = await _context.MktAds
                .Include(a => a.Img)
                .FirstOrDefaultAsync(a => a.AdId == id);
            if (ad == null) return NotFound();
            return PartialView("~/Areas/MKT/Views/Partial/_EditAdModal.cshtml", ad);
        }

        // ✏️ 編輯廣告
        [HttpPost]
        public async Task<IActionResult> Edit(MktAd model)
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
            ad.ButtonText = model.ButtonText;
            ad.ButtonLink = model.ButtonLink;

            if (model.ImgId != null)
                ad.ImgId = model.ImgId;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // 🧩 上傳圖片至雲端 + 寫入 Sys_AssetFile
        [HttpPost]
        public async Task<IActionResult> UploadToCloud(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "未選擇檔案" });

            // ✅ 上傳到 Cloudinary
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = "tHerd/uploads/ads"
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            // ✅ 儲存進 Sys_AssetFile
            var asset = new SysAssetFile
            {
                FileKey = Guid.NewGuid().ToString(),
                IsExternal = true,
                FileUrl = uploadResult.SecureUrl.ToString(),
                FileExt = Path.GetExtension(file.FileName)?.TrimStart('.'),
                MimeType = file.ContentType,
                FileSizeBytes = file.Length,
                CreatedDate = DateTime.Now,
                IsActive = true,
                IsDeleted = false
            };

            _context.SysAssetFiles.Add(asset);
            await _context.SaveChangesAsync();

            return Json(new { success = true, fileId = asset.FileId, fileUrl = asset.FileUrl });
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
                    model.ImgId = null;
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
