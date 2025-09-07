using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Infra.Repository.PROD;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.Products.Rcl.Areas.PROD.Controllers
{
	[Area("PROD")]
	public class ProductsController : Controller
    {
        private readonly IProdProductRepository _repo;

        public ProductsController(IProdProductRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _repo.GetAllAsync();
            return View(products);
        }
    }
}
