using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.UIKit.Rcl.ViewComponents.UploadImageModal
{
    public class UploadImageModalViewComponent : ViewComponent
    {
        public class UploadImageModalViewModel
        {
            public string ModuleId { get; set; } = "";
            public string ProgId { get; set; } = "";

            // 預設要不要雲端
            public bool DefaultIsExternal { get; set; } = true;
        }

        public IViewComponentResult Invoke(string moduleId, string progId, string modalId = "uploadImageModal")
        {
            var vm = new UploadImageModalViewModel();

            return View("Default", vm);
        }
    }
}
