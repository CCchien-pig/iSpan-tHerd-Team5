using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels
{
    public class CartItemVM : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
