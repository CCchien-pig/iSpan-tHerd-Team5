using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.Products.Rcl.Areas.Products.Controllers
{
	[Area("Products")]
	public class ProductsController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }
    }
}
