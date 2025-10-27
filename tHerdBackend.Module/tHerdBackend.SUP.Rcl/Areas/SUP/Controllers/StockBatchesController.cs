using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Text;
using System.Text.Json;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP.Stock;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;
using tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels;
using SupStockBatch = tHerdBackend.Infra.Models.SupStockBatch;
using SupStockHistory = tHerdBackend.Infra.Models.SupStockHistory;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.Controllers
{
	[Area("SUP")]
	public class StockBatchesController : Controller
	{
		private readonly tHerdDBContext _context;
		private readonly IStockBatchService _stockBatchService;
		private readonly IStockService _stockService;
		private readonly ICurrentUser _me;
		private readonly UserManager<ApplicationUser> _userMgr;

		public StockBatchesController(
			tHerdDBContext context,
			IStockBatchService stockBatchService,
			IStockService stockService,
			ICurrentUser me,
			UserManager<ApplicationUser> userMgr)
		{
			_context = context;
			_stockBatchService = stockBatchService;
			_stockService = stockService;
			_me = me;
			_userMgr = userMgr;
		}

		// GET: /SUP/StockBatches/Index
		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

		// POST: /SUP/StockBatches/IndexJson
		[HttpPost("SUP/StockBatches/IndexJson")]
		public async Task<IActionResult> IndexJson(
			[FromForm] string? supplierId = null,
			[FromForm] string? expireFilter = null,
			[FromForm] string? startDate = null,
			[FromForm] string? endDate = null)
		{
			try
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
								b.SupplierId,

								sb.StockBatchId,
								skuId = sku.SkuId,
								sku.SkuCode,
								sb.BatchNumber,
								sb.ExpireDate,
								sb.Qty,
								sku.SafetyStockQty,
								sku.ReorderPoint,
								//sb.IsSellable,
								p.IsPublished,

								sb.CreatedDate, // <-- 用來排序 BatchNumber 的日期部分
								sb.RevisedDate,
								// 非 null 排序欄位
								SortDate = sb.RevisedDate ?? sb.CreatedDate,

								// 展開要的
								p.ProductName,
								b.BrandName,
								b.BrandCode,

								// SKU 總庫存
								TotalStock = sku.StockQty,

								// SKU 顏色狀態
								BatchStockStatus = sku.StockQty < sku.SafetyStockQty ? "danger" :
									(sku.StockQty >= sku.SafetyStockQty && sku.StockQty < sku.ReorderPoint ? "low" : "normal"),

								// 批次庫存顏色狀態
								// SKU總庫存量 X，安全庫存 Y，再訂購點 Z
								// 後端 BatchStockStatus
								//BatchStockStatus = sku.StockQty < sku.SafetyStockQty ? "danger" :
								//   (sku.StockQty >= sku.SafetyStockQty && sku.StockQty < sku.ReorderPoint ? "low" : "normal"),

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

				// 到期日篩選
				var today = DateTime.Today;

				if (!string.IsNullOrEmpty(expireFilter))
				{
					switch (expireFilter)
					{
						case "valid":
							query = query.Where(x => !x.ExpireDate.HasValue || x.ExpireDate.Value.Date >= today);
							break;
						case "expired":
							query = query.Where(x => x.ExpireDate.HasValue && x.ExpireDate.Value.Date < today);
							break;
					}
				}

				// 建立日期篩選
				if (DateTime.TryParse(startDate, out var sDate))
				{
					query = query.Where(x => x.CreatedDate >= sDate);
				}
				if (DateTime.TryParse(endDate, out var eDate))
				{
					// 包含當天整天
					var endOfDay = eDate.AddDays(1);
					query = query.Where(x => x.CreatedDate < endOfDay);
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
				query = sortColumnIndex switch
				{
					0 => sortDirection == "asc" ? query.OrderBy(s => s.SortDate)
												: query.OrderByDescending(s => s.SortDate),
					1 => sortDirection == "asc" ? query.OrderBy(s => s.SkuCode) : query.OrderByDescending(s => s.SkuCode),
					2 => sortDirection == "asc" ? query.OrderBy(s => s.BatchNumber) : query.OrderByDescending(s => s.BatchNumber),
					3 => sortDirection == "asc" ? query.OrderBy(s => s.BrandName) : query.OrderByDescending(s => s.BrandName),
					4 => sortDirection == "asc" ? query.OrderBy(s => s.ProductName) : query.OrderByDescending(s => s.ProductName),
					5 => sortDirection == "asc" ? query.OrderBy(s => s.ExpireDate) : query.OrderByDescending(s => s.ExpireDate),
					6 => sortDirection == "asc" ? query.OrderBy(s => s.Qty) : query.OrderByDescending(s => s.Qty),
					7 => sortDirection == "asc" ? query.OrderBy(s => s.ReorderPoint) : query.OrderByDescending(s => s.ReorderPoint),
					8 => sortDirection == "asc" ? query.OrderBy(s => s.SafetyStockQty) : query.OrderByDescending(s => s.SafetyStockQty),
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
					d.skuId,
					d.BatchNumber,
					//d.ExpireDate,
					ExpireDate = d.ExpireDate?.ToString("yyyy-MM-dd"),
					RevisedDate = d.RevisedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
					SortDate = d.RevisedDate ?? d.CreatedDate,
					d.Qty,
					d.SafetyStockQty,  // 保護 null
					d.ReorderPoint,      // 保護 null

					// 展開列需要的
					d.BrandName,
					d.ProductName,

					d.TotalStock,

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
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				System.Diagnostics.Debug.WriteLine(ex.ToString());
				return StatusCode(500, ex.Message);
			}
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

		/// <summary>
		/// 採購入庫跟手動調整的
		/// </summary>
		// POST: /SUP/StockBatches/CreateStockBatch
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateStockBatch([FromForm] StockBatchContactViewModel vm)
		{
			try
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
				int requestedQty = vm.ChangeQty ?? 0;

				// 非Adjust視為增加 (例如 Purchase)
				bool isAdd = vm.MovementType != "Adjust" || vm.IsAdd;

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

				// 把 vm.ChangeQty 換成實際套用值
				vm.ChangeQty = requestedQty;

				string brandCode = sku?.Product?.Brand?.BrandCode ?? "XXX";

				var userId = _me.Id; // Claims 裡的 Id
				var user = await _userMgr.Users
					.AsNoTracking()
					.FirstOrDefaultAsync(u => u.Id == userId);

				if (user == null)
					return Json(new { success = false, message = "找不到使用者資料" });

				int currentUserId = user.UserNumberId;
				//int currentUserId = 1004;

				var stockBatch = new SupStockBatch
				{
					SkuId = sku.SkuId,
					Qty = 0,
					ManufactureDate = vm.ManufactureDate,
					Creator = currentUserId,
					CreatedDate = DateTime.Now,
					BatchNumber = GenerateBatchNumber(brandCode)
				};

				if (sku.ShelfLifeDays > 0)
					stockBatch.ExpireDate = vm.ManufactureDate.Value.AddDays(sku.ShelfLifeDays);

				_context.SupStockBatches.Add(stockBatch);

				try
				{
					await _context.SaveChangesAsync();
				}
				catch (Exception ex)
				{
					return Json(new { success = false, message = "儲存失敗：" + ex.Message });
				}

				// 初始化回傳
				List<SupStockMovementDto> batchMovements = [];
				int appliedChangeQty = 0;
				int totalStockQty = sku.StockQty;

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
							vm.MovementType, // 傳入異動類型
							currentUserId,
							vm.Remark ?? string.Empty
						);

						if (!result.Success)
						{
							return Json(new { success = false, message = result.Message });
						}

						// 使用服務回傳的 BatchMovements
						batchMovements = result.BatchMovements ?? new List<SupStockMovementDto>();

						appliedChangeQty = result.AdjustedQty;
						totalStockQty = result.TotalStock;
					}
					catch (NotImplementedException ex)
					{
						return Json(new { success = false, message = ex.Message });
					}
				}

				// 統一回傳格式
				return Json(new
				{
					success = true,
					batchMovements,
					batchNumber = stockBatch.BatchNumber,
					newQty = stockBatch.Qty,
					appliedChangeQty,   // 實際套用數量
					totalStockQty       // 實際總庫存
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "伺服器錯誤：" + ex.Message });
			}
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
				.Where(b => b.SupplierId == supplierId)
				.Select(b => new
				{
					b.BrandId,
					b.BrandName,
					b.IsActive // 回傳啟用狀態
				})
				.ToListAsync();

			//if (brandNames == null || !brandNames.Any())
			//	return NotFound("找不到對應的品牌");

			return Ok(brandNames);
		}

		// 取得品牌列表
		// [HttpGet("brands")]
		// /Stock/brands
		[HttpGet]
		public async Task<IActionResult> GetBrands()
		{
			var brands = await _context.SupBrands
				//.Where(b => b.IsActive)
				.Select(b => new
				{
					b.BrandId,
					b.BrandName,

					IsSupplierActive = b.Supplier != null ? b.Supplier.IsActive : (bool?)null  // 取得供應商狀態
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
				.Where(sku => sku.ProductId == productId)
				.Select(sku => new
				{
					sku.SkuId,
					sku.SkuCode,
					CurrentStock = sku.StockQty,      // 當前庫存qty
													  //TotalStock = sku.StockQty,        // 這裡改成整個 SKU 的總庫存量
					ExpectedStock = 0,                // 預計庫存，初始為 0，可前端計算
					sku.ReorderPoint,                 // 再訂購點
					sku.SafetyStockQty,               // 安全庫存量
					sku.MaxStockQty,                  // 最大庫存量
					sku.IsAllowBackorder,             // 是否可缺貨預購
					IsSellable = sku.Product.IsPublished, // 從商品表帶入
														  //StockStatus = sku.StockQty <= sku.SafetyStockQty ? "danger" :
														  //	  sku.StockQty <= sku.ReorderPoint ? "low" : "normal"
				})
				.ToListAsync();
			return Ok(skus);
		}

		// 取得 SKU 詳細資訊 (選完 SKU 後自動帶入底下欄位)
		[Route("SUP/StockBatches/GetSkuInfo")]
		[HttpGet]
		public async Task<IActionResult> GetSkuInfo(int skuId)
		{
			var skuInfo = await _context.ProdProductSkus
				.Where(sku => sku.SkuId == skuId && sku.IsActive)
				.Select(sku => new
				{
					sku.SkuId,
					sku.SkuCode,
					CurrentStock = sku.StockQty,      // 批次庫存 qty
													  //TotalStock = sku.StockQty,        // 這裡改成整個 SKU 的總庫存量
					ExpectedStock = 0,                // 預計庫存初始為 0
					sku.ReorderPoint,
					sku.SafetyStockQty,
					sku.MaxStockQty,
					sku.IsAllowBackorder,
					//StockStatus = sku.StockQty <= sku.SafetyStockQty ? "danger" :
					//	sku.StockQty <= sku.ReorderPoint ? "low" : "normal"					
				})
				.FirstOrDefaultAsync();

			if (skuInfo == null)
				return NotFound("找不到 SKU");

			return Ok(skuInfo);
		}

		// 取得異動類型 (僅回傳 Purchase / Adjust)
		// GET: /SUP/StockBatches/GetMovementTypes
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

		// POST： /SUP/StockBatches/GetAllStockHistory
		[HttpPost("SUP/StockBatches/GetAllStockHistory")]
		public async Task<IActionResult> GetAllStockHistory(
			[FromForm] string? supplierId = null,
			[FromForm] string? expireFilter = null,
			[FromForm] string? startDate = null,
			[FromForm] string? endDate = null)
		{
			// 1. DataTables 參數
			var draw = Request.Form["draw"].FirstOrDefault() ?? "1";
			var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
			var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
			var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
			var sortColumnIndex = Convert.ToInt32(Request.Form["order[0][column]"].FirstOrDefault() ?? "0");
			var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault() ?? "asc";

			// 2. 基本查詢（join 相關表）
			var query = from h in _context.SupStockHistories
						join sb in _context.SupStockBatches on h.StockBatchId equals sb.StockBatchId
						join sku in _context.ProdProductSkus on sb.SkuId equals sku.SkuId
						join p in _context.ProdProducts on sku.ProductId equals p.ProductId
						join b in _context.SupBrands on p.BrandId equals b.BrandId
						select new
						{
							h.StockHistoryId,
							sb.StockBatchId,
							sku.SkuCode,
							sb.BatchNumber,
							b.BrandName,
							sb.ExpireDate,
							h.ChangeType,
							h.BeforeQty,
							h.ChangeQty,
							h.AfterQty,
							h.RevisedDate,
							h.Reviser,
							b.BrandCode,
							b.SupplierId
						};

			// 3. 篩選供應商
			if (!string.IsNullOrEmpty(supplierId) && int.TryParse(supplierId, out int sId))
			{
				query = query.Where(x => x.SupplierId == sId);
			}

			// 4. 到期日篩選
			if (!string.IsNullOrEmpty(expireFilter))
			{
				var today = DateTime.Today;
				if (expireFilter == "valid")
					query = query.Where(x => !x.ExpireDate.HasValue || x.ExpireDate.Value.Date >= today);
				else if (expireFilter == "expired")
					query = query.Where(x => x.ExpireDate.HasValue && x.ExpireDate.Value.Date < today);
			}

			// RevisedDate 篩選
			if (DateTime.TryParse(startDate, out var sDate))
			{
				query = query.Where(x => x.RevisedDate >= sDate);
			}
			if (DateTime.TryParse(endDate, out var eDate))
			{
				// 包含當天整天
				var endOfDay = eDate.AddDays(1);
				query = query.Where(x => x.RevisedDate < endOfDay);
			}

			// 5. 搜尋
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(x =>
					EF.Functions.Like(x.SkuCode, $"%{searchValue}%") ||
					EF.Functions.Like(x.BrandName, $"%{searchValue}%") ||
					EF.Functions.Like(x.BatchNumber, $"%{searchValue}%") ||
					EF.Functions.Like(x.ChangeType, $"%{searchValue}%") ||
					EF.Functions.Like((x.Reviser.HasValue ? x.Reviser.Value.ToString() : ""), $"%{searchValue}%")
				);
			}

			var totalRecords = await query.CountAsync();

			// 6. 排序
			query = sortColumnIndex switch
			{
				0 => sortDirection == "asc" ? query.OrderBy(x => x.SkuCode) : query.OrderByDescending(x => x.SkuCode),
				1 => sortDirection == "asc" ? query.OrderBy(x => x.BatchNumber) : query.OrderByDescending(x => x.BatchNumber),
				2 => sortDirection == "asc" ? query.OrderBy(x => x.ExpireDate) : query.OrderByDescending(x => x.ExpireDate),
				3 => sortDirection == "asc" ? query.OrderBy(x => x.ChangeType) : query.OrderByDescending(x => x.ChangeType),
				4 => sortDirection == "asc" ? query.OrderBy(x => x.BeforeQty) : query.OrderByDescending(x => x.BeforeQty),
				5 => sortDirection == "asc" ? query.OrderBy(x => x.ChangeQty) : query.OrderByDescending(x => x.ChangeQty),
				6 => sortDirection == "asc" ? query.OrderBy(x => x.AfterQty) : query.OrderByDescending(x => x.AfterQty),
				7 => sortDirection == "asc" ? query.OrderBy(x => x.RevisedDate) : query.OrderByDescending(x => x.RevisedDate),
				8 => sortDirection == "asc" ? query.OrderBy(x => x.Reviser) : query.OrderByDescending(x => x.Reviser),
				_ => sortDirection == "asc" ? query.OrderBy(x => x.StockHistoryId) : query.OrderByDescending(x => x.StockHistoryId),
			};

			// 7. 分頁
			var data = await query.Skip(start).Take(length).ToListAsync();

			// 8. 回傳 JSON
			return Json(new
			{
				draw,
				recordsTotal = totalRecords,
				recordsFiltered = totalRecords,
				data
			});
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

		// GET: /SUP/StockBatches/Edit/5
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			try
			{
				// 取得該批號資料
				var stockBatchWithInfo = await (
					from sb in _context.SupStockBatches
					join sku in _context.ProdProductSkus on sb.SkuId equals sku.SkuId
					join prod in _context.ProdProducts on sku.ProductId equals prod.ProductId
					join brand in _context.SupBrands on prod.BrandId equals brand.BrandId
					where sb.StockBatchId == id
					select new StockBatchEditDto
					{
						StockBatchId = sb.StockBatchId,
						BrandName = brand.BrandName,
						ProductName = prod.ProductName,
						SkuCode = sku.SkuCode,
						ManufactureDate = sb.ManufactureDate,
						CurrentQty = sb.Qty,
						MaxStockQty = sku.MaxStockQty,
						IsAdd = true, // 預設為增加
					}
				).FirstOrDefaultAsync();


				if (stockBatchWithInfo == null)
					return NotFound();

				// 取得該批號在 SUP_StockHistory 的最後一次備註
				var lastRemark = await _context.SupStockHistories
					.Where(h => h.StockBatchId == id)
					.OrderByDescending(h => h.RevisedDate)
					.Select(h => h.Remark)
					.FirstOrDefaultAsync();

				stockBatchWithInfo.Remark = lastRemark ?? "";

				// 回傳 PartialView
				return PartialView("~/Areas/SUP/Views/StockBatches/Partials/_StockBatchEditPartial.cshtml", stockBatchWithInfo);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message, "Edit 發生錯誤 (StockBatchId={id})", id);
				return StatusCode(500, "發生內部錯誤");
			}
		}

		public class StockBatchUpdateDto
		{
			public int ChangeQty { get; set; }
			public bool IsAdd { get; set; }
			public string? Remark { get; set; }
		}

		// POST: /SUP/StockBatches/Update
		[HttpPost]
		public async Task<JsonResult> Update(int id, StockBatchUpdateDto dto)
		{
			// 取得批號
			var batch = await _context.SupStockBatches.FindAsync(id);
			if (batch == null)
				return Json(new { success = false, message = "找不到批號" });

			int beforeQty = batch.Qty;

			// 增加/減少批號庫存
			int changeQty = dto.IsAdd ? dto.ChangeQty : -dto.ChangeQty;
			batch.Qty += changeQty;
			int afterQty = batch.Qty;

			// 使用者資訊
			var userId = _me.Id;
			var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
			if (user == null)
				return Json(new { success = false, message = "找不到使用者資料" });
			int currentUserId = user.UserNumberId;

			// 異動紀錄
			_context.SupStockHistories.Add(new SupStockHistory
			{
				StockBatchId = id,
				ChangeType = "Adjust",
				ChangeQty = changeQty,
				BeforeQty = beforeQty,
				AfterQty = afterQty,
				Remark = dto.Remark,
				Reviser = currentUserId,
				RevisedDate = DateTime.Now
			});

			// 更新 SKU 總庫存：依異動量增減，而不是重新 sum
			var sku = await _context.ProdProductSkus.FirstOrDefaultAsync(s => s.SkuId == batch.SkuId);
			if (sku != null)
			{
				sku.StockQty += changeQty; // 直接累加變動量
				_context.Update(sku);
			}

			// 一次提交：批號 + 異動紀錄 + SKU 總庫存
			await _context.SaveChangesAsync();

			return Json(new { success = true });
		}

		// GET: /SUP/StockBatches/Remark/5
		[HttpGet]
		public async Task<JsonResult> Remark(int id)
		{
			var stockBatch = await (
				from sb in _context.SupStockBatches
				join sku in _context.ProdProductSkus on sb.SkuId equals sku.SkuId
				join prod in _context.ProdProducts on sku.ProductId equals prod.ProductId
				join brand in _context.SupBrands on prod.BrandId equals brand.BrandId
				where sb.StockBatchId == id
				select new
				{
					sb.StockBatchId,
					sb.BatchNumber,
					brand.BrandName,
					prod.ProductName,
					sku.SkuCode,
					sb.ManufactureDate,
					CurrentQty = sb.Qty
				}
			).FirstOrDefaultAsync();

			if (stockBatch == null)
				return Json(new { success = false, message = "找不到批號資料" });

			// 取得最後一次異動（包含 StockHistoryId）
			var lastHistory = await _context.SupStockHistories
				.Where(h => h.StockBatchId == id)
				.OrderByDescending(h => h.RevisedDate)
				.Select(h => new { h.StockHistoryId, h.Remark })
				.FirstOrDefaultAsync();

			return Json(new
			{
				success = true,
				data = new
				{
					stockBatch.StockBatchId,
					stockBatch.BatchNumber,
					stockBatch.BrandName,
					stockBatch.ProductName,
					stockBatch.SkuCode,
					stockBatch.ManufactureDate,
					stockBatch.CurrentQty,
					Remark = lastHistory?.Remark ?? "",
					lastHistory?.StockHistoryId
				}
			});
		}

		// POST: /SUP/StockBatches/UpdateHistoryRemark
		[HttpPost]
		public async Task<JsonResult> UpdateHistoryRemark(StockHistoryRemarkDto dto)
		{
			// 前端直接傳最後一次異動的ID
			var history = await _context.SupStockHistories.FindAsync(dto.StockHistoryId);
			if (history == null)
				return Json(new { success = false, message = "找不到該異動紀錄" });

			history.Remark = dto.Remark;
			history.RevisedDate = DateTime.Now;

			var userId = _me.Id;
			var user = await _userMgr.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
				return Json(new { success = false, message = "找不到使用者資料" });

			history.Reviser = user.UserNumberId;

			await _context.SaveChangesAsync();

			return Json(new { success = true, stockHistoryId = history.StockHistoryId });
		}

		private bool SupStockBatchExists(int id)
		{
			return _context.SupStockBatches.Any(e => e.StockBatchId == id);
		}

		#region 匯入功能

		// 下載匯入範例(.xlsx)
		// GET: /SUP/StockBatches/DownloadStockBatchTemplateExcel
		[HttpGet]
		public IActionResult DownloadStockBatchTemplateExcel()
		{
			// 設定 EPPlus 非商業授權（個人）
			ExcelPackage.License.SetNonCommercialPersonal("<Your Name>");

			using var package = new ExcelPackage();
			var ws = package.Workbook.Worksheets.Add("StockBatchTemplate");

			// 表頭
			ws.Cells[1, 1].Value = "SkuCode (必填)";
			ws.Cells[1, 2].Value = "數量 (必填)";
			ws.Cells[1, 3].Value = "製造日期 (可選)";
			ws.Cells[1, 4].Value = "備註 (可選)";

			// 設定表頭樣式
			using (var range = ws.Cells[1, 1, 1, 4])
			{
				range.Style.Font.Bold = true;
				range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
			}

			// 調整欄寬
			ws.Column(1).Width = 25; // SkuCode
			ws.Column(2).Width = 15; // Qty
			ws.Column(3).Width = 20; // MfgDate
			ws.Column(4).Width = 15; // Remark
			ws.Column(5).Width = 25;

			// 範例資料
			ws.Cells[2, 1].Value = "AN-Y-P1180-0002-M";
			ws.Cells[2, 2].Value = 20;
			ws.Cells[2, 3].Value = DateTime.Today;
			ws.Cells[2, 3].Style.Numberformat.Format = "yyyy/MM/dd"; // 設定為日期格式
			ws.Cells[2, 4].Value = "Import";
			ws.Cells[2, 5].Value = "←為範例，請勿刪除";

			var stream = new MemoryStream();
			package.SaveAs(stream);
			stream.Position = 0;

			// 回傳檔案
			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				"StockBatchTemplate.xlsx"
			);
		}

		// 下載匯入範例(.csv)
		// GET: /SUP/StockBatches/DownloadStockBatchTemplateCsv
		[HttpGet]
		public IActionResult DownloadStockBatchTemplateCsv()
		{
			var sb = new StringBuilder();

			// 表頭
			sb.AppendLine("SkuCode (必填),數量 (必填),製造日期 (可選),備註 (可選),說明");

			// 範例資料
			sb.AppendLine($"AN-Y-P1180-0002-M,20,{DateTime.Today:yyyy/MM/dd},Import,←為範例，請勿刪除");

			// UTF-8 + BOM
			var bytes = Encoding.UTF8.GetPreamble()
						.Concat(Encoding.UTF8.GetBytes(sb.ToString()))
						.ToArray();

			return File(bytes, "text/csv", "StockBatchTemplate.csv");
		}


		// 匯入檔案上傳
		// 新增批次與異動紀錄
		// 回傳每筆成功或失敗訊息
		// POST: /SUP/StockBatches/ImportStockBatchExcel
		[HttpPost]
		public async Task<IActionResult> ImportStockBatchExcel(IFormFile file)
		{
			// 設定 EPPlus 非商業授權（個人）
			ExcelPackage.License.SetNonCommercialPersonal("<Your Name>");

			if (file == null || file.Length == 0)
				return Json(new { success = false, message = "沒有收到檔案，請選擇檔案" });

			// 在 list 中存原始 Excel 列號
			var list = new List<(int Row, string SkuCode, int Qty, DateTime? MfgDate, string Remark)>();

			var successResults = new List<(int Row, string Message)>();
			var failResults = new List<(int Row, string Message)>();

			try
			{
				using (var stream = new MemoryStream())
				{
					await file.CopyToAsync(stream);
					using var package = new ExcelPackage(stream);
					var ws = package.Workbook.Worksheets.FirstOrDefault();
					if (ws == null)
						return Json(new { success = false, message = "Excel 無工作表" });

					//int row = 3; // 第一列是標題，第二列是範例，從第三列開始讀

					// 讀Excel，檢查SKU與數量必填					
					int startRow = 3; // 第一列標題，第二列範例
					int endRow = ws.Dimension.End.Row; // 最後一列
					for (int row = startRow; row <= endRow; row++)
					{
						// 判斷該列是否完全為空 (前四欄)
						bool isEmpty = true;
						for (int col = 1; col <= 4; col++)
						{
							if (!string.IsNullOrWhiteSpace(ws.Cells[row, col]?.Text))
							{
								isEmpty = false;
								break;
							}
						}
						if (isEmpty)
							continue; // 整列空就跳過

						string skuCode = ws.Cells[row, 1].Text.Trim();
						string qtyText = ws.Cells[row, 2].Text.Trim();
						int qty = int.TryParse(qtyText, out int q) ? q : 0;

						DateTime? mfgDate = null;
						if (DateTime.TryParse(ws.Cells[row, 3]?.Text?.Trim(), out var dt))
							mfgDate = dt;

						string remark = ws.Cells[row, 4]?.Text?.Trim() ?? "";

						if (string.IsNullOrEmpty(skuCode) || qty <= 0)
						{
							failResults.Add((row, $"第 {row} 列資料格式錯誤，必須同時填寫 SkuCode 與數量"));
							continue;
						}

						list.Add((row, skuCode, qty, mfgDate, remark));
					}
				}

				foreach (var item in list)
				{
					try
					{
						var sku = await _context.ProdProductSkus
							.Include(s => s.Product)
								.ThenInclude(p => p.Brand)
							.FirstOrDefaultAsync(s => s.SkuCode == item.SkuCode);

						if (sku == null)
						{
							failResults.Add((item.Row, $"找不到 SKU: {item.SkuCode}"));
							continue;
						}

						string batchNumber = GenerateBatchNumber(sku.Product?.Brand?.BrandCode ?? "XX");

						var userId = _me.Id;
						var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
						if (user == null)
						{
							failResults.Add((item.Row, $"找不到 userId"));
							continue;
						}
						int currentUserId = user.UserNumberId;

						DateTime? expireDate = null;
						if (item.MfgDate.HasValue && sku.ShelfLifeDays > 0)
							expireDate = item.MfgDate.Value.AddDays(sku.ShelfLifeDays);

						// 建立最少資訊批次
						var stockBatch = new SupStockBatch
						{
							SkuId = sku.SkuId,
							BatchNumber = batchNumber,
							Qty = 0, // Qty 交給 service 更新
							ManufactureDate = item.MfgDate,
							ExpireDate = expireDate,
							Creator = currentUserId,
							CreatedDate = DateTime.Now
						};
						_context.SupStockBatches.Add(stockBatch);
						await _context.SaveChangesAsync();

						// 呼叫 service 入庫 + 異動紀錄
						var adjustResult = await _stockService.AdjustStockAsync(
							stockBatch.StockBatchId,
							sku.SkuId,
							item.Qty,
							isAdd: true,
							movementType: "Purchase",
							reviserId: currentUserId,
							remark: string.IsNullOrWhiteSpace(item.Remark) ? "Import" : item.Remark
						);

						if (!adjustResult.Success)
						{
							failResults.Add((item.Row, $"第 {item.Row} 列匯入失敗。SkuCode：{item.SkuCode}, 原因: {adjustResult.Message}"));
							continue;
						}

						successResults.Add((item.Row, $"第 {item.Row} 列匯入成功。SkuCode：{item.SkuCode}, 數量：{item.Qty}"));
					}
					catch (Exception ex)
					{
						failResults.Add((item.Row, $"第 {item.Row} 列匯入失敗, 原因: {ex.Message}"));
					}
				}

				// 依行號排序
				successResults = successResults.OrderBy(r => r.Row).ToList();
				failResults = failResults.OrderBy(r => r.Row).ToList();

				var messages = new List<string>();
				if (successResults.Any())
				{
					messages.Add("----匯入成功----");
					messages.AddRange(successResults.Select(r => r.Message));
				}
				if (failResults.Any())
				{
					messages.Add("----匯入失敗----");
					messages.AddRange(failResults.Select(r => r.Message));
				}

				return Json(new { success = true, messages });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "匯入過程發生錯誤: " + ex.Message });
			}
		}

		// POST: /SUP/StockBatches/ImportStockBatchCsv
		[HttpPost]
		public async Task<IActionResult> ImportStockBatchCsv(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return Json(new { success = false, message = "沒有收到檔案，請選擇檔案" });

			var list = new List<(int Row, string SkuCode, int Qty, DateTime? MfgDate, string Remark)>();
			var successResults = new List<(int Row, string Message)>();
			var failResults = new List<(int Row, string Message)>();

			try
			{
				using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
				{
					int row = 0;
					while (!reader.EndOfStream)
					{
						row++;
						var line = await reader.ReadLineAsync();
						if (string.IsNullOrWhiteSpace(line))
							continue;

						// CSV 拆欄
						var columns = line.Split(',');

						// 第一列是表頭，第二列是範例
						if (row <= 2)
							continue;

						string skuCode = columns.Length > 0 ? columns[0].Trim() : "";
						string qtyText = columns.Length > 1 ? columns[1].Trim() : "";
						int qty = int.TryParse(qtyText, out int q) ? q : 0;

						DateTime? mfgDate = null;
						if (columns.Length > 2 && DateTime.TryParse(columns[2]?.Trim(), out var dt))
							mfgDate = dt;

						string remark = columns.Length > 3 ? columns[3].Trim() : "";

						if (string.IsNullOrEmpty(skuCode) || qty <= 0)
						{
							failResults.Add((row, $"第 {row} 列資料格式錯誤，必須同時填寫 SkuCode 與數量"));
							continue;
						}

						list.Add((row, skuCode, qty, mfgDate, remark));
					}
				}

				foreach (var item in list)
				{
					try
					{
						var sku = await _context.ProdProductSkus
							.Include(s => s.Product)
								.ThenInclude(p => p.Brand)
							.FirstOrDefaultAsync(s => s.SkuCode == item.SkuCode);

						if (sku == null)
						{
							failResults.Add((item.Row, $"找不到 SKU: {item.SkuCode}"));
							continue;
						}

						string batchNumber = GenerateBatchNumber(sku.Product?.Brand?.BrandCode ?? "XX");

						var userId = _me.Id;
						var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
						if (user == null)
						{
							failResults.Add((item.Row, $"找不到 userId"));
							continue;
						}
						int currentUserId = user.UserNumberId;

						DateTime? expireDate = null;
						if (item.MfgDate.HasValue && sku.ShelfLifeDays > 0)
							expireDate = item.MfgDate.Value.AddDays(sku.ShelfLifeDays);

						var stockBatch = new SupStockBatch
						{
							SkuId = sku.SkuId,
							BatchNumber = batchNumber,
							Qty = 0,
							ManufactureDate = item.MfgDate,
							ExpireDate = expireDate,
							Creator = currentUserId,
							CreatedDate = DateTime.Now
						};
						_context.SupStockBatches.Add(stockBatch);
						await _context.SaveChangesAsync();

						var adjustResult = await _stockService.AdjustStockAsync(
							stockBatch.StockBatchId,
							sku.SkuId,
							item.Qty,
							isAdd: true,
							movementType: "Purchase",
							reviserId: currentUserId,
							remark: string.IsNullOrWhiteSpace(item.Remark) ? "Import" : item.Remark
						);

						if (!adjustResult.Success)
						{
							failResults.Add((item.Row, $"第 {item.Row} 列匯入失敗。SkuCode：{item.SkuCode}, 原因: {adjustResult.Message}"));
							continue;
						}

						successResults.Add((item.Row, $"第 {item.Row} 列匯入成功。SkuCode：{item.SkuCode}, 數量：{item.Qty}"));
					}
					catch (Exception ex)
					{
						failResults.Add((item.Row, $"第 {item.Row} 列匯入失敗, 原因: {ex.Message}"));
					}
				}

				// 排序
				successResults = successResults.OrderBy(r => r.Row).ToList();
				failResults = failResults.OrderBy(r => r.Row).ToList();

				var messages = new List<string>();
				if (successResults.Any())
				{
					messages.Add("----匯入成功----");
					messages.AddRange(successResults.Select(r => r.Message));
				}
				if (failResults.Any())
				{
					messages.Add("----匯入失敗----");
					messages.AddRange(failResults.Select(r => r.Message));
				}

				return Json(new { success = true, messages });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "匯入過程發生錯誤: " + ex.Message });
			}
		}


		#endregion

		#region 匯出功能
		[HttpGet]
		public async Task<IActionResult> ExportStockFiltered(
			string? searchValue = null,
			string? startDate = null,
			string? endDate = null,
			string? expireFilter = null,
			string type = "csv")
		{
			var query = from sb in _context.SupStockBatches
						join sku in _context.ProdProductSkus on sb.SkuId equals sku.SkuId
						join p in _context.ProdProducts on sku.ProductId equals p.ProductId
						join b in _context.SupBrands on p.BrandId equals b.BrandId
						select new
						{
							sb.StockBatchId,
							SKU = sku.SkuCode,
							sb.BatchNumber,
							p.ProductName,
							b.BrandName,
							b.BrandCode,
							sb.Qty,
							sb.ExpireDate,
							sb.ManufactureDate,
							sb.CreatedDate
						};

			var today = DateTime.Today;

			// 到期日篩選
			if (!string.IsNullOrEmpty(expireFilter))
			{
				switch (expireFilter)
				{
					case "valid":
						query = query.Where(x => !x.ExpireDate.HasValue || x.ExpireDate.Value.Date >= today);
						break;
					case "expired":
						query = query.Where(x => x.ExpireDate.HasValue && x.ExpireDate.Value.Date < today);
						break;
				}
			}

			// 建立日期篩選
			if (DateTime.TryParse(startDate, out var sDate))
				query = query.Where(x => x.CreatedDate >= sDate);

			if (DateTime.TryParse(endDate, out var eDate))
			{
				var endOfDay = eDate.AddDays(1);
				query = query.Where(x => x.CreatedDate < endOfDay);
			}

			// 全局搜尋
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(s =>
					EF.Functions.Like(s.SKU, $"%{searchValue}%") ||
					EF.Functions.Like(s.BatchNumber, $"%{searchValue}%") ||
					EF.Functions.Like(s.ProductName, $"%{searchValue}%") ||
					EF.Functions.Like(s.BrandName, $"%{searchValue}%") ||
					EF.Functions.Like(s.BrandCode, $"%{searchValue}%")
				);
			}

			var data = await query.ToListAsync();

			return type.ToLower() switch
			{
				"csv" => ExportCsv(data),
				"excel" => ExportExcel(data),
				"pdf" => await ExportPdf(
					searchValue ?? string.Empty,
					startDate ?? string.Empty,
					endDate ?? string.Empty,
					expireFilter ?? string.Empty
				), // 傳搜尋與篩選條件
				_ => BadRequest("未知匯出類型")
			};
		}

		[HttpGet]
		public async Task<IActionResult> ExportHistoryFiltered(
			string? searchValue = null,
			string? startDate = null,
			string? endDate = null,
			string? expireFilter = null,
			string type = "csv")
		{
			var query = from h in _context.SupStockHistories
						join sb in _context.SupStockBatches on h.StockBatchId equals sb.StockBatchId
						join sku in _context.ProdProductSkus on sb.SkuId equals sku.SkuId
						join p in _context.ProdProducts on sku.ProductId equals p.ProductId
						join b in _context.SupBrands on p.BrandId equals b.BrandId
						select new
						{
							h.StockHistoryId,
							sku.SkuCode,
							sb.BatchNumber,
							p.ProductName,
							b.BrandName,
							sb.ExpireDate,
							h.ChangeQty,
							h.ChangeType,
							h.RevisedDate,
						};

			var today = DateTime.Today;

			// 過期與否
			if (!string.IsNullOrEmpty(expireFilter))
			{
				switch (expireFilter)
				{
					case "valid":
						query = query.Where(x => !x.ExpireDate.HasValue || x.ExpireDate.Value.Date >= today);
						break;
					case "expired":
						query = query.Where(x => x.ExpireDate.HasValue && x.ExpireDate.Value.Date < today);
						break;
				}
			}

			// 最後異動日期篩選
			if (DateTime.TryParse(startDate, out var sDate))
				query = query.Where(x => x.RevisedDate >= sDate);

			if (DateTime.TryParse(endDate, out var eDate))
			{
				var endOfDay = eDate.AddDays(1);
				query = query.Where(x => x.RevisedDate < endOfDay);
			}

			// 全局搜尋
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(s =>
					EF.Functions.Like(s.SkuCode, $"%{searchValue}%") ||
					EF.Functions.Like(s.BatchNumber, $"%{searchValue}%") ||
					EF.Functions.Like(s.ProductName, $"%{searchValue}%") ||
					EF.Functions.Like(s.BrandName, $"%{searchValue}%")
				);
			}

			var data = await query.ToListAsync();

			return type.ToLower() switch
			{
				"csv" => ExportCsv(data),
				"excel" => ExportExcel(data),
				"pdf" => await ExportPdf(
					searchValue ?? string.Empty,
					startDate ?? string.Empty,
					endDate ?? string.Empty,
					expireFilter ?? string.Empty
				), // 傳搜尋與篩選條件
				_ => BadRequest("未知匯出類型")
			};
		}
		#endregion

		#region 匯出API (CSV/Excel/PDF)
		private IActionResult ExportCsv(IEnumerable<dynamic> data)
		{
			var sb = new StringBuilder();
			using (var writer = new StringWriter(sb))
			using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
				csv.WriteRecords(data);

			var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
			return File(bytes, "text/csv", "StockBatch.csv");
		}

		private IActionResult ExportExcel(IEnumerable<dynamic> data)
		{
			using var workbook = new ClosedXML.Excel.XLWorkbook();
			var ws = workbook.Worksheets.Add("StockBatch");
			ws.Cell(1, 1).InsertTable(data);
			using var ms = new MemoryStream();
			workbook.SaveAs(ms);
			return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockBatch.xlsx");
		}

		private static PdfFont GetChineseFont()
		{
			string[] fontPaths = new[]
			{
			@"C:\Windows\Fonts\msjhlui.ttc,0",
			@"C:\Windows\Fonts\msjh.ttc,0",
			@"C:\Windows\Fonts\msjh.ttf"
		};

			foreach (var path in fontPaths)
			{
				try
				{
					return PdfFontFactory.CreateFont(path, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
				}
				catch
				{
					// 無法載入就繼續下一個
				}
			}

			// 都失敗時用預設字型
			return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
		}

		private async Task<IActionResult> ExportPdf(string searchValue, string startDate, string endDate, string expireFilter)
		{
			// 1️ 取得資料，套用 DataTables 篩選
			var query = from sb in _context.SupStockBatches
						join sku in _context.ProdProductSkus on sb.SkuId equals sku.SkuId
						join p in _context.ProdProducts on sku.ProductId equals p.ProductId
						join b in _context.SupBrands on p.BrandId equals b.BrandId
						select new
						{
							sb.StockBatchId,
							sku.SkuCode,
							sb.BatchNumber,
							p.ProductName,
							b.BrandName,
							sb.ExpireDate,
							sb.Qty,
							sb.CreatedDate
						};

			var today = DateTime.Today;

			if (!string.IsNullOrEmpty(expireFilter))
			{
				switch (expireFilter)
				{
					case "valid":
						query = query.Where(x => !x.ExpireDate.HasValue || x.ExpireDate.Value.Date >= today);
						break;
					case "expired":
						query = query.Where(x => x.ExpireDate.HasValue && x.ExpireDate.Value.Date < today);
						break;
				}
			}

			// 建立日期篩選
			if (DateTime.TryParse(startDate, out var sDate))
				query = query.Where(x => x.CreatedDate >= sDate);

			if (DateTime.TryParse(endDate, out var eDate))
			{
				var endOfDay = eDate.AddDays(1);
				query = query.Where(x => x.CreatedDate < endOfDay);
			}

			// 全局搜尋
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(x =>
					EF.Functions.Like(x.SkuCode, $"%{searchValue}%") ||
					EF.Functions.Like(x.BatchNumber, $"%{searchValue}%") ||
					EF.Functions.Like(x.ProductName, $"%{searchValue}%") ||
					EF.Functions.Like(x.BrandName, $"%{searchValue}%")
				);
			}

			var data = await query
				.Select(x => new
				{
					x.SkuCode,
					x.BatchNumber,
					x.BrandName,
					x.ProductName,
					x.ExpireDate,
					x.Qty
				})
				.ToListAsync();

			// 2️ 匯出 PDF
			if (!data.Any())
				return Content("No data to export");

			// 建立 PDF
			using var ms = new MemoryStream();
			var writer = new PdfWriter(ms);
			var pdf = new PdfDocument(writer);

			// 設為橫向
			var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
			var font = GetChineseFont();

			// 指定欄位順序與名稱
			var columns = new[]
			{
				new { Title = "SKU", Func = (Func<dynamic, string>)(x => x.SkuCode) },
				new { Title = "批號", Func = (Func<dynamic, string>)(x => x.BatchNumber) },
				new { Title = "品牌名", Func = (Func<dynamic, string>)(x => x.BrandName) },
				new { Title = "商品名稱", Func = (Func<dynamic, string>)(x => x.ProductName) },
				new { Title = "到期日", Func = (Func<dynamic, string>)(x => x.ExpireDate.HasValue ? x.ExpireDate.Value.ToString("yyyy-MM-dd") : "無") },
				new { Title = "批次庫存量", Func = (Func<dynamic, string>)(x => x.Qty.ToString()) }
			};

			var table = new iText.Layout.Element.Table(columns.Length).UseAllAvailableWidth();

			// 表頭
			foreach (var col in columns)
			{
				table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new Paragraph(col.Title).SetFont(font).SetFontSize(10)));
			}

			// 資料列
			foreach (var row in data)
			{
				if (row == null) continue;

				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.SkuCode ?? "").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.BatchNumber ?? "").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.BrandName ?? "").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.ProductName ?? "").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.ExpireDate?.ToString("yyyy-MM-dd") ?? "無").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.Qty.ToString()).SetFont(font).SetFontSize(10)));
			}


			doc.Add(table);
			doc.Close();

			return File(ms.ToArray(), "application/pdf", "StockBatch.pdf");
		}

		private async Task<IActionResult> ExportHistoryPdf(string searchValue, string startDate, string endDate, string expireFilter)
		{
			var query = from h in _context.SupStockHistories
						join sb in _context.SupStockBatches on h.StockBatchId equals sb.StockBatchId
						join sku in _context.ProdProductSkus on sb.SkuId equals sku.SkuId
						join p in _context.ProdProducts on sku.ProductId equals p.ProductId
						join b in _context.SupBrands on p.BrandId equals b.BrandId
						select new
						{
							sku.SkuCode,
							sb.BatchNumber,
							b.BrandName,
							p.ProductName,
							sb.ExpireDate,
							h.ChangeQty,
							h.ChangeType,
							h.RevisedDate,
							h.Remark
						};

			var today = DateTime.Today;

			// 過期與否
			if (!string.IsNullOrEmpty(expireFilter))
			{
				switch (expireFilter)
				{
					case "valid":
						query = query.Where(x => !x.ExpireDate.HasValue || x.ExpireDate.Value.Date >= today);
						break;
					case "expired":
						query = query.Where(x => x.ExpireDate.HasValue && x.ExpireDate.Value.Date < today);
						break;
				}
			}

			// 異動日期篩選
			if (DateTime.TryParse(startDate, out var sDate))
				query = query.Where(x => x.RevisedDate >= sDate);

			if (DateTime.TryParse(endDate, out var eDate))
			{
				var endOfDay = eDate.AddDays(1);
				query = query.Where(x => x.RevisedDate < endOfDay);
			}

			// 全局搜尋
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(x =>
					EF.Functions.Like(x.SkuCode, $"%{searchValue}%") ||
					EF.Functions.Like(x.BatchNumber, $"%{searchValue}%") ||
					EF.Functions.Like(x.ProductName, $"%{searchValue}%") ||
					EF.Functions.Like(x.BrandName, $"%{searchValue}%") ||
					EF.Functions.Like(x.Remark, $"%{searchValue}%")
				);
			}

			var data = await query.ToListAsync();

			if (!data.Any())
				return Content("No data to export");

			using var ms = new MemoryStream();
			var writer = new PdfWriter(ms);
			var pdf = new PdfDocument(writer);
			var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
			var font = GetChineseFont();

			var columns = new[]
			{
				new { Title = "SkuCode", Func = (Func<dynamic, string>)(x => x.SkuCode) },
				new { Title = "批號", Func = (Func<dynamic, string>)(x => x.BatchNumber) },
				new { Title = "品牌名", Func = (Func<dynamic, string>)(x => x.BrandName) },
				new { Title = "商品名稱", Func = (Func<dynamic, string>)(x => x.ProductName) },
				new { Title = "到期日", Func = (Func<dynamic, string>)(x => x.ExpireDate.HasValue ? x.ExpireDate.Value.ToString("yyyy-MM-dd") : "無") },
				new { Title = "異動數量", Func = (Func<dynamic, string>)(x => x.ChangeQty.ToString()) },
				new { Title = "異動類型", Func = (Func<dynamic, string>)(x => x.ChangeType) },
				new { Title = "異動時間", Func = (Func<dynamic, string>)(x => x.RevisedDate.ToString("yyyy-MM-dd HH:mm")) },
				new { Title = "備註", Func = (Func<dynamic, string>)(x => x.Remark ?? "") } // ← 新增
			};

			var table = new iText.Layout.Element.Table(columns.Length).UseAllAvailableWidth();

			// 表頭置中
			foreach (var col in columns)
			{
				table.AddHeaderCell(new iText.Layout.Element.Cell()
					.Add(new Paragraph(col.Title).SetFont(font).SetFontSize(10))
					.SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
			}

			// 資料列
			foreach (var row in data)
			{
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.SkuCode ?? "").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.BatchNumber ?? "").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.BrandName ?? "").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.ProductName ?? "").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.ExpireDate?.ToString("yyyy-MM-dd") ?? "無").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.ChangeQty.ToString()).SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.ChangeType ?? "").SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.RevisedDate.ToString("yyyy-MM-dd HH:mm")).SetFont(font).SetFontSize(10)));
				table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(row.Remark ?? "").SetFont(font).SetFontSize(10)));
			}

			doc.Add(table);
			doc.Close();

			return File(ms.ToArray(), "application/pdf", "StockHistory.pdf");
		}

		#endregion

		#region 初始化所有 SKU 批號
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> InitializeBatches()
		{
			try
			{
				await InitializeStockBatchesAsync(); // 呼叫內部方法
				return Json(new { success = true, message = "已初始化所有 SKU 批號" });
			}
			catch (Exception ex)
			{
				Console.WriteLine("❌ InitializeBatches error: " + ex);
				return Json(new { success = false, message = "初始化失敗：" + ex.Message });
			}
		}

		private async Task InitializeStockBatchesAsync(int? brandId = null, int? productId = null)
		{
			var skus = _context.ProdProductSkus
				.Include(s => s.Product)
					.ThenInclude(p => p.Brand)
				.Where(s => s.IsActive);

			if (brandId.HasValue)
				skus = skus.Where(s => s.Product.BrandId == brandId.Value);

			if (productId.HasValue)
				skus = skus.Where(s => s.ProductId == productId.Value);

			var skuList = await skus.ToListAsync();

			// 先查今天最大流水號
			var today = DateTime.Today;
			var todayBatchNumbers = await _context.SupStockBatches
				.Where(sb => sb.CreatedDate >= today && sb.CreatedDate < today.AddDays(1))
				.Select(sb => sb.BatchNumber)
				.ToListAsync();

			int maxSeq = 0;
			foreach (var bn in todayBatchNumbers)
			{
				var seqStr = bn.Split('-').Last();
				if (int.TryParse(seqStr, out int seqNum))
					maxSeq = Math.Max(maxSeq, seqNum);
			}

			int nextSeq = maxSeq + 1;

			foreach (var sku in skuList)
			{
				bool hasBatch = await _context.SupStockBatches
					.AnyAsync(b => b.SkuId == sku.SkuId && b.Qty > 0);

				if (!hasBatch && sku.StockQty > 0)
				{
					var batch = new SupStockBatch
					{
						SkuId = sku.SkuId,
						Qty = sku.StockQty,
						BatchNumber = GenerateBatchNumber(sku.Product.Brand.BrandCode, today, ref nextSeq),
						ManufactureDate = DateTime.Now,
						Creator = 1004,
						CreatedDate = DateTime.Now
					};

					_context.SupStockBatches.Add(batch);

					Console.WriteLine($"初始化批號: SkuId={sku.SkuId}, Qty={sku.StockQty}, BatchNumber={batch.BatchNumber}");
				}
			}

			await _context.SaveChangesAsync();
		}

		private static string GenerateBatchNumber(string brandCode, DateTime today, ref int nextSeq)
		{
			if (string.IsNullOrEmpty(brandCode))
				brandCode = "XX";

			var datePart = today.ToString("yyyyMMdd");

			string batchNumber = $"{brandCode}{datePart}-{nextSeq:000}";
			nextSeq++;

			return batchNumber;
		}
		#endregion

		/// <summary>
		/// 到期報廢 (Expire)，自動 FIFO 扣庫
		/// 不指定批號，AdjustStockAsync 會自動按 FIFO 扣庫
		/// 前端只需要傳 SkuId 與 ChangeQty，可選 Remark
		/// 回傳結果包含每個批次的扣庫紀錄 (batchMovements) 以及總扣庫數量 (expiredQty)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExpireStock([FromForm] StockBatchContactViewModel vm)
		{
			try
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

				if ((vm.ChangeQty ?? 0) <= 0)
					return Json(new { success = false, message = "異動數量必須大於 0" });

				int expireQty = vm.ChangeQty ?? 0;

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				int currentUserId = user?.UserNumberId ?? 1006;

				// 使用 AdjustStockAsync，服務自動處理 FIFO 扣庫
				var result = await _stockService.AdjustStockAsync(
					batchId: 0,          // 不指定批號
					skuId: sku.SkuId,
					changeQty: expireQty,
					isAdd: false,
					movementType: "Expire",
					reviserId: currentUserId,
					remark: vm.Remark ?? string.Empty
				);

				if (!result.Success)
					return Json(new { success = false, message = result.Message });

				return Json(new
				{
					success = true,
					batchMovements = result.BatchMovements,
					expiredQty = result.AdjustedQty,
					totalStockQty = result.TotalStock
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "伺服器錯誤：" + ex.Message });
			}
		}


		#region 處理出庫 / 退貨
		//前端只需傳：
		//ExpireStock: SkuId, ChangeQty, 可選 Remark
		//SaleStock: SkuId, ChangeQty, OrderItemId, 可選 Remark
		//ReturnStock: SkuId, ChangeQty, OrderItemId, 可選 Remark

		/// <summary>
		/// 銷售出庫 (Sale)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SaleStock([FromForm] StockBatchContactViewModel vm)
		{
			try
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

				if ((vm.ChangeQty ?? 0) <= 0)
					return Json(new { success = false, message = "異動數量必須大於 0" });

				int requestedQty = vm.ChangeQty ?? 0;

				if (sku.StockQty < requestedQty)
					return Json(new { success = false, message = "庫存不足，無法出庫" });

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				int currentUserId = user?.UserNumberId ?? 1006;

				// 使用 AdjustStockAsync，服務自動處理 FIFO 扣庫
				var result = await _stockService.AdjustStockAsync(
					batchId: 0,          // 不指定批號
					skuId: sku.SkuId,
					changeQty: requestedQty,
					isAdd: false,
					movementType: "Sale",
					reviserId: currentUserId,
					remark: vm.Remark ?? string.Empty,
					//orderItemId: vm.OrderItemId
					orderItemId: 2521  // TODO:目前寫死對應訂單明細

				);
				//var result = await _stockService.AdjustStockAsync(
				//	batchId: 9131,      // 指定已存在的批號
				//	skuId: 1084,
				//	changeQty: 1,
				//	isAdd: false,
				//	movementType: "Sale",
				//	reviserId: 1004,
				//	remark: null,
				//	orderItemId: 2521  // 對應訂單明細
				//);


				if (!result.Success)
					return Json(new { success = false, message = result.Message });

				return Json(new
				{
					success = true,
					batchMovements = result.BatchMovements,
					appliedChangeQty = result.AdjustedQty,
					totalStockQty = result.TotalStock
				});
			}
			catch (Exception ex)
			{
				var inner = ex.InnerException != null ? ex.InnerException.Message : "";
				var fullMessage = $"伺服器錯誤: {ex.Message}" + (string.IsNullOrEmpty(inner) ? "" : " | Inner: " + inner);
				return Json(new { success = false, message = fullMessage });
			}
		}

		/// <summary>
		/// 退貨入庫 (Return)
		/// 先入庫能存的量
		/// 超過 SKU 最大庫存，依 FIFO 報廢（Expire）
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ReturnStock([FromForm] StockBatchContactViewModel vm)
		{
			try
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

				if ((vm.ChangeQty ?? 0) <= 0)
					return Json(new { success = false, message = "異動數量必須大於 0" });

				int requestedQty = vm.ChangeQty ?? 0;

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				int currentUserId = user?.UserNumberId ?? 1006;

				// 呼叫 AdjustStockAsync，讓服務自動處理：
				// 1️ 回原銷售批次
				// 2️ 超過 SKU.MaxStockQty → FIFO 報廢
				var result = await _stockService.AdjustStockAsync(
					batchId: 0,          // 不指定批號，由服務端找原批次
					skuId: sku.SkuId,
					changeQty: requestedQty,
					isAdd: true,
					movementType: "Return",
					reviserId: currentUserId,
					remark: vm.Remark ?? string.Empty,
					//orderItemId: vm.OrderItemId
					orderItemId: 2521  // TODO:目前寫死對應訂單明細
				);

				if (!result.Success)
					return Json(new { success = false, message = result.Message });

				return Json(new
				{
					success = true,
					batchMovements = result.BatchMovements,
					appliedChangeQty = result.AdjustedQty,
					totalStockQty = result.TotalStock
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "伺服器錯誤：" + ex.Message });
			}
		}

		#endregion

	}
}