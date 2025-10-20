using CloudinaryDotNet;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.Interfaces.SYS;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    [Area("SYS")]
    public class UploadTestController : Controller
    {
        private readonly ISysAssetFileService _frepo;
        private readonly Cloudinary _cloudinary;

        public UploadTestController(ISysAssetFileService frepo, Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
            _frepo = frepo;
        }

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
                var imageUrls = await _frepo.AddImages(uploadDto);

                ViewBag.ImageUrls = imageUrls; // 將圖片 URL 傳遞到 View

                TempData["SuccessMessage"] = "圖片已成功上傳！";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"上傳失敗：{ex.Message}";
            }

            return RedirectToAction("Index"); // 建議 Redirect，避免重新整理重送
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var files = await _frepo.GetFilesByProg("SYS", "UploadTest");
            return View(files);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(int fileId, CancellationToken ct)
        {
            bool success = await _frepo.DeleteImage(fileId, ct);
            if (success)
                return Json(new { success = true, message = "刪除成功" });
            else
                return Json(new { success = false, message = "刪除失敗" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMeta([FromBody] SysAssetFileDto dto, CancellationToken ct)
        {
            bool success = await _frepo.UpdateImageMeta(dto, ct);
            if (success)
                return Json(new { success = true, message = "更新成功" });
            else
                return Json(new { success = false, message = "更新失敗" });
        }
    }
}
