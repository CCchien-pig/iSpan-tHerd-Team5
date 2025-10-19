using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.ORD.Rcl.Areas.ORD.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
