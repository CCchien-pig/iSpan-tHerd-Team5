using FlexBackend.Core.DTOs.SUP;
using FlexBackend.Infra.Models;
using FlexBackend.SUP.Rcl.Areas.SUP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupStockBatch = FlexBackend.Infra.Models.SupStockBatch;
using SupStockHistory = FlexBackend.Infra.Models.SupStockHistory;

namespace FlexBackend.SUP.Rcl.Areas.SUP.Controllers
{
	[Area("SUP")]
	public class StockBatchesController : Controller
    {
        private readonly tHerdDBContext _context;

        public StockBatchesController(tHerdDBContext context)
        {
            _context = context;
        }

		// GET: StockBatches/Index
		[HttpGet]
        public IActionResult Index()
        {
            return View();
        }

		// POST: StockBatches/IndexJson
		[HttpPost]
		public async Task<IActionResult> IndexJson()
		{
			// 從 DataTables POST 取得參數
			var draw = Request.Form["draw"].FirstOrDefault() ?? "1";
			var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
			var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
			var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";

			// 取得排序資訊
			var sortColumnIndex = Convert.ToInt32(Request.Form["order[0][column]"].FirstOrDefault() ?? "0");
			var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault() ?? "asc";

			//var query = _context.StockBatches.AsQueryable();

			// 建立 join 查詢（SupStockBatches → Prod_ProductSku → ProdProducts → SupBrands）
			var query = from sb in _context.SupStockBatches
						join sku in _context.ProdProductSkus on sb.SkuId equals sku.SkuId
						join p in _context.ProdProducts on sku.ProductId equals p.ProductId
						join b in _context.SupBrands on p.BrandId equals b.BrandId
						select new
						{
							sb.StockBatchId,
							sku.SkuCode,
							sb.BatchNumber,
							sb.ExpireDate,
							sb.Qty,
							sku.SafetyStockQty,
							sku.ReorderPoint,
							sb.IsSellable,

							// 展開要的
							sb.Sku.Product.ProductName,
							sb.Sku.Product.Brand.BrandName,
							sb.Sku.Product.Brand.BrandCode,

							// 規格群組 + 規格選項
							Specifications = sb.Sku.SpecificationOptions
								.OrderBy(o => o.OrderSeq)
								.Select(o => new {
									o.SpecificationConfig.GroupName,  // 群組名稱
									o.OptionName                      // 選項名稱
								})
								.ToArray()
						};


			// 搜尋功能
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(s =>
					EF.Functions.Like(s.SkuCode, $"%{searchValue}%") ||
					EF.Functions.Like(s.BatchNumber, $"%{searchValue}%") ||
					EF.Functions.Like(s.ProductName, $"%{searchValue}%") ||
					EF.Functions.Like(s.BrandName, $"%{searchValue}%") ||
					EF.Functions.Like(s.BrandCode, $"%{searchValue}%")
				);
			}

			var totalRecords = await query.CountAsync();

			// 欄位索引排序
			// orderable: false，不會傳 0，不用算進來
			query = sortColumnIndex switch
			{
				0 => sortDirection == "asc" ? query.OrderBy(s => s.SkuCode) : query.OrderByDescending(s => s.SkuCode),
				1 => sortDirection == "asc" ? query.OrderBy(s => s.BatchNumber) : query.OrderByDescending(s => s.BatchNumber),
				2 => sortDirection == "asc" ? query.OrderBy(s => s.ExpireDate) : query.OrderByDescending(s => s.ExpireDate),
				3 => sortDirection == "asc" ? query.OrderBy(s => s.Qty) : query.OrderByDescending(s => s.Qty),
				4 => sortDirection == "asc" ? query.OrderBy(s => s.SafetyStockQty) : query.OrderByDescending(s => s.SafetyStockQty),
				5 => sortDirection == "asc" ? query.OrderBy(s => s.ReorderPoint) : query.OrderByDescending(s => s.ReorderPoint),
				_ => query.OrderBy(s => s.StockBatchId),
			};

			// 分頁
			var data = await query
				.Skip(start)
				.Take(length)
				.ToListAsync();

			// 傳給前端
			var result = data.Select(d => new
			{
				d.StockBatchId,
				d.SkuCode,
				d.BatchNumber,
				d.ExpireDate,
				d.Qty,
				d.SafetyStockQty,
				d.ReorderPoint,

				// 展開列需要的
				d.BrandName,
				d.ProductName,
				// 字串版規格，用斜線分隔
				//Specifications = string.Join(" / ", d.Specifications),
				// 陣列版規格
				Specifications = d.Specifications,

			});

			return Json(new
			{
				draw,
				recordsTotal = totalRecords,
				recordsFiltered = totalRecords,
				data = result
			});
		}


		// GET: StockBatches/Create
		[HttpGet]
        public IActionResult Create()
        {
			// 空物件給 Partial View 使用
			var viewModel = new StockBatchContactViewModel();

			ViewBag.FormAction = "Create"; // 告訴 Partial View 這是 Create 動作
			return PartialView("Partials/_StockBatchFormPartial", viewModel); // 回傳 Partial View
		}

		// POST: StockBatches/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateStockBatch([Bind("SkuId,ManufactureDate,Qty,IsSellable,MovementType,IsAdd,ChangeQty,Remark,UserId")] StockBatchContactViewModel vm)
		{
			if (!ModelState.IsValid)
				return PartialView("Partials/_StockBatchFormPartial", vm);

			// 1. 找 SKU 對應的 Product 與 Brand
			var sku = await _context.ProdProductSkus
				.Include(s => s.Product)
					.ThenInclude(p => p.Brand)
				.FirstOrDefaultAsync(s => s.SkuId == vm.SkuId);

			if (sku == null)
				return BadRequest("找不到 SKU");

			// 2. 檢查製造日期
			if (!vm.ManufactureDate.HasValue)
				return BadRequest("請選擇製造日期");

			// 3. 新增 SupStockBatch（先不產生批號）
			var stockBatch = new SupStockBatch
			{
				SkuId = vm.SkuId,
				Qty = vm.Qty,
				IsSellable = vm.IsSellable,
				ManufactureDate = vm.ManufactureDate,
				Creator = vm.UserId ?? 0,
				CreatedDate = DateTime.Now
			};

			// 4. 計算有效日期
			if (vm.ManufactureDate.HasValue && sku.ShelfLifeDays > 0)
			{
				stockBatch.ExpireDate = vm.ManufactureDate.Value.AddDays(sku.ShelfLifeDays);
			}
			else
			{
				stockBatch.ExpireDate = null; // 無限制
			}

			_context.SupStockBatches.Add(stockBatch);
			await _context.SaveChangesAsync(); // 先新增，取得 StockBatchId

			// 5. 生成批號（新增成功後）
			string brandCode = sku?.Product?.Brand?.BrandCode ?? "XXX";
			stockBatch.BatchNumber = GenerateBatchNumber(brandCode);
			await _context.SaveChangesAsync();

			// 6. 記錄庫存異動（Create 時視為新增入庫）
			if (!string.IsNullOrEmpty(vm.MovementType))
			{
				bool isValidMovementType = await _context.SysCodes
					.AnyAsync(c => c.ModuleId == "SUP" && c.CodeId == "01" && c.CodeNo == vm.MovementType);

				if (!isValidMovementType)
					return BadRequest($"無效的異動類型: {vm.MovementType}");

				int beforeQty = stockBatch.Qty; // 真實庫存
				int newQty = beforeQty;

				switch (vm.MovementType)
				{
					case "Purchase": newQty += vm.ChangeQty; break;
					case "Sale":
						if (vm.ChangeQty > beforeQty)
							return BadRequest("銷售出庫量不能大於現有庫存");
						newQty -= vm.ChangeQty;
						break;
					case "Return": newQty += vm.ChangeQty; break;
					case "Expire": newQty -= vm.ChangeQty; break;
					case "Adjust": newQty += vm.IsAdd ? vm.ChangeQty : -vm.ChangeQty; break;
				}

				// 檢查最大庫存
				if (sku.MaxStockQty > 0 && newQty > sku.MaxStockQty)
				{
					return BadRequest($"調整後庫存 {newQty} 超過最大庫存量 {sku.MaxStockQty}");
				}

				stockBatch.Qty = newQty;


				// 更新批次庫存
				stockBatch.Qty = newQty;
				stockBatch.IsSellable = vm.IsSellable;
				await _context.SaveChangesAsync();

				// 記錄 SupStockHistory
				_context.SupStockHistories.Add(new SupStockHistory
				{
					StockBatchId = stockBatch.StockBatchId,
					ChangeType = vm.MovementType,
					ChangeQty = vm.ChangeQty,
					BeforeQty = stockBatch.Qty,
					AfterQty = newQty,
					Reviser = vm.UserId ?? 0,
					RevisedDate = DateTime.Now,
					Remark = vm.Remark
				});
				await _context.SaveChangesAsync();
			}

			return Json(new { success = true, batchNumber = stockBatch.BatchNumber, newQty = stockBatch.Qty });
		}

		// 自動產生批號
		// 批號{BrandCode}{yyyymmdd}-{流水號}
		// 流水號每日統一遞增，不分品牌
		private string GenerateBatchNumber(string brandCode)
		{
			if (string.IsNullOrEmpty(brandCode))
				brandCode = "XX"; // 若無品牌簡碼，預設 XX

			// 日期字串

			var today = DateTime.Today;
			var datePart = today.ToString("yyyyMMdd");

			// 計算當天所有批次的流水號 +1（不分品牌）
			int seq = _context.SupStockBatches
						.Where(sb => sb.CreatedDate >= today && sb.CreatedDate < today.AddDays(1))
						.Count() + 1;

			return $"{brandCode}{datePart}-{seq:000}";
		}

		#region 新增庫存表單用 API

		// 取得品牌列表
		[HttpGet]
		public async Task<IActionResult> GetBrands()
		{
			var brands = await _context.SupBrands
				.Where(b => b.IsActive)
				.Select(b => new
				{
					b.BrandId,
					b.BrandName
				})
				.ToListAsync();
			return Ok(brands);
		}

		// 取得商品列表 (依品牌)
		[HttpGet]
		public async Task<IActionResult> GetProductsByBrand(int brandId)
		{
			var products = await _context.ProdProducts
				.Where(p => p.BrandId == brandId && p.IsPublished)
				.Select(p => new
				{
					p.ProductId,
					p.ProductName
				})
				.ToListAsync();
			return Ok(products);
		}

		// 取得 SKU 列表 (依產品)
		[HttpGet]
		public async Task<IActionResult> GetSkusByProduct(int productId)
		{
			var skus = await _context.ProdProductSkus
				.Where(sku => sku.ProductId == productId && sku.IsActive)
				.Select(sku => new
				{
					sku.SkuId,
					sku.SkuCode,
					sku.StockQty,         // 目前庫存量
					sku.ReorderPoint,     // 再訂購點
					sku.SafetyStockQty,   // 安全庫存量
					sku.MaxStockQty       // 最大庫存量
				})
				.ToListAsync();
			return Ok(skus);
		}

		// 取得 SKU 詳細資訊 (選完 SKU 後自動帶入底下欄位)
		[HttpGet]
		public async Task<IActionResult> GetSkuInfo(int skuId)
		{
			var skuInfo = await _context.ProdProductSkus
				.Where(sku => sku.SkuId == skuId && sku.IsActive)
				.Select(sku => new
				{
					sku.SkuId,
					sku.SkuCode,
					sku.StockQty,
					sku.ReorderPoint,
					sku.SafetyStockQty,
					sku.MaxStockQty
				})
				.FirstOrDefaultAsync();

			if (skuInfo == null)
				return NotFound("找不到 SKU");

			return Ok(skuInfo);
		}

		// 取得異動類型
		[HttpGet]
		public async Task<IActionResult> GetMovementTypes()
		{
			var codes = await _context.SysCodes
				.Where(c => c.ModuleId == "SUP" && c.CodeId == "01" && c.IsActive)
				.Select(c => new
				{
					Value = c.CodeNo,    // Purchase/Sale/Return/Expire/Adjust
					Text = c.CodeDesc    // 採購入庫/銷售出庫/退貨入庫/到期報廢/手動調整
				})
				.ToListAsync();

			return Ok(codes);
		}

		#endregion


		// GET: StockBatches/Edit/5
		[HttpGet]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var supStockBatch = await _context.SupStockBatches
				.Include(sb => sb.Sku)
					.ThenInclude(s => s.Product)
						.ThenInclude(p => p.Brand)
				.FirstOrDefaultAsync(sb => sb.StockBatchId == id);

			if (supStockBatch == null) return NotFound();

			var sku = supStockBatch.Sku;
			var product = sku?.Product;
			var brand = product?.Brand;

			// 計算有效日期（只做顯示）
			DateTime? expireDate = null;
			if (supStockBatch.ManufactureDate.HasValue && sku?.ShelfLifeDays > 0)
			{
				expireDate = supStockBatch.ManufactureDate.Value.AddDays(sku.ShelfLifeDays);
			}

			var viewModel = new StockBatchContactViewModel
			{
				StockBatchId = supStockBatch.StockBatchId,
				SkuId = supStockBatch.SkuId,
				SkuCode = sku?.SkuCode,
				ProductName = product?.ProductName,
				BrandName = brand?.BrandName,
				BatchNumber = supStockBatch.BatchNumber,
				IsSellable = supStockBatch.IsSellable,
				ManufactureDate = supStockBatch.ManufactureDate,
				ExpireDate = expireDate,
				Qty = supStockBatch.Qty,
				//SafetyStockQty = sku?.SafetyStockQty ?? 0,
				//ReorderPoint = sku?.ReorderPoint ?? 0,
				//MaxStockQty = sku?.MaxStockQty ?? 0,
				Creator = supStockBatch.Creator,
				CreatedDate = supStockBatch.CreatedDate,
				Reviser = supStockBatch.Reviser,
				RevisedDate = supStockBatch.RevisedDate
			};

			ViewBag.FormAction = "Edit";
			return PartialView("Partials/_StockBatchFormPartial", viewModel);
		}


		// 儲存庫存異動
		[HttpPost]
		public async Task<IActionResult> SaveStockMovement(SupStockMovementDto dto)
		{
			if (dto == null) return BadRequest("資料無效");
			if (dto.ChangeQty <= 0) return BadRequest("變動數量必須大於 0");

			bool isValidMovementType = await _context.SysCodes
				.AnyAsync(c => c.ModuleId == "SUP" && c.CodeId == "01" && c.CodeNo == dto.MovementType);
			if (!isValidMovementType)
				return BadRequest($"無效的 MovementType: {dto.MovementType}");

			var batch = await _context.SupStockBatches
				.Include(sb => sb.Sku)
				.FirstOrDefaultAsync(sb => sb.StockBatchId == dto.StockBatchId);

			if (batch == null) return NotFound($"找不到庫存批次: {dto.StockBatchId}");

			int newQty = dto.CurrentQty;
			switch (dto.MovementType)
			{
				case "Purchase": newQty += dto.ChangeQty; break;
				case "Sale": newQty -= dto.ChangeQty; break;
				case "Return": newQty += dto.ChangeQty; break;
				case "Expire": newQty -= dto.ChangeQty; break;
				case "Adjust": newQty += dto.IsAdd ? dto.ChangeQty : -dto.ChangeQty; break;
				default: return BadRequest($"未支援的 MovementType: {dto.MovementType}");
			}

			// 後端驗證
			if (newQty < 0) return BadRequest("異動後庫存不能小於 0");
			if (batch.Sku.MaxStockQty > 0 && newQty > batch.Sku.MaxStockQty)
				return BadRequest($"異動後庫存不能超過最大庫存量 {batch.Sku.MaxStockQty}");

			// 更新批次
			batch.Qty = newQty;
			batch.IsSellable = dto.IsSellable;
			await _context.SaveChangesAsync();

			// 記錄異動歷史
			_context.SupStockHistories.Add(new SupStockHistory
			{
				StockBatchId = batch.StockBatchId,
				ChangeType = dto.MovementType,
				ChangeQty = dto.ChangeQty,
				BeforeQty = dto.CurrentQty,
				AfterQty = newQty,
				Reviser = dto.UserId ?? 0,
				RevisedDate = DateTime.Now,
				Remark = dto.Remark
			});

			await _context.SaveChangesAsync();

			return Ok(new { success = true, newQty });
		}


		// POST: StockBatches/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, StockBatchContactViewModel model)
		{
			if (id != model.StockBatchId)
				return NotFound();

			if (!ModelState.IsValid)
				return PartialView("Partials/_StockBatchFormPartial", model);

			try
			{
				// 1. 取得庫存批次及 SKU
				var stockBatch = await _context.SupStockBatches
					.Include(sb => sb.Sku)
					.FirstOrDefaultAsync(sb => sb.StockBatchId == id);

				if (stockBatch == null)
					return Json(new { success = false, message = "找不到資料" });

				var sku = stockBatch.Sku;

				// 2. 計算是否有變更
				bool hasChanged = stockBatch.ManufactureDate != model.ManufactureDate ||
								  stockBatch.Qty != model.Qty ||
								  stockBatch.IsSellable != model.IsSellable;

				if (!hasChanged)
					return Json(new { success = false, message = "未變更" });

				// 3. 記錄修改前庫存
				int beforeQty = stockBatch.Qty;

				// 4. 更新欄位
				stockBatch.ManufactureDate = model.ManufactureDate;
				stockBatch.Qty = model.Qty;
				stockBatch.IsSellable = model.IsSellable;

				// 5. 計算有效日期
				if (model.ManufactureDate.HasValue && sku?.ShelfLifeDays > 0)
				{
					stockBatch.ExpireDate = model.ManufactureDate.Value.AddDays(sku.ShelfLifeDays);
				}
				else
				{
					stockBatch.ExpireDate = null;
				}

				// 6. 更新異動人員與時間
				stockBatch.Reviser = 111111; // TODO: 改成登入使用者ID
				stockBatch.RevisedDate = DateTime.Now;

				_context.Update(stockBatch);
				await _context.SaveChangesAsync();

				// 7. 記錄庫存異動
				int changeQty = model.Qty - beforeQty;
				if (changeQty != 0)
				{
					_context.SupStockHistories.Add(new SupStockHistory
					{
						StockBatchId = stockBatch.StockBatchId,
						ChangeType = "Adjust",       // 編輯手動調整
						ChangeQty = changeQty,
						BeforeQty = beforeQty,
						AfterQty = model.Qty,
						Reviser = 111111,            // TODO: 改成登入使用者ID
						RevisedDate = DateTime.Now,
						Remark = model.Remark
					});

					await _context.SaveChangesAsync();
				}

				return Json(new { success = true, expireDate = stockBatch.ExpireDate });
			}
			catch (DbUpdateConcurrencyException)
			{
				return Json(new { success = false, message = "資料已被其他使用者修改" });
			}
			catch (DbUpdateException dbEx)
			{
				return Json(new { success = false, message = "資料庫更新失敗: " + dbEx.Message });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}


		private bool SupStockBatchExists(int id)
        {
            return _context.SupStockBatches.Any(e => e.StockBatchId == id);
        }
    }
}
