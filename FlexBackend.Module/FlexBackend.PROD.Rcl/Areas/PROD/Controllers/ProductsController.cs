using FlexBackend.Core.Interfaces.PROD;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.Products.Rcl.Areas.PROD.Controllers
{
	[Area("PROD")]
	public class ProductsController : Controller
    {
        private readonly IProductService _repo;
        private readonly IProductQueryService _qrepo;

        public ProductsController(IProductService repo, IProductQueryService qrepo)
        {
            _repo = repo;
            _qrepo = qrepo;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _repo.GetAllAsync();

            var productquerys = await _qrepo.GetAllProductQueryListAsync(1000);
            return View(products);
        }
    }
}
