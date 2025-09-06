using FlexBackend.UIKit.Rcl.Views.Shared.Components.DataTable;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.UIKit.Rcl.ViewComponents
{
    public class DataTableViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(DataTableViewModel model)
        {
            return View(model);
        }
    }
}
