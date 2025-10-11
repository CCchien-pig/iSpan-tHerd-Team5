using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace tHerdBackend.SharedApi.Controllers.Common
{
    /// <summary>
    /// 自動將 [folder] 取代成 Controller 所在命名空間最後一段（例如 PROD）
    /// </summary>
    public class FolderBasedRouteConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var ns = controller.ControllerType.Namespace;
            if (string.IsNullOrEmpty(ns)) return;

            // 取命名空間中的模組名稱（例如 Module.PROD）
            var parts = ns.Split('.');
            var moduleIndex = parts.ToList().FindIndex(p => p == "Module");
            var folderName = moduleIndex >= 0 && moduleIndex < parts.Length - 1
                ? parts[moduleIndex + 1]
                : parts.Last();

            foreach (var selector in controller.Selectors)
            {
                var route = selector.AttributeRouteModel;
                if (route?.Template?.Contains("[folder]") == true)
                {
                    // 替換 token
                    route.Template = route.Template.Replace("[folder]", folderName.ToLower());
                }
            }
        }
    }
}
