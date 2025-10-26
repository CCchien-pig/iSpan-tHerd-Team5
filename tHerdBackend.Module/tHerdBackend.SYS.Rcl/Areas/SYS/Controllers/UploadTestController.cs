using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.Interfaces.SYS;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    [Area("SYS")]
    public class UploadTestController : Controller
    {
        private readonly ISysAssetFileService _frepo;
        private readonly IWebHostEnvironment _env;

        public UploadTestController(ISysAssetFileService frepo, IWebHostEnvironment env)
        {
            _frepo = frepo;
            _env = env;
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="uploadDto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Index(AssetFileUploadDto uploadDto)
        {
            if (uploadDto.Meta == null || uploadDto.Meta.Count == 0)
            {
                ViewBag.Message = "請至少選擇一個檔案";
                var files = await _frepo.GetFilesByProg(uploadDto.ModuleId, uploadDto.ProgId);
                return View(files);
            }

            // 檢查前端有無傳入模組與程式代號
            if (string.IsNullOrWhiteSpace(uploadDto.ModuleId) ||
                string.IsNullOrWhiteSpace(uploadDto.ProgId))
            {
                TempData["ErrorMessage"] = "上傳來源缺少必要資訊（模組代號 或 程式代號）";
                return RedirectToAction("Index");
            }

            try
            {
                if (uploadDto.IsExternal)
                {
                    // Cloudinary 上傳
                    var imageUrls = await _frepo.AddImages(uploadDto);
                    TempData["SuccessMessage"] = $"已成功上傳 Cloudinary";
                }
                else
                {
                    // 本地上傳
                    var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", uploadDto.ModuleId, uploadDto.ProgId);
                    if (!Directory.Exists(uploadRoot))
                        Directory.CreateDirectory(uploadRoot);

                    int count = 0;

                    foreach (var meta in uploadDto.Meta)
                    {
                        if (meta.File == null || meta.File.Length == 0)
                            continue;

                        var fileExt = Path.GetExtension(meta.File.FileName);
                        var fileName = $"{Guid.NewGuid()}{fileExt}";
                        var filePath = Path.Combine(uploadRoot, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await meta.File.CopyToAsync(stream);

                        var relativeUrl = $"/uploads/{uploadDto.ModuleId}/{uploadDto.ProgId}/{fileName}";
                        await _frepo.AddLocalFileAsync(uploadDto, meta, relativeUrl);
                        count++;
                    }

                    TempData["SuccessMessage"] = $"已成功上傳本地";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"上傳失敗：{ex.Message}";
            }

            return RedirectToAction("Index"); // 建議 Redirect，避免重新整理重送
        }

        /// <summary>
        /// 預設載入
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var files = await _frepo.GetFilesByProg("SYS", "UploadTest"); // 依據模組及程式，取得已上傳的檔案
            return View(files);
        }
    }
}
