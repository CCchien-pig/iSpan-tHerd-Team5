using FlexBackend.Core.DTOs.PROD;
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

        // GET: Products/Index
        public async Task<IActionResult> Index()
        {
            var products = await _repo.GetAllAsync();

            //var products = await _qrepo.GetAllProductQueryListAsync(1000);
            await LoadBrandOptionsAsync();
            return View(products);
        }

        // 取得品牌選項
        public async Task LoadBrandOptionsAsync()
        {
            var brands = await _repo.LoadBrandOptionsAsync();
            ViewBag.BrandDtos = brands.Select(b => new LoadBrandOptionDto
            {
                BrandId = b.BrandId,
                BrandName = b.BrandName,
                SupplierName = b.SupplierName,
            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int? id)
        {
            var dto = id.HasValue ? await _repo.GetByIdAsync((int)id) : new ProdProductDto();
            await LoadBrandOptionsAsync();
            return View("~/Areas/PROD/Views/Products/Upsert.cshtml", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ProdProductDto dto)
        {
            foreach (var k in new[]
            {
                     nameof(dto.CreatorNm),
                     nameof(dto.ReviserNm),
                     nameof(dto.SupplierName),
                     nameof(dto.ProductTypeDesc),
                     nameof(dto.BrandName),
					 nameof(dto.Seo)
				 })
                ModelState.Remove(k);

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new {
                        Field = x.Key,
                        Attempted = x.Value.AttemptedValue,
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                    })
                    .ToList();

                // 先用 Debug/Console 看
                System.Diagnostics.Debug.WriteLine(System.Text.Json.JsonSerializer.Serialize(errors));
                await LoadBrandOptionsAsync();
                return View("Upsert", dto);   // ← 用視圖名稱，不要用絕對路徑
            }
            if (dto.ProductId > 0) { await _repo.UpdateAsync(dto); } else { await _repo.CreateAsync(dto); }

			// 成功訊息
			TempData["SuccessMessage"] = "商品已成功儲存！";

			return RedirectToAction("Index");
        }
    }
}
