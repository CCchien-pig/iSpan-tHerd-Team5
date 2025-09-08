using FlexBackend.Core.Interfaces.PROD;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.Products.Rcl.Areas.PROD.Controllers
{
	[Area("PROD")]
	public class ProductsController : Controller
    {
        private readonly IProductService _repo;

        public ProductsController(IProductService repo)
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
