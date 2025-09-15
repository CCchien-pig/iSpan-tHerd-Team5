using FlexBackend.Core.DTOs.SUP;
using FlexBackend.Core.Interfaces.SUP;
using FlexBackend.Infra.Models;
using FlexBackend.SUP.Rcl.Areas.SUP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SupStockBatch = FlexBackend.Infra.Models.SupStockBatch;
using SupStockHistory = FlexBackend.Infra.Models.SupStockHistory;

namespace FlexBackend.SUP.Rcl.Areas.SUP.Controllers
{
	[Area("SUP")]
	public class StockBatchesController : Controller
	{
		private readonly tHerdDBContext _context;
		private readonly IStockBatchService _stockBatchService;
		private readonly IStockService _stockService;

		public StockBatchesController(tHerdDBContext context, IStockBatchService stockBatchService, IStockService stockService)
		{
			_context = context;
			_stockBatchService = stockBatchService;
			_stockService = stockService;
		}

		// GET: /SUP/StockBatches/Index
		[HttpGet]
		public IActionResult Index()
		{
			//ViewBag.SupplierId = supplierId;
			//ViewBag.BrandName = TempData["BrandName"] as string ?? "";

			return View();
		}

		// POST: StockBatches/IndexJson
		[HttpPost]
		public async Task<IActionResult> IndexJson([FromForm] string supplierId = null)
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
							SupplierId = b.SupplierId,

							sb.StockBatchId,
							SkuCode = sku.SkuCode,
							sb.BatchNumber,
							sb.ExpireDate,
							sb.Qty,
							sku.SafetyStockQty,
							sku.ReorderPoint,
							sb.IsSellable,

							CreatedDate = sb.CreatedDate, // <-- 用來排序 BatchNumber 的日期部分
														  
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

			// 如果有傳 SupplierId，就過濾對應品牌
			if (!string.IsNullOrEmpty(supplierId) && int.TryParse(supplierId, out int sId))
			{
				query = query.Where(x => x.SupplierId == sId);
			}

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
				// 點 BatchNumber 時改用 CreatedDate（或 ManufactureDate）排序，讓「按批號日期排序」正確
				1 => sortDirection == "asc" ? query.OrderBy(s => s.CreatedDate) : query.OrderByDescending(s => s.CreatedDate),
				2 => sortDirection == "asc" ? query.OrderBy(s => s.BrandName) : query.OrderByDescending(s => s.BrandName),
				3 => sortDirection == "asc" ? query.OrderBy(s => s.ProductName) : query.OrderByDescending(s => s.ProductName),
				4 => sortDirection == "asc" ? query.OrderBy(s => s.ExpireDate) : query.OrderByDescending(s => s.ExpireDate),
				5 => sortDirection == "asc" ? query.OrderBy(s => s.Qty) : query.OrderByDescending(s => s.Qty),
				6 => sortDirection == "asc" ? query.OrderBy(s => s.ReorderPoint) : query.OrderByDescending(s => s.ReorderPoint),
				7 => sortDirection == "asc" ? query.OrderBy(s => s.SafetyStockQty) : query.OrderByDescending(s => s.SafetyStockQty),
				_ => sortDirection == "asc" ? query.OrderBy(s => s.StockBatchId) : query.OrderByDescending(s => s.StockBatchId),
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
				}).ToArray() ?? Array.Empty<object>(),
				// 新增：允許的操作類型
				AllowedActions = new[] { "ADJUST" }, // 已有批號庫存，只允許手動調整

				CreatedDate = d.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")

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
			catch (Exception ex)
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

			var sku = await _context.ProdProductSkus
				.Include(s => s.Product)
					.ThenInclude(p => p.Brand)
				.FirstOrDefaultAsync(s => s.SkuId == vm.SkuId && s.IsActive);

			if (sku == null)
				return Json(new { success = false, message = "找不到 SKU" });

			if (!vm.ManufactureDate.HasValue)
				return Json(new { success = false, message = "請選擇製造日期" });

			if ((vm.ChangeQty ?? 0) <= 0)
				return Json(new { success = false, message = "異動數量必須大於 0" });

			// 上限
			int requestedQty = vm.ChangeQty.Value;

			// 非Adjust視為增加 (例如 Purchase)
			bool isAdd = vm.MovementType != "Adjust" ? true : vm.IsAdd;

			// 計算上限
			int maxAllowed;
			if (isAdd)
			{
				// 採購入庫及手動調整(增加)
				maxAllowed = sku.MaxStockQty - sku.StockQty;
				if (maxAllowed < 0) maxAllowed = 0;
			}
			else
			{
				// 手動調整(減少)
				maxAllowed = sku.StockQty;
				if (maxAllowed < 0) maxAllowed = 0;
			}

			// 如果 requestedQty 超過上限，直接矯正為上限（後端保險）
			if (requestedQty > maxAllowed)
				requestedQty = maxAllowed;

			// 如果矯正後為 0，代表沒有可操作數量 -> 回傳錯誤
			if (requestedQty <= 0)
			{
				string errMsg = isAdd
					? "已達最大庫存，無法再增加。"
					: "庫存不足，無法執行減少。";
				return Json(new { success = false, message = errMsg });
			}

			// 把 vm.ChangeQty 換成實際套用值（或直接傳 requestedQty 到 Service）
			vm.ChangeQty = requestedQty;


			string brandCode = sku?.Product?.Brand?.BrandCode ?? "XXX";

			var stockBatch = new SupStockBatch
			{
				SkuId = sku.SkuId,
				Qty = 0,
				ManufactureDate = vm.ManufactureDate,
				Creator = vm.UserId ?? 0,
				CreatedDate = DateTime.Now,
				BatchNumber = GenerateBatchNumber(brandCode)
			};

			if (sku.ShelfLifeDays > 0)
				stockBatch.ExpireDate = vm.ManufactureDate.Value.AddDays(sku.ShelfLifeDays);

			_context.SupStockBatches.Add(stockBatch);
			await _context.SaveChangesAsync(); // 確保 BatchNumber 已存入 DB

			List<SupStockMovementDto> batchMovements = new(); // 用於回傳

			// 處理異動
			if (!string.IsNullOrEmpty(vm.MovementType))
			{
				try
				{
					var result = await _stockService.AdjustStockAsync(
						stockBatch.StockBatchId,
						sku.SkuId,
						requestedQty,   // <-- 已被上限限制判斷過的正整數
						vm.IsAdd,
						vm.MovementType, // 新增傳入異動類型
						vm.UserId,
						vm.Remark
					);

					if (!result.Success)
					{
						return Json(new { success = false, message = result.Message });
					}

					// 使用服務回傳的 BatchMovements
					batchMovements = result.BatchMovements ?? new List<SupStockMovementDto>();

					return Json(new
					{
						success = true,
						batchMovements = batchMovements,
						batchNumber = stockBatch.BatchNumber,
						newQty = stockBatch.Qty,
						appliedChangeQty = result.AdjustedQty,  // 實際套用數量
						totalStockQty = result.TotalStock       // 實際總庫存
					});
				}
				catch (NotImplementedException ex)
				{
					return Json(new { success = false, message = ex.Message });
				}
			}

			return Json(new
			{
				success = true,
				batchMovements = new List<SupStockMovementDto>(), // 空陣列
				batchNumber = stockBatch.BatchNumber,
				newQty = stockBatch.Qty,
				totalStockQty = sku.StockQty
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

			// 查詢今日所有批號（不分品牌）
			var todayBatchNumbers = _context.SupStockBatches
				.Where(sb => sb.CreatedDate >= today && sb.CreatedDate < today.AddDays(1))
				.Select(sb => sb.BatchNumber)
				.ToList(); // 不篩品牌

			int maxSeq = 0;
			foreach (var bn in todayBatchNumbers)
			{
				var seqStr = bn.Split('-').Last();
				if (int.TryParse(seqStr, out int seqNum))
					maxSeq = Math.Max(maxSeq, seqNum);
			}

			int nextSeq = maxSeq + 1;

			return $"{brandCode}{datePart}-{nextSeq:000}";
		}


		#region 新增庫存表單用 API

		[HttpGet]
		public async Task<IActionResult> GetBrandNameBySupplier(int supplierId)
		{
			var brandNames = await _context.SupBrands
				.Where(b => b.SupplierId == supplierId && b.IsActive)
				.Select(b => new
				{
					b.BrandId,
					b.BrandName
				})
				.ToListAsync();

			if (brandNames == null || !brandNames.Any())
				return NotFound("找不到對應的品牌");

			return Ok(brandNames);
		}

		// 取得品牌列表
		// [HttpGet("brands")]
		// /Stock/brands
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
		// [HttpGet("brands/{brandId}/products")]
		// /api/brands/{brandId}/products
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
		// [HttpGet("products/{productId}/skus")]
		// /api/products/{productId}/skus
		[HttpGet]
		public async Task<IActionResult> GetSkusByProduct(int productId)
		{
			var skus = await _context.ProdProductSkus
				.Where(sku => sku.ProductId == productId && sku.IsActive)
				.Select(sku => new
				{
					sku.SkuId,
					sku.SkuCode,
					CurrentStock = sku.StockQty,      // 當前庫存
					ExpectedStock = 0,                // 預計庫存，初始為 0，可前端計算
					sku.ReorderPoint,                 // 再訂購點
					sku.SafetyStockQty,               // 安全庫存量
					sku.MaxStockQty,                  // 最大庫存量
					sku.IsAllowBackorder,             // 是否可缺貨預購
					IsSellable = sku.Product.IsPublished // 從商品表帶入
				})
				.ToListAsync();
			return Ok(skus);
		}


		// 取得 SKU 詳細資訊 (選完 SKU 後自動帶入底下欄位)
		// [HttpGet("skus/{skuId}")]
		// /api/skus/{skuId}
		[HttpGet]
		public async Task<IActionResult> GetSkuInfo(int skuId)
		{
			var skuInfo = await _context.ProdProductSkus
				.Where(sku => sku.SkuId == skuId && sku.IsActive)
				.Select(sku => new
				{
					sku.SkuId,
					sku.SkuCode,
					CurrentStock = sku.StockQty,
					ExpectedStock = 0,                // 預計庫存初始為 0
					sku.ReorderPoint,
					sku.SafetyStockQty,
					sku.MaxStockQty,
					sku.IsAllowBackorder
				})
				.FirstOrDefaultAsync();

			if (skuInfo == null)
				return NotFound("找不到 SKU");

			return Ok(skuInfo);
		}

		// 取得異動類型 (僅回傳 Purchase / Adjust)
		[HttpGet]
		public async Task<IActionResult> GetMovementTypes()
		{
			var allowedTypes = new[] { "Purchase", "Adjust" };

			var codes = await _context.SysCodes
				.Where(c => c.ModuleId == "SUP" && c.CodeId == "01" && c.IsActive && allowedTypes.Contains(c.CodeNo))
				.Select(c => new
				{
					Value = c.CodeNo,    // Purchase / Adjust
					Text = c.CodeDesc    // 採購入庫 / 手動調整
				})
				.ToListAsync();

			return Ok(codes);
		}

		#endregion


		// GET: /SUP/StockBatches/Edit/5
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			// 取得批號資料
			var dto = await _stockBatchService.GetStockBatchForEditAsync(id);
			if (dto == null)
				return Json(new { success = false, message = "找不到此批次" });

			// 取得最新一筆異動紀錄(含備註)（StockBatchService.GetLastRemarkAsync）
			var lastRemark = await _stockBatchService.GetLastRemarkAsync(id);

			// DTO → ViewModel
			var vm = new StockBatchContactViewModel
			{
				StockBatchId = dto.StockBatchId,
				SkuId = dto.SkuId,
				SkuCode = dto.SkuCode ?? "",
				ProductName = dto.ProductName ?? "",
				BrandName = dto.BrandName ?? "",
				BatchNumber = dto.BatchNumber ?? "",
				Qty = dto.Qty,                   // 當前庫存，只讀
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
				RevisedDate = dto.RevisedDate,
				Remark = lastRemark,            // 帶入最新異動備註
				PredictedQty = dto.Qty         // 預計庫存初始等於當前庫存
			};

			ViewBag.FormAction = "Edit";

			// 前端表單顯示建議：
			// - 品牌名、商品名、SkuCode、製造日期、當前庫存、最大庫存、再訂購點、安全庫存量、可缺貨預購 → 只讀
			// - 異動類型鎖定顯示 "手動調整"
			// - 可選 +/-、數量
			// - 備註可修改，送出時存到 SupStockHistory

			return PartialView("Partials/_StockBatchFormPartial", vm);
		}


		// POST： /SUP/StockBatches/SaveStockMovement
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SaveStockMovement(SupStockMovementDto dto)
		{
			try
			{
				var result = await _stockBatchService.SaveStockMovementAsync(dto);
				return Json(new { success = true, result });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}


		// POST: /SUP/StockBatches/Edit/5
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
				int currentQty = stockBatch.Qty;

				// 更新可修改欄位
				stockBatch.ManufactureDate = model.ManufactureDate;
				stockBatch.Reviser = model.UserId ?? 0; // 登入使用者
				stockBatch.RevisedDate = DateTime.Now;
				stockBatch.ExpireDate = model.ManufactureDate.HasValue && sku?.ShelfLifeDays > 0
					? model.ManufactureDate.Value.AddDays(sku.ShelfLifeDays)
					: null;

				// 計算變動量（只對 SupStockHistory）
				int changeQty = model.IsAdd ? (model.ChangeQty ?? 0) : -(model.ChangeQty ?? 0);
				if (changeQty != 0)
				{
					_context.SupStockHistories.Add(new SupStockHistory
					{
						StockBatchId = stockBatch.StockBatchId,
						ChangeType = "Adjust",
						ChangeQty = changeQty,
						BeforeQty = currentQty,
						AfterQty = currentQty + changeQty,
						Reviser = model.UserId ?? 0,
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
					currentQty = currentQty,              // 保持只讀
					predictedQty = currentQty + changeQty, // 前端顯示預計庫存
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
