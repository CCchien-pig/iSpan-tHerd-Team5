using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.Interfaces.PROD;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

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
            await GetData();
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
                BrandCode = b.BrandCode,
                SupplierName = b.SupplierName
            }).ToList();
        }

        // 取得產品分類
		public async Task GetAllProductTypesAsync()
		{
			var Types = await _repo.GetAllProductTypesAsync();
			ViewBag.TypeDtos = Types.Select(b => new ProdProductTypeConfigDto
            {
                ProductTypeId = b.ProductTypeId,
                ProductTypeCode = b.ProductTypeCode,
                ProductTypeName = b.ProductTypeName
			}).ToList();
		}

		// 取得系統代號
		public async Task GetSysCodes()
        {
            var syss = await _repo.GetSysCodes("PROD", new List<string> { "01" });
            ViewBag.Units = syss.Select(b => new SelectListItem
            {
                Value = b.CodeId,
                Text = b.CodeDesc
            }).ToList();
        }

        private async Task GetData()
        {
            ViewData["Title"] = "商品管理";
            ViewData["Controller"] = "Products";
            ViewData["Area"] = "PROD";
            await GetSysCodes();
            await LoadBrandOptionsAsync();
            await GetAllProductTypesAsync();
        }

		//      public IActionResult AddSkuCard(int index)
		//{
		//	var newSku = new ProdProductSkuDto
		//	{
		//		SkuCode = $"TEMP-{DateTime.UtcNow.Ticks}" // 或 Guid.NewGuid().ToString()
		//	};
		//	ViewData["Index"] = index;
		//	return PartialView("_SkuCard", newSku);
		//}

		public IActionResult AddSkuCard(int index)
		{
			var newSku = new ProdProductSkuDto
			{
				SkuCode = $"TEMP-{DateTime.UtcNow.Ticks}"
			};

			// 從主頁的 ViewData 或資料庫帶 SpecConfigs
			var specConfigs = ViewData["SpecConfigs"] as List<ProdSpecificationConfigDto> ?? new();

			// 塞進 ViewData
			ViewData["Index"] = index;
			ViewData["SpecConfigs"] = specConfigs;

			return PartialView("_SkuCard", newSku);
		}


		[HttpGet]
        public async Task<IActionResult> Upsert(int? id)
        {
            var dto = id.HasValue ? await _repo.GetByIdAsync((int)id) : new ProdProductDto();
            await GetData();
            return View("Upsert", dto);
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

			if (string.IsNullOrWhiteSpace(dto.ProductCode))
			{
				dto.ProductCode = $"P-{DateTime.UtcNow:yyyyMMddHHmmss}";
				ModelState.Remove(nameof(dto.ProductCode)); // 關鍵！
			}

            foreach (var i in dto.Types) {
                if (i.ProductId == null)
                {
                    i.ProductId = 0;
                }
            }

            var (isValid, errorMsg) = await _repo.ValidateProductAsync(dto);
			if (!isValid)
			{
				ViewBag.ErrorMessage = errorMsg;
				await GetData();
				return View("Upsert", dto);
			}

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
                await GetData();
                return View("Upsert", dto);
            }
            if (dto.ProductId > 0) { await _repo.UpdateAsync(dto); } else { await _repo.CreateAsync(dto); }

            TempData["SuccessMessage"] = "資料已成功儲存！";

            return RedirectToAction("Index");
        }
    }
}
