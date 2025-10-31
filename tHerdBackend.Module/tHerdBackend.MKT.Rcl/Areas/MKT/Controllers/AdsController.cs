using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.SYS;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Repository.SYS;

namespace tHerdBackend.MKT.Rcl.Areas.MKT.Controllers
{
    [Area("MKT")]
    [Route("MKT/[controller]/[action]")]
    public class AdsController : Controller
    {
        private readonly tHerdDBContext _context;
        private readonly SysAssetFileRepository _fileRepo;

        public AdsController(tHerdDBContext context, SysAssetFileRepository fileRepo)
        {
            _context = context;
            _fileRepo = fileRepo;
        }

        [HttpGet]
        public IActionResult Index() => View();

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
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"上傳失敗：{ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(MktAd model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            model.Status = "aActive";
            model.CreatedDate = DateTime.Now;
            model.IsActive = true;

            _context.MktAds.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "廣告已新增" });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ad = await _context.MktAds.Include(a => a.Img).FirstOrDefaultAsync(a => a.AdId == id);
            if (ad == null) return NotFound();
            return PartialView("~/Areas/MKT/Views/Partial/_EditAdModal.cshtml", ad);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MktAd model)
        {
            var ad = await _context.MktAds.FindAsync(model.AdId);
            if (ad == null)
                return Json(new { success = false, message = "找不到廣告" });

            ad.Title = model.Title;
            ad.Content = model.Content;
            ad.ButtonText = model.ButtonText;
            ad.ButtonLink = model.ButtonLink;
            ad.StartDate = model.StartDate;
            ad.EndDate = model.EndDate;
            ad.AdType = model.AdType;
            ad.Status = "aActive";
            ad.IsActive = model.IsActive;
            ad.RevisedDate = DateTime.Now;

            if (model.ImgId != null)
                ad.ImgId = model.ImgId;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
