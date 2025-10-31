using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.UIKit.Rcl.ViewComponents
{
    public class FileManagerModalViewComponent : ViewComponent
    {
        public class FileManagerModalViewModel
        {
            public string ModalId { get; set; } = "fileManagerModal";
            public string ModuleId { get; set; } = "SYS";
            public string ProgId { get; set; } = "Files";
            public bool EnableCloud { get; set; } = true; // ✅ 是否開啟雲端
        }

        public IViewComponentResult Invoke(FileManagerModalViewModel vm)
        {
            return View(vm);
        }
    }
}
