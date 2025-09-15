using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.USER.Rcl.Areas.USER.Controllers
{
	public class MembersController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
