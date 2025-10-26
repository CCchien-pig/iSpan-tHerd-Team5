using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.Interfaces.SYS;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    [Area("SYS")]
    public class UploadTestController : BaseUploadController
    {
        public UploadTestController(ISysAssetFileService frepo, IWebHostEnvironment env)
            : base(frepo, env)
        {
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="uploadDto"></param>
        /// <returns></returns>
        [RequestSizeLimit(100_000_000)] // 100MB
        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPost()
        {
            return await HandleUploadAsync("SYS", "UploadTest");
        }

        /// <summary>
        /// 預設載入
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var files = await _fileService.GetFilesByProg("SYS", "UploadTest"); // 依據模組及程式，取得已上傳的檔案
            return View(files);
        }
    }
}
