using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.Interfaces.PROD;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        }

        public IActionResult AddSkuCard(int index)
		{
			var newSku = new ProdProductSkuDto
			{
				SkuCode = $"TEMP-{DateTime.UtcNow.Ticks}" // 或 Guid.NewGuid().ToString()
			};
			ViewData["Index"] = index;
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

            // 手動驗證 SKU 庫存與價格邏輯
            foreach (var (sku, i) in dto.Skus.Select((x, i) => (x, i)))
            {
                var max = sku.MaxStockQty;
                var reorder = sku.ReorderPoint;
                var safety = sku.SafetyStockQty;

                // === 1. 庫存檢查 ===
                if (max <= reorder || reorder <= safety)
                {
                    ViewBag.ErrorMessage = "最大庫存必須大於再訂點，再訂點必須大於安全庫存！";
                    await GetData();
                    return View("Upsert", dto);
                }

                // === 2. 價格層級檢查 ===
                var listPrice = sku.ListPrice ?? 0;      // 原價
                var unitPrice = sku.UnitPrice ?? 0;      // 單價
                var discount = sku.SalePrice;  // 優惠價
                var cost = sku.CostPrice ?? 0;      // 成本價

                if (!(listPrice > unitPrice && unitPrice > discount && discount > cost))
                {
                    ViewBag.ErrorMessage = $"第{i + 1}筆 SKU 的價格設定錯誤：必須符合 原價 > 單價 > 優惠價 > 成本價！";
                    await GetData();
                    return View("Upsert", dto);
                }
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
