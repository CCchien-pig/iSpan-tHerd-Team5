using CloudinaryDotNet;
using FlexBackend.Core.DTOs;
using FlexBackend.Core.Interfaces.SYS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace FlexBackend.SYS.Rcl.Areas.SYS.Controllers
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
            if (uploadDto.Files == null || uploadDto.Files.Count == 0)
            {
                ViewBag.Message = "請至少選擇一個檔案";
                return View();
            }

            uploadDto.ModuleId = "SYS";
            uploadDto.ProgId = "UploadTest";

            var imageUrls = await _frepo.AddImages(uploadDto);

            ViewBag.ImageUrls = imageUrls; // 將圖片 URL 傳遞到 View

            ViewBag.Message = $"圖片已成功上傳！";

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var files = await _frepo.GetFiles("SYS", "UploadTest");
            return View(files);
        }
    }
}
