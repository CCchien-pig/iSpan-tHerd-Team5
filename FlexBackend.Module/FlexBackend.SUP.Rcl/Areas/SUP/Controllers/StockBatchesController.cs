using FlexBackend.Core.Abstractions;
using FlexBackend.Core.DTOs.SUP;
using FlexBackend.Core.DTOs.USER;
using FlexBackend.Core.Interfaces.SUP;
using FlexBackend.Infra.Models;
using FlexBackend.SUP.Rcl.Areas.SUP.ViewModels;
using Microsoft.AspNetCore.Identity;
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
			//ViewBag.SupplierId = supplierId;
			//ViewBag.BrandName = TempData["BrandName"] as string ?? "";

			return View();
		}

		// POST: StockBatches/IndexJson
		[HttpPost]
		public async Task<IActionResult> IndexJson(
			[FromForm] string supplierId = null, 
			[FromForm] string expireFilter = null,
			[FromForm] string stockDateSort = "updated")
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
								SupplierId = b.SupplierId,

								sb.StockBatchId,
								SkuCode = sku.SkuCode,
								sb.BatchNumber,
								sb.ExpireDate,
								sb.RevisedDate,
								sb.Qty,
								sku.SafetyStockQty,
								sku.ReorderPoint,
								sb.IsSellable,

								CreatedDate = sb.CreatedDate, // <-- 用來排序 BatchNumber 的日期部分

								// 展開要的
								ProductName = p.ProductName,
								BrandName = b.BrandName,
								BrandCode = b.BrandCode,

								// SKU 總庫存
								TotalStock = sku.StockQty,

								// SKU 顏色狀態
								StockStatus = sku.StockQty <= sku.SafetyStockQty ? "danger" :
									sku.StockQty <= sku.ReorderPoint ? "low" : "normal",

								// 批次庫存顏色狀態
								BatchStockStatus = sb.Qty <= sku.SafetyStockQty ? "danger" :
								   sb.Qty <= sku.ReorderPoint ? "low" : "normal",

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

				// 1️ 到期日篩選
				var today = DateTime.Today;
				if (!string.IsNullOrEmpty(expireFilter))
				{
					query = expireFilter switch
					{
						"valid" => query.Where(x => !x.ExpireDate.HasValue || x.ExpireDate.Value.Date >= today),
						"expired" => query.Where(x => x.ExpireDate.HasValue && x.ExpireDate.Value.Date < today),
						_ => query
					};
				}

				// 2️ 依 dated 排序
				query = stockDateSort switch
				{
					"updated-asc" => query.OrderBy(s => s.RevisedDate),
					"updated-desc" => query.OrderByDescending(s => s.RevisedDate),
					"created-asc" => query.OrderBy(s => s.CreatedDate),
					"created-desc" => query.OrderByDescending(s => s.CreatedDate),
					_ => query.OrderByDescending(s => s.RevisedDate)
				};


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
					RevisedDate = d.RevisedDate?.ToString("yyyy-MM-dd"),
					d.Qty,
					SafetyStockQty = d.SafetyStockQty,  // 保護 null
					ReorderPoint = d.ReorderPoint,      // 保護 null

					// 展開列需要的
					d.BrandName,
					d.ProductName,

					TotalStock = d.TotalStock,

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


		// POST: /SUP/StockBatches/CreateStockBatch
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
				List<SupStockMovementDto> batchMovements = new();
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
							vm.Remark
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
					totalStockQty = totalStockQty       // 實際總庫存
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


		[HttpPost]
		public async Task<IActionResult> GetAllStockHistory([FromForm] string supplierId = null, [FromForm] string expireFilter = null)
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
							SkuCode = sku.SkuCode,
							sb.BatchNumber,
							b.BrandName,
							ExpireDate = sb.ExpireDate,
							h.ChangeType,
							h.BeforeQty,
							h.ChangeQty,
							h.AfterQty,
							RevisedDate = h.RevisedDate,
							Reviser = h.Reviser,
							BrandCode = b.BrandCode,
							SupplierId = b.SupplierId
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



		// POST: /SUP/StockBatches/Update
		[HttpPost]
		public async Task<JsonResult> Update(int id, StockBatchUpdateDto dto)
		{
			var batch = await _context.SupStockBatches
				.Include(b => b.Sku) // 假設 SupStockBatch 有 Sku 導航屬性
				.FirstOrDefaultAsync(b => b.StockBatchId == id);

			if (batch == null)
				return Json(new { success = false, message = "找不到批號" });

			var sku = batch.Sku;
			if (sku == null)
				return Json(new { success = false, message = "找不到對應 SKU" });

			int beforeQty = batch.Qty;
			int changeQty = dto.ChangeQty;

			if (dto.IsAdd)
			{
				int maxAdd = sku.MaxStockQty - sku.StockQty;
				if (changeQty > maxAdd)
					return Json(new { success = false, message = $"增加數量不可超過最大庫存，上限 {maxAdd}" });

				batch.Qty += changeQty;
				sku.StockQty += changeQty;
			}
			else
			{
				if (changeQty > batch.Qty)
					return Json(new { success = false, message = $"扣減數量不可超過批號庫存，上限 {batch.Qty}" });

				batch.Qty -= changeQty;
				sku.StockQty -= changeQty;
				changeQty = -changeQty; // 記錄異動時為負數
			}

			var userId = _me.Id;
			var user = await _userMgr.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
				return Json(new { success = false, message = "找不到使用者資料" });

			int currentUserId = user.UserNumberId;

			// 建立異動紀錄
			_context.SupStockHistories.Add(new SupStockHistory
			{
				StockBatchId = id,
				ChangeType = "Adjust",
				ChangeQty = changeQty,
				BeforeQty = beforeQty,
				AfterQty = batch.Qty,
				Remark = dto.Remark,
				Reviser = currentUserId,
				RevisedDate = DateTime.Now
			});

			await _context.SaveChangesAsync();

			return Json(new { success = true, newQty = batch.Qty, totalStock = sku.StockQty });
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
					BrandName = brand.BrandName,
					ProductName = prod.ProductName,
					SkuCode = sku.SkuCode,
					ManufactureDate = sb.ManufactureDate,
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
					StockHistoryId = lastHistory?.StockHistoryId
				}
			});
		}

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

		#region 初始化所有 SKU 批號
		[HttpPost]
		public async Task<IActionResult> InitializeBatches()
		{
			try
			{
				await InitializeStockBatchesAsync();
				return Json(new { success = true, message = "已初始化所有 SKU 批號" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "初始化失敗：" + ex.Message });
			}
		}

		public async Task InitializeStockBatchesAsync()
		{
			var today = DateTime.Today;

			// 取得所有有庫存的 SKU
			var skus = await _context.ProdProductSkus
				.Include(s => s.Product)
					.ThenInclude(p => p.Brand)
				.Where(s => s.IsActive && s.StockQty > 0)
				.ToListAsync();

			foreach (var sku in skus)
			{
				// 計算今日已有最大流水號
				var todayBatchNumbers = _context.SupStockBatches
					.Where(sb => sb.CreatedDate >= today && sb.CreatedDate < today.AddDays(1))
					.Select(sb => sb.BatchNumber)
					.ToList();

				int maxSeq = 0;
				foreach (var bn in todayBatchNumbers)
				{
					var seqStr = bn.Split('-').Last();
					if (int.TryParse(seqStr, out int seqNum))
						maxSeq = Math.Max(maxSeq, seqNum);
				}

				int nextSeq = maxSeq + 1;

				// 生成批號
				string brandCode = sku.Product.Brand?.BrandCode ?? "XX";
				string datePart = today.ToString("yyyyMMdd");
				string batchNumber = $"{brandCode}{datePart}-{nextSeq:000}";

				// 建立批號
				var batch = new SupStockBatch
				{
					SkuId = sku.SkuId,
					Qty = sku.StockQty,
					BatchNumber = batchNumber,
					ManufactureDate = today,
					Creator = 1004, // 可以改成當前登入者
					CreatedDate = DateTime.Now
				};

				_context.SupStockBatches.Add(batch);

				Console.WriteLine($"初始化批號: SkuId={sku.SkuId}, Qty={sku.StockQty}, BatchNumber={batchNumber}");
			}

			await _context.SaveChangesAsync();
		}

		#endregion
	}
}
