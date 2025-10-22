using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.Interfaces.SYS;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    [Area("SYS")]
    public class UploadTestController : Controller
    {
        private readonly ISysAssetFileService _frepo;

        public UploadTestController(ISysAssetFileService frepo)
        {
            _frepo = frepo;
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
                var files = await _frepo.GetFilesByProg("SYS", "UploadTest");
                return View(files);
            }

            uploadDto.ModuleId = "SYS";
            uploadDto.ProgId = "UploadTest";

            try
            {
                var imageUrls = await _frepo.AddImages(uploadDto); // 呼叫服務上傳圖片

                ViewBag.ImageUrls = imageUrls; // 將圖片 URL 傳遞到 View

                TempData["SuccessMessage"] = "圖片已成功上傳！";
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
