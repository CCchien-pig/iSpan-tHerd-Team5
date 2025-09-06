using FlexBackend.Core.Interfaces;
using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Infra.Repository;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.Products.Rcl.Areas.Products.Controllers
{
	[Area("PROD")]
	public class ProductsController : Controller
    {
        private readonly ISysProgramConfigRepository _repo;

        public ProductsController(ISysProgramConfigRepository repo)
        {
            _repo = repo;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
