using FlexBackend.UIKit.Rcl.Views.Shared.Components.DataTable;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.UIKit.Rcl.ViewComponents.DataTable
{
    public class DataTableViewComponent : ViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync(DataTableViewModel model)
        {
            // 防呆：如果有 Headers，就自動補上第一欄控制列（若啟用）
            if (model.EnableChildRow && model.Headers is not null)
            {
                // 確保第一個是空白欄（給 dt-control）
                var list = new List<string>(model.Headers);
                if (list.Count == 0 || list[0] != string.Empty)
                    list.Insert(0, string.Empty);
                model.Headers = list;
            }
            return Task.FromResult<IViewComponentResult>(View("DataTableDefault", model));
        }
    }
}
