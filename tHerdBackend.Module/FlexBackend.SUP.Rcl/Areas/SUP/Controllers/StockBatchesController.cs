using FlexBackend.Core.DTOs.SUP;
using FlexBackend.Core.Interfaces.SUP;
using FlexBackend.Infra.Models;
using FlexBackend.SUP.Rcl.Areas.SUP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Xml;
using SupStockBatch = FlexBackend.Infra.Models.SupStockBatch;
using SupStockHistory = FlexBackend.Infra.Models.SupStockHistory;

namespace FlexBackend.SUP.Rcl.Areas.SUP.Controllers
{
	[Area("SUP")]
	public class StockBatchesController : Controller
	{
		private readonly tHerdDBContext _context;
		private readonly IStockBatchService _stockBatchService;

		public StockBatchesController(tHerdDBContext context, IStockBatchService stockBatchService)
		{
			_context = context;
			_stockBatchService = stockBatchService;
		}

		// GET: /SUP/StockBatches/Index
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

			// 建立 join 查詢（SupStockBatches → Prod_ProductSku → ProdProducts → SupBrands）
			var query = from sb in _context.SupStockBatches
						join sku in _context.ProdProductSkus on sb.SkuId equals sku.SkuId
						join p in _context.ProdProducts on sku.ProductId equals p.ProductId
						join b in _context.SupBrands on p.BrandId equals b.BrandId
						select new
						{
							sb.StockBatchId,
							SkuCode = sku.SkuCode,
							sb.BatchNumber,
							sb.ExpireDate,
							sb.Qty,
							sku.SafetyStockQty,
							sku.ReorderPoint,
							sb.IsSellable,

							// 展開要的
							ProductName = p.ProductName,
							BrandName = b.BrandName,
							BrandCode = b.BrandCode,

							// 規格陣列 群組 + 規格選項
							Specifications = sb.Sku.SpecificationOptions
								.OrderBy(o => o.OrderSeq)
								.Select(o => new
								{
									GroupName = o.SpecificationConfig.GroupName,  // 群組名稱
									OptionName = o.OptionName                     // 選項名稱
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

			// 資料傳給前端
			var result = data.Select(d => new
			{
				d.StockBatchId,
				SkuCode = d.SkuCode ?? "",   // 保護 null
				d.BatchNumber,
				//d.ExpireDate,
				ExpireDate = d.ExpireDate?.ToString("yyyy-MM-dd"),
				d.Qty,
				SafetyStockQty = d.SafetyStockQty,  // 保護 null
				ReorderPoint = d.ReorderPoint,      // 保護 null

				// 展開列需要的
				d.BrandName,
				d.ProductName,
				// 字串版規格，用斜線分隔
				//Specifications = string.Join(" / ", d.Specifications),
				// 陣列版規格
				// 將 Specifications 轉成陣列，前端 render 時可用 map
				Specifications = d.Specifications?.Select(s => new
				{
					GroupName = s.GroupName ?? "",
					OptionName = s.OptionName ?? ""
				}).ToArray() ?? Array.Empty<object>()	
			}).ToList();

			// ====== 測試輸出 JSON 到 Debug ======
			System.Diagnostics.Debug.WriteLine(
				JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
			);

			return Json(new
			{
				draw,
				recordsTotal = totalRecords,
				recordsFiltered = totalRecords,
				data = result
			});
		}


		// GET: /SUP/StockBatches/Create
		[HttpGet]
		public IActionResult Create()
		{
			try
			{
				// 空物件給 Partial View 使用
				var viewModel = new StockBatchContactViewModel();

				ViewBag.FormAction = "Create"; // 告訴 Partial View 這是 Create 動作
				// return PartialView("Partials/_StockBatchFormPartial", viewModel); // 回傳 Partial View
				return PartialView("~/Areas/SUP/Views/StockBatches/Partials/_StockBatchFormPartial.cshtml", viewModel);
			}
			catch(Exception ex)
			{
				return Content("錯誤：" + ex.Message);
			}
		}

		// POST: /SUP/StockBatches/CreateStockBatch
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateStockBatch([FromForm] StockBatchContactViewModel vm)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
				return Json(new { success = false, message = "資料格式錯誤", errors });
			}

			// 1. 找 SKU（含產品 & 品牌）
			var sku = await _context.ProdProductSkus
				.Include(s => s.Product)
					.ThenInclude(p => p.Brand)
				.FirstOrDefaultAsync(s => s.SkuId == vm.SkuId);

			if (sku == null)
				return Json(new { success = false, message = "找不到 SKU" });

			// 2. 製造日期
			if (!vm.ManufactureDate.HasValue)
				return Json(new { success = false, message = "請選擇製造日期" });

			// 3. 新增 SupStockBatch（Qty 預設 0，之後根據異動再更新）（這裡就先產生批號）
			string brandCode = sku?.Product?.Brand?.BrandCode ?? "XXX";

			var stockBatch = new SupStockBatch
			{
				SkuId = vm.SkuId,
				Qty = 0,
				IsSellable = vm.IsSellable,
				ManufactureDate = vm.ManufactureDate,
				Creator = vm.UserId ?? 0,
				CreatedDate = DateTime.Now,
				BatchNumber = GenerateBatchNumber(brandCode) // 👈 先生成批號
			};

			if (sku.ShelfLifeDays > 0 && vm.ManufactureDate.HasValue)
				stockBatch.ExpireDate = vm.ManufactureDate.Value.AddDays(sku.ShelfLifeDays);

			_context.SupStockBatches.Add(stockBatch);
			await _context.SaveChangesAsync(); // 先存一次，拿到 StockBatchId

			// 4. 產生批號（從商品 → 品牌 → BrandCode）
			//string brandCode = sku?.Product?.Brand?.BrandCode ?? "XXX";
			stockBatch.BatchNumber = GenerateBatchNumber(brandCode);
			await _context.SaveChangesAsync();

			// 5. 異動邏輯（若 MovementType 有傳值）
			if (!string.IsNullOrEmpty(vm.MovementType))
			{
				// 將 nullable 轉成 int（若 null 則用 0）
				int changeQty = vm.ChangeQty ?? 0;

				// 驗證異動類型是否存在
				bool isValidMovementType = await _context.SysCodes
					.AnyAsync(c => c.ModuleId == "SUP" && c.CodeId == "01" && c.CodeNo == vm.MovementType);

				if (!isValidMovementType)
					return Json(new { success = false, message = $"無效的異動類型: {vm.MovementType}" });

				int beforeQty = stockBatch.Qty;
				int newQty = beforeQty;

				switch (vm.MovementType)
				{
					case "Purchase": // 進貨
						newQty += changeQty;
						break;
					case "Sale": // 銷售
						if (changeQty > beforeQty)
							return Json(new { success = false, message = "銷售出庫量不能大於現有庫存" });
						newQty -= changeQty;
						break;
					case "Return": // 退貨
						newQty += changeQty;
						break;
					case "Expire": // 報廢/過期
						newQty -= changeQty;
						break;
					case "Adjust": // 人工調整
						newQty += vm.IsAdd ? changeQty : -changeQty;
						break;
				}

				// 最大庫存檢查
				if (sku.MaxStockQty > 0 && newQty > sku.MaxStockQty)
					return Json(new { success = false, message = $"調整後庫存 {newQty} 超過最大庫存量 {sku.MaxStockQty}" });

				// 更新並存檔
				stockBatch.Qty = newQty;
				stockBatch.IsSellable = vm.IsSellable;
				await _context.SaveChangesAsync();

				// 新增歷史紀錄（使用 changeQty 確保型別一致）
				_context.SupStockHistories.Add(new SupStockHistory
				{
					StockBatchId = stockBatch.StockBatchId,
					ChangeType = vm.MovementType,
					ChangeQty = changeQty,
					BeforeQty = beforeQty,
					AfterQty = newQty,
					Reviser = vm.UserId ?? 0,
					RevisedDate = DateTime.Now,
					Remark = vm.Remark
				});
				await _context.SaveChangesAsync();
			}

			return Json(new
			{
				success = true,
				batchNumber = stockBatch.BatchNumber,
				newQty = stockBatch.Qty
			});
		}


		// 自動產生批號
		// 批號{BrandCode}{yyyymmdd}-{流水號}
		// 流水號每日統一遞增，不分品牌
		private string GenerateBatchNumber(string brandCode)
		{
			if (string.IsNullOrEmpty(brandCode))
				brandCode = "XX"; // 若無品牌簡碼，預設 XX

			var today = DateTime.Today;
			var datePart = today.ToString("yyyyMMdd");

			// 查詢今日目前最大流水號
			var maxSeq = _context.SupStockBatches
				.Where(sb => sb.CreatedDate >= today && sb.CreatedDate < today.AddDays(1))
				.Select(sb => sb.BatchNumber)
				.Where(bn => bn.StartsWith(brandCode + datePart))
				.OrderByDescending(bn => bn)
				.FirstOrDefault();

			int seq = 1;
			if (!string.IsNullOrEmpty(maxSeq))
			{
				// 取最後 3 碼數字部分
				var seqStr = maxSeq.Split('-').Last();
				if (int.TryParse(seqStr, out int lastSeq))
					seq = lastSeq + 1;
			}

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
					BrandId = b.BrandId,
					BrandName = b.BrandName
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
					sku.MaxStockQty,       // 最大庫存量
					IsSellable = sku.Product.IsPublished // 從商品表帶入
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


		// GET: /SUP/StockBatches/Edit/5
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			// 從 Service 拿 DTO
			SupStockBatchDto dto = await _stockBatchService.GetStockBatchForEditAsync(id);

			if (dto == null)
				return Json(new { success = false, message = "找不到此批次" });

			// DTO → ViewModel
			var vm = new StockBatchContactViewModel
			{
				StockBatchId = dto.StockBatchId,
				SkuId = dto.SkuId,
				SkuCode = dto.SkuCode ?? "",
				ProductName = dto.ProductName ?? "",
				BrandName = dto.BrandName ?? "",
				BatchNumber = dto.BatchNumber ?? "",
				Qty = dto.Qty,
				IsSellable = dto.IsSellable,
				ManufactureDate = dto.ManufactureDate,
				ShelfLifeDays = dto.ExpireDate.HasValue && dto.ManufactureDate.HasValue
								? (int)(dto.ExpireDate.Value - dto.ManufactureDate.Value).TotalDays
								: 0,
				SafetyStockQty = dto.SafetyStockQty,
				ReorderPoint = dto.ReorderPoint,
				MaxStockQty = dto.MaxStockQty,
				Creator = dto.Creator,
				CreatedDate = dto.CreatedDate,
				Reviser = dto.Reviser,
				RevisedDate = dto.RevisedDate
			};

			ViewBag.FormAction = "Edit";
			return PartialView("Partials/_StockBatchFormPartial", vm);
		}

		// POST： /SUP/StockBatches/SaveStockMovement
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SaveStockMovement(SupStockMovementDto dto)
		{
			if (dto == null)
				return Json(new { success = false, message = "資料無效" });
			if (dto.ChangeQty <= 0)
				return Json(new { success = false, message = "變動數量必須大於 0" });

			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				// 驗證 MovementType 是否有效
				bool isValidMovementType = await _context.SysCodes
					.AnyAsync(c => c.ModuleId == "SUP" && c.CodeId == "01" && c.CodeNo == dto.MovementType);
				if (!isValidMovementType)
					return Json(new { success = false, message = $"無效的 MovementType: {dto.MovementType}" });

				// 取得批次與 SKU
				var batch = await _context.SupStockBatches
					.Include(sb => sb.Sku)
						.ThenInclude(s => s.Product)
							.ThenInclude(p => p.Brand)
					.FirstOrDefaultAsync(sb => sb.StockBatchId == dto.StockBatchId);

				if (batch == null)
					return Json(new { success = false, message = $"找不到庫存批次: {dto.StockBatchId}" });

				int beforeQty = batch.Qty;
				int newQty = beforeQty;

				// 計算異動後庫存
				switch (dto.MovementType)
				{
					case "Purchase": newQty += dto.ChangeQty; break;
					case "Sale": newQty -= dto.ChangeQty; break;
					case "Return": newQty += dto.ChangeQty; break;
					case "Expire": newQty -= dto.ChangeQty; break;
					case "Adjust": newQty += dto.IsAdd ? dto.ChangeQty : -dto.ChangeQty; break;
					default:
						return Json(new { success = false, message = $"未知異動類型: {dto.MovementType}" });
				}

				// 後端驗證
				if (newQty < 0)
					return Json(new { success = false, message = "異動後庫存不能小於 0" });
				if (batch.Sku.MaxStockQty > 0 && newQty > batch.Sku.MaxStockQty)
					return Json(new { success = false, message = $"異動後庫存不能超過最大庫存量 {batch.Sku.MaxStockQty}" });

				// 更新批次
				batch.Qty = newQty;
				batch.IsSellable = dto.IsSellable;
				batch.Reviser = dto.UserId ?? 0;
				batch.RevisedDate = DateTime.Now;

				// 記錄庫存異動歷史
				_context.SupStockHistories.Add(new SupStockHistory
				{
					StockBatchId = batch.StockBatchId,
					ChangeType = dto.MovementType,
					ChangeQty = dto.ChangeQty,
					BeforeQty = beforeQty,
					AfterQty = newQty,
					Reviser = dto.UserId ?? 0,
					RevisedDate = DateTime.Now,
					Remark = dto.Remark
				});

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				// 回傳完整資料給前端
				return Json(new
				{
					success = true,
					StockBatchId = batch.StockBatchId,
					SkuCode = batch.Sku.SkuCode,
					ProductName = batch.Sku.Product.ProductName,
					BrandName = batch.Sku.Product.Brand.BrandName,
					BatchNumber = batch.BatchNumber,
					CurrentQty = batch.Qty,
					IsSellable = batch.IsSellable,
					ExpireDate = batch.ExpireDate,
					ChangeQty = newQty - beforeQty,
					MovementType = dto.MovementType,
					Remark = dto.Remark
				});
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return Json(new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}


		// POST: /SUP/StockBatches/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, StockBatchContactViewModel model)
		{
			if (id != model.StockBatchId)
				return Json(new { success = false, message = "找不到此異動紀錄Id" });
			if (!ModelState.IsValid)
				return PartialView("Partials/_StockBatchFormPartial", model);

			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				var stockBatch = await _context.SupStockBatches
					.Include(sb => sb.Sku)
					.FirstOrDefaultAsync(sb => sb.StockBatchId == id);

				if (stockBatch == null)
					return Json(new { success = false, message = "找不到資料" });

				var sku = stockBatch.Sku;
				int beforeQty = stockBatch.Qty;

				bool hasChanged = stockBatch.ManufactureDate != model.ManufactureDate ||
								  stockBatch.Qty != model.Qty ||
								  stockBatch.IsSellable != model.IsSellable;

				if (!hasChanged)
					return Json(new { success = false, message = "未變更" });

				// 更新批次
				stockBatch.ManufactureDate = model.ManufactureDate;
				stockBatch.Qty = model.Qty.Value;
				stockBatch.IsSellable = model.IsSellable;
				stockBatch.Reviser = 111111; // TODO: 改成登入使用者ID
				stockBatch.RevisedDate = DateTime.Now;
				stockBatch.ExpireDate = model.ManufactureDate.HasValue && sku?.ShelfLifeDays > 0
					? model.ManufactureDate.Value.AddDays(sku.ShelfLifeDays)
					: null;

				// 記錄異動
				int changeQty = model.Qty.Value - beforeQty;
				if (changeQty != 0)
				{
					_context.SupStockHistories.Add(new SupStockHistory
					{
						StockBatchId = stockBatch.StockBatchId,
						ChangeType = "Adjust",
						ChangeQty = changeQty,
						BeforeQty = beforeQty,
						AfterQty = model.Qty,
						Reviser = 111111, // TODO: 改成登入使用者ID
						RevisedDate = DateTime.Now,
						Remark = model.Remark
					});
				}

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				return Json(new
				{
					success = true,
					expireDate = stockBatch.ExpireDate,
					currentQty = stockBatch.Qty,
					isSellable = stockBatch.IsSellable,
					changeQty = changeQty
				});
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return Json(new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}


		private bool SupStockBatchExists(int id)
		{
			return _context.SupStockBatches.Any(e => e.StockBatchId == id);
		}
	}
}
