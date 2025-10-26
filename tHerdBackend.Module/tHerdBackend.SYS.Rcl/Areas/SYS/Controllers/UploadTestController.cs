using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Interfaces.SYS;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    [Area("SYS")]
    public class UploadTestController : BaseUploadController
    {
        private const string MODULE_ID = "SYS";
        private const string PROG_ID = "UploadTest";

        public UploadTestController(ISysAssetFileService frepo, IWebHostEnvironment env)
            : base(frepo, env)
        {
        }

        /// <summary>
        /// 上傳處理（POST）
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(100_000_000)] // 100MB
        [ActionName("Index")]
        public async Task<IActionResult> IndexPost()
        {
            // 呼叫基底邏輯，包含上傳 + 儲存 + 更新檔案清單
            return await HandleUploadAsync(MODULE_ID, PROG_ID);
        }

        /// <summary>
        /// 初次載入（GET）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var files = await _fileService.GetFilesByProg(MODULE_ID, PROG_ID);
            return View(files);
        }

        /// <summary>
        /// 覆寫基底回傳方式（確保使用正確 View）
        /// </summary>
        protected override async Task<IActionResult> ReturnIndexViewAsync(string moduleId, string progId)
        {
            var files = await _fileService.GetFilesByProg(MODULE_ID, PROG_ID);
            return View("Index", files);
        }

        [HttpGet]
        public async Task<IActionResult> GetFilesByProg(string moduleId, string progId)
        {
            var files = await _fileService.GetFilesByProg(moduleId, progId);
            return PartialView("~/Views/Shared/_FileListPartial.cshtml", files);
        }
    }
}
