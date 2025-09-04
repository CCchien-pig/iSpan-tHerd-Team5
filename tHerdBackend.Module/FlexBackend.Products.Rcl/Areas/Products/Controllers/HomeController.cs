using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.Products.Rcl.Areas.Products.Controllers
{
    public class HomeController : Controller
    {
        [Area("Products")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
