using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.Admin.Areas.CS.Controllers
{
	[Area("CS")]
	[AllowAnonymous]
	public class ChatController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
