using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.PROD.Rcl.Areas.PROD.Controllers
{
	[Area("PROD")]
	public class ProductstempController : Controller
	{

		private readonly IProductService _repo;

		public ProductstempController(IProductService repo)
		{
			_repo = repo;
		}

		public async Task<IActionResult> Index_ex_datatable()
		{
			var products = await _repo.GetAllAsync();
			return View(products);
		}
	}
}
