using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Syncfusion.EJ2.FileManager.Base;
using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using System.IO;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    [Area("SYS")]  // ✅ 這行非常重要
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

        [HttpPost]
        public object FileOperations([FromBody] FileManagerDirectoryContent args)
        {
            switch (args.Action)
            {
                case "read":
                    return _provider.GetFiles(args.Path, args.ShowHiddenItems, args.Data);
                case "create":
                    return _provider.Create(args.Path, args.Name, args.Data);
                case "delete":
                    return _provider.Delete(args.Path, args.Names, args.Data);
                case "rename":
                    return _provider.Rename(args.Path, args.Name, args.NewName, false, true, args.Data);
                default:
                    return null;
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
