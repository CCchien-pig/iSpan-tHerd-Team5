using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.SYS;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.MKT.Rcl.Areas.MKT.Controllers
{
    [AllowAnonymous] // ✅ 避免未登入被導向登入頁
    [Area("MKT")]
    [Route("MKT/[controller]/[action]")]
    public class AdsController : Controller
    {
        private readonly tHerdDBContext _context;
        private readonly ISysAssetFileRepository 
            
            ;

        public AdsController(tHerdDBContext context, ISysAssetFileRepository fileRepo)
        {
            _context = context;
            _fileRepo = fileRepo;
        }

        [HttpGet]
        public IActionResult Index() => View();

        // ✅ 圖片上傳（上傳至雲端）
        [HttpPost]
        public async Task<IActionResult> UploadToCloud(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "請選擇圖片" });

            try
            {
                var dto = new AssetFileUploadDto
                {
                    ModuleId = "MKT",
                    ProgId = "Ad",
                    Meta = new List<AssetFileDetailsDto>
                    {
                        new AssetFileDetailsDto
                        {
                            File = file,
                            IsActive = true,
                            AltText = Path.GetFileNameWithoutExtension(file.FileName),
                            Caption = "廣告圖片"
                        }
                    }
                };

                var result = await _fileRepo.AddFilesAsync(dto);
                var json = JsonSerializer.Serialize(result);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                bool isSuccess = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                if (!isSuccess)
                {
                    var msg = root.TryGetProperty("message", out var msgProp)
                        ? msgProp.GetString()
                        : "上傳失敗";
                    return Json(new { success = false, message = msg });
                }

                if (!root.TryGetProperty("data", out var dataProp) || dataProp.ValueKind != JsonValueKind.Array)
                    return Json(new { success = false, message = "上傳失敗：未取得檔案資料" });

                var first = dataProp.EnumerateArray().FirstOrDefault();
                var fileId = first.TryGetProperty("FileId", out var fid) ? fid.GetInt32() : 0;
                var fileUrl = first.TryGetProperty("FileUrl", out var furl) ? furl.GetString() : "";

                return Json(new { success = true, message = "圖片上傳成功", fileId, fileUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"上傳失敗：{ex.Message}" });
            }
        }

        // ✅ 新增廣告畫面
        [HttpGet]
        public IActionResult Create() =>
            PartialView("~/Areas/MKT/Views/Partial/_CreateAdModal.cshtml");

        // ✅ 新增廣告（POST）
        [HttpPost]
        public async Task<IActionResult> Create(MktAd model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            if (model.StartDate == default)
                return Json(new { success = false, message = "開始日期不得為空" });

            if (model.EndDate == default)
                model.EndDate = null;

            model.Status = "aActive";
            model.CreatedDate = DateTime.Now;
            model.IsActive = true;

            _context.MktAds.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "廣告已新增" });
        }

        // ✅ FullCalendar 點擊編輯：支援 /MKT/Ads/Edit/1004
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var ad = await _context.MktAds
                .Include(a => a.Img)
                .FirstOrDefaultAsync(a => a.AdId == id);

            if (ad == null)
                return NotFound();

            // ✅ 防呆：確保 Img 不為 null
            ad.Img ??= new SysAssetFile();

            return PartialView("~/Areas/MKT/Views/Partial/_EditAdModal.cshtml", ad);
        }

        // ✅ 編輯廣告（POST）
        [HttpPost]
        public async Task<IActionResult> Edit(MktAd model)
        {
            var ad = await _context.MktAds.FirstOrDefaultAsync(a => a.AdId == model.AdId);
            if (ad == null)
                return Json(new { success = false, message = "找不到廣告" });

            if (model.StartDate == default)
                return Json(new { success = false, message = "開始日期不得為空" });

            ad.Title = model.Title;
            ad.Content = model.Content;
            ad.ButtonText = model.ButtonText;
            ad.ButtonLink = model.ButtonLink;
            ad.StartDate = model.StartDate;
            ad.EndDate = model.EndDate == default ? null : model.EndDate;
            ad.RevisedDate = DateTime.Now;

            // ✅ 僅當新圖片存在時才更新 ImgId
            if (model.ImgId.HasValue && model.ImgId > 0)
                ad.ImgId = model.ImgId;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "更新成功" });
        }

        // ✅ 取得單一廣告（JSON 給動態用）
        [HttpGet]
        public async Task<IActionResult> GetAdById(int id)
        {
            var ad = await _context.MktAds
                .Include(a => a.Img)
                .FirstOrDefaultAsync(a => a.AdId == id);

            if (ad == null)
                return Json(new { success = false, message = "找不到該廣告" });

            return Json(new
            {
                success = true,
                data = new
                {
                    ad.AdId,
                    ad.Title,
                    ad.Content,
                    ad.AdType,
                    ad.ButtonText,
                    ad.ButtonLink,
                    ad.StartDate,
                    ad.EndDate,
                    ad.ImgId,
                    ImgUrl = ad.Img?.FileUrl ?? ""
                }
            });
        }

        // ✅ FullCalendar 事件資料
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var ads = await _context.MktAds
                .Where(a => a.IsActive && a.Status == "aActive")
                .Select(a => new
                {
                    id = a.AdId,
                    title = a.Title,
                    start = a.StartDate.ToString("yyyy-MM-dd"),
                    end = a.EndDate.HasValue
                        ? a.EndDate.Value.AddDays(1).ToString("yyyy-MM-dd")
                        : a.StartDate.AddDays(1).ToString("yyyy-MM-dd"),
                    backgroundColor = a.AdType == "Popup" ? "#f57c00"
                        : a.AdType == "Marquee" ? "#43a047"
                        : "#6a1b9a",
                    borderColor = "#ffffff",
                    textColor = "#ffffff"
                })
                .ToListAsync();

            return Json(ads);
        }

        // ✅ 真實刪除（硬刪除）
        [HttpPost]
        [Route("/MKT/Ads/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ad = await _context.MktAds.FirstOrDefaultAsync(a => a.AdId == id);
                if (ad == null)
                    return Json(new { success = false, message = "找不到該廣告" });

                _context.MktAds.Remove(ad);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "廣告已永久刪除" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"刪除失敗：{ex.Message}" });
            }
        }

        // ✅ 總數查詢（給畫面顯示用）
        [HttpGet]
        public async Task<IActionResult> GetTotalCount()
        {
            var count = await _context.MktAds.CountAsync(a => a.IsActive && a.Status == "aActive");
            return Json(new { count });
        }
    }
}
