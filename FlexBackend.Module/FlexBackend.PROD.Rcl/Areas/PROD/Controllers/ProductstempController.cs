using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.PROD.Rcl.Areas.PROD.Controllers
{
	[Area("PROD")]
	public class ProductstempController : Controller
	{

		private readonly tHerdDBContext _repo;

		public ProductstempController(tHerdDBContext repo)
		{
			_repo = repo;
		}

        public async Task<IActionResult> Index_ex_datatable()
		{
			var products = _repo.ProdProducts.ToList();
			return View(products);
		}
	}
}
