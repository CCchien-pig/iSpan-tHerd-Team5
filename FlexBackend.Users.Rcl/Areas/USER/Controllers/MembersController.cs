using FlexBackend.Core.Interfaces;
using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Infra.Repository;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.Users.Rcl.Areas.USER.Controllers
{
	[Area("USER")]
	public class MembersController : Controller
    {
        private readonly ISysProgramConfigRepository _repo;

        public MembersController(ISysProgramConfigRepository repo)
        {
            _repo = repo;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
