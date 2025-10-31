using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Syncfusion.EJ2.FileManager.Base;
using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using System.IO;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    [Area("SYS")]
    public class FileManagerController : Controller
    {
        private readonly PhysicalFileProvider _provider;
        private readonly string _rootPath;

        public FileManagerController()
        {
            _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);

            _provider = new PhysicalFileProvider();
            _provider.RootFolder(_rootPath);
        }

        /// <summary>
        /// ✅ 檔案總管主頁面（測試用）
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// ✅ 同時支援「真實模式」與「假資料模式」
        /// 可用 query 參數 ?mode=fake 進入假資料
        /// </summary>
        [HttpPost]
        public IActionResult FileOperations([FromBody] FileManagerDirectoryContent args)
        {
            var mode = Request.Query["mode"].ToString();

            // 🔹 假資料模式
            if (mode == "fake")
            {
                var cwd = new
                {
                    name = "Root",
                    type = "folder",
                    size = 0,
                    dateModified = "2025-10-28T00:00:00",
                    hasChild = true
                };

                var files = new List<object>
        {
            new { name = "Documents", type = "folder", hasChild = true, dateModified = "2025-10-28T00:00:00", size = 0 },
            new { name = "Images", type = "folder", hasChild = true, dateModified = "2025-10-28T00:00:00", size = 0 },
            new { name = "readme.txt", type = "file", isFile = true, size = 1245, dateModified = "2025-10-27T09:00:00" },
            new { name = "invoice.pdf", type = "file", isFile = true, size = 45200, dateModified = "2025-10-26T11:30:00" },
            new { name = "photo.jpg", type = "file", isFile = true, size = 245000, dateModified = "2025-10-25T20:45:00" }
        };

                return Json(new { cwd, files });
            }

            // 🔹 真實模式（注意這裡每一行都加了 Json()）
            switch (args.Action)
            {
                case "read":
                    return Json(_provider.GetFiles(args.Path, args.ShowHiddenItems, args.Data));
                case "create":
                    return Json(_provider.Create(args.Path, args.Name, args.Data));
                case "delete":
                    return Json(_provider.Delete(args.Path, args.Names, args.Data));
                case "rename":
                    return Json(_provider.Rename(args.Path, args.Name, args.NewName, false, true, args.Data));
                default:
                    return Json(new { error = "Unknown action" });
            }
        }


        [HttpPost]
        public IActionResult Upload(string path, IList<IFormFile> uploadFiles, string action)
        {
            _provider.Upload(path, uploadFiles, null);
            return Json(new { success = true });
        }

        [HttpPost]
        public FileStreamResult Download(string[] names, string path)
        {
            return _provider.Download(path, names);
        }

        [HttpGet]
        public IActionResult GetImage(string path, string id)
        {
            return _provider.GetImage(_rootPath, path, true, null, null);
        }
    }
}
