using Microsoft.AspNetCore.Mvc;
using System;

namespace tHerdBackend.UIKit.Rcl.ViewComponents.UploadImageModal
{
    public class UploadImageModalViewComponent : ViewComponent
    {
        public class UploadImageModalViewModel
        {
            public string ModuleId { get; set; } = "SYS";          // ✅ 給預設值，避免 null
            public string ProgId { get; set; } = "Images";
            public string ModalId { get; set; } = "uploadImageModal";
            public bool DefaultIsExternal { get; set; } = true;
        }

        public IViewComponentResult Invoke(
            string moduleId = "SYS",
            string progId = "Images",
            string modalId = null,
            bool? defaultIsExternal = null)
        {
            // ✅ 自動產生唯一 ID（若未傳）
            var finalModalId = !string.IsNullOrEmpty(modalId)
                ? modalId
                : $"uploadImageModal_{Guid.NewGuid():N}".Substring(24);

            var vm = new UploadImageModalViewModel
            {
                ModuleId = moduleId ?? "SYS",
                ProgId = progId ?? "Images",
                ModalId = finalModalId,
                DefaultIsExternal = defaultIsExternal ?? true
            };

            return View("Default", vm);
        }
    }
}
