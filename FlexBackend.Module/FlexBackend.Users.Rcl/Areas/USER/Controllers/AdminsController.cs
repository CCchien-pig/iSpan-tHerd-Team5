using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.USER.Rcl.Areas.USER.Controllers
{
	public class AdminsController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
