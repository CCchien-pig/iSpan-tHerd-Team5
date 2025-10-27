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
        public async Task<IActionResult> GetFilesByProg(string moduleId, string progId, bool json = false)
        {
            try
            {
                moduleId ??= "SYS";
                progId ??= "UploadTest";

                var files = await _fileService.GetFilesByProg(moduleId, progId);

                if (json)
                    return Json(files); // 使用原本的 DTO 命名

                return PartialView("_FileListPartial", files);
            }
            catch (Exception ex)
            {
                // 若要除錯，這行可以暫時開啟
                // return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });

                // 正式環境建議用這行
                return StatusCode(500, "伺服器內部錯誤，請稍後再試。");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSelectableFiles(string moduleId, string progId)
        {
            var files = await _fileService.GetFilesByProg(moduleId, progId);
            return PartialView("~/Views/Shared/_FileSelectPartial.cshtml", files);
        }
    }
}
