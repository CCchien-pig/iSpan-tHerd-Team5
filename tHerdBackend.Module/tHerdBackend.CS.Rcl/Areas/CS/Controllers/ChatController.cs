using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.Admin.Areas.CS.Controllers
{
	[Area("CS")]
	public class ChatController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
