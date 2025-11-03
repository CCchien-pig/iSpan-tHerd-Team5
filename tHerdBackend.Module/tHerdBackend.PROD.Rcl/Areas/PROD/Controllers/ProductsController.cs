using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.Models;

namespace tHerdBackend.Products.Rcl.Areas.PROD.Controllers
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
            await GetData();
            return View();
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
                Value = b.CodeNo,
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
            var dto = id.HasValue ? await _repo.GetByIdAsync((int)id) : new ProdProductDetailDto();
            await GetData();
            return View("Upsert", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ProdProductDetailDto dto)
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

            // 🔹 Step 1. 清除錯誤
            var keysToRemove = ModelState.Keys
                .Where(k => k.Contains("Images[") && (k.EndsWith(".AltText") || k.EndsWith(".Caption")))
                .ToList();

            foreach (var key in keysToRemove)
                ModelState[key].Errors.Clear();

            // 🔹 Step 2. 清除狀態
            ModelState.ClearValidationState("Images");

            // 🔹 Step 3. 強制設定為 Valid ✅
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Images[")))
            {
                ModelState[key].ValidationState = ModelValidationState.Valid;
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

		[HttpGet]
		public async Task<IActionResult> GetProducts(
	        int? brandId,
	        int? productTypeId,
	        bool? isPublished,
	        string? keyword = null,
			int? productId = null,
			int pageIndex = 1,
	        int pageSize = 20,
	        string? sortBy = null,
	        bool sortDesc = false)
		{
			try
			{
				var query = new ProductFilterQueryDto
				{
					PageIndex = pageIndex,
					PageSize = pageSize,
					BrandId = brandId,
					ProductTypeId = productTypeId,
					Keyword = keyword,
					SortBy = sortBy,
					SortDesc = sortDesc,
					ProductId = productId,
                    IsPublished = isPublished
                };

				var (list, totalCount) = await _repo.GetAllAsync(query);

                // === 回傳 DataTables 標準格式 ===
                return Json(new
                {
                    draw = Request.Query["draw"].FirstOrDefault(), // DataTables 自動帶
                    recordsTotal = totalCount,                     // 總筆數（未篩選）
                    recordsFiltered = totalCount,                  // 篩選後筆數
                    data = list                                    // 當頁資料
                });
            }
			catch (Exception ex)
			{
                return StatusCode(500, new
                {
                    error = "伺服器錯誤",
                    message = ex.Message
                });
            }
		}
	}
}
