using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.SharedApi.Controllers.Module.USER
{
	[ApiController]
	[Route("api/User")]
	public class UserController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
