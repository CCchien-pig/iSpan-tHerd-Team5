using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.UIKit.Rcl.ViewComponents.UploadImageModal
{
    public class UploadImageModalViewComponent : ViewComponent
    {
        public class UploadImageModalViewModel
        {
            public string ModuleId { get; set; } = "";
            public string ProgId { get; set; } = "";
            public string ModalId { get; set; } = "uploadImageModal";
            public bool DefaultIsExternal { get; set; } = true;
        }

        public IViewComponentResult Invoke(string moduleId, string progId, string modalId = "uploadImageModal")
        {
            var vm = new UploadImageModalViewModel
            {
                ModuleId = moduleId,
                ProgId = progId,
                ModalId = modalId
            };

            return View("Default", vm);
        }
    }
}
