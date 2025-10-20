using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Interfaces.SYS;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    /// <summary>
    /// 圖片管理
    /// </summary>
    [Area("SYS")]
    public class ImagesController : Controller
    {
        private readonly ISysAssetFileService _frepo;

        public ImagesController(ISysAssetFileService frepo, Cloudinary cloudinary)
        {
            _frepo = frepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var files = await _frepo.GetFiles("SYS", "Images");
            return View(files);
        }
    }
}
