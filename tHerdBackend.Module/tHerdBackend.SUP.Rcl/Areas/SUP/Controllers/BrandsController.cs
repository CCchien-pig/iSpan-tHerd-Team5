using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.Controllers
{
	public class BrandsController : Controller
	{
		private readonly tHerdDBContext _context;
		private readonly ICurrentUser _me;
		private readonly UserManager<ApplicationUser> _userMgr;

		public BrandsController(
			tHerdDBContext context,
			ICurrentUser me,
			UserManager<ApplicationUser> userMgr)
		{
			_context = context;
			_me = me;
			_userMgr = userMgr;
		}

		// GET: SUP/Brands/Index
		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}
	}
}
