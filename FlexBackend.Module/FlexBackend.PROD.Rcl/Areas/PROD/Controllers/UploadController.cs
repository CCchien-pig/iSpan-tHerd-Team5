using CloudinaryDotNet;
using FlexBackend.Core.Interfaces.SYS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.PROD.Rcl.Areas.PROD.Controllers
{
    [Area("PROD")]
    public class UploadController : Controller
    {
        private readonly ISysAssetFileService _frepo;
        private readonly Cloudinary _cloudinary;

        public UploadController(ISysAssetFileService frepo, Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
            _frepo = frepo;
        }

        [HttpPost]
        public async Task<IActionResult> Index(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                ViewBag.Message = "請至少選擇一個檔案";
                return View();
            }

            _frepo.AddImages(files);

            ViewBag.Message = $"圖片已成功上傳！";

            return View();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
