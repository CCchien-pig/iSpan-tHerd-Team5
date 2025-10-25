using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SUP.Data.Helpers;
using System.Diagnostics;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.DTOs.SUP.BrandLayoutBlocks;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.Services.SUP;
using tHerdBackend.Infra.Models;
using tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.Controllers
{
	[Area("SUP")]
	public class BrandsController : Controller
	{
		private readonly tHerdDBContext _context;
		private readonly ICurrentUser _me;
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly ISupplierService _supplierService;
		private readonly IBrandService _brandService;
		private readonly IBrandLayoutService _layoutService;

		public BrandsController(
			tHerdDBContext context,
			ICurrentUser me,
			UserManager<ApplicationUser> userMgr,
			ISupplierService supplierService,
			IBrandService brandService,
			IBrandLayoutService layoutService)
		{
			_context = context;
			_me = me;
			_userMgr = userMgr;
			_supplierService = supplierService;
			_brandService = brandService;
			_layoutService = layoutService;
		}

		// GET: SUP/Brands/Index
		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

		// POST: SUP/Brands/IndexJson
		[HttpPost]
		public async Task<IActionResult> IndexJson()
		{
			var draw = Request.Form["draw"].FirstOrDefault() ?? "1";
			var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
			var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
			var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";

			var sortColumnIndex = Convert.ToInt32(Request.Form["order[0][column]"].FirstOrDefault() ?? "0");
			var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault() ?? "asc";

			var query = from b in _context.SupBrands
						join s in _context.SupSuppliers on b.SupplierId equals s.SupplierId into bs
						from s in bs.DefaultIfEmpty() // 左外連接，避免無供應商導致 null
						select new
						{
							b.BrandId,
							b.BrandName,
							b.BrandCode,
							b.DiscountRate,
							b.StartDate,          // 加上折扣開始日期
							b.EndDate,            // 加上折扣結束日期
							b.IsDiscountActive,
							b.IsFeatured,
							b.IsActive,
							b.LikeCount,
							b.RevisedDate,
							b.CreatedDate,
							SupplierId = s != null ? s.SupplierId : 0,
							SupplierName = s != null ? s.SupplierName : ""
						};

			// 搜尋（品牌、供應商）
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(x =>
					EF.Functions.Like(x.BrandName, $"%{searchValue}%") ||
					EF.Functions.Like(x.SupplierName, $"%{searchValue}%")
				);
			}

			var totalRecords = await _context.SupBrands.CountAsync();
			var filteredRecords = await query.CountAsync();
			var today = DateOnly.FromDateTime(DateTime.Today);

			// 排序（0=#, 1=品牌名稱, 2=供應商名稱 ...需對應前端DataTable的欄位索引）
			// 3:「依折扣狀態」先分『0進行中→1尚未開始→2已結束→3其他非折扣→4尚未設定』再依折扣率排序
			query = sortColumnIndex switch
			{
				0 => sortDirection == "asc"
					? query.OrderBy(x => x.RevisedDate ?? x.CreatedDate)
					: query.OrderByDescending(x => x.RevisedDate ?? x.CreatedDate),
				1 => sortDirection == "asc" ? query.OrderBy(x => x.BrandName) : query.OrderByDescending(x => x.BrandName),
				2 => sortDirection == "asc" ? query.OrderBy(x => x.SupplierName) : query.OrderByDescending(x => x.SupplierName),
				3 => sortDirection == "asc"
					? query.OrderBy(x =>
						!x.DiscountRate.HasValue || x.DiscountRate.Value <= 0 ? 4           // 尚未設定折扣（沒折扣率/null/<=0）
						: x.StartDate > today ? 1                                           // 尚未開始
						: x.EndDate < today ? 2                                             // 已結束
						: !x.IsDiscountActive ? 3                                           // 其他非折扣（例如品牌停用）
						: 0                                                                 // 進行中
					).ThenBy(x => x.DiscountRate)
					: query.OrderByDescending(x =>
						!x.DiscountRate.HasValue || x.DiscountRate.Value <= 0 ? 4
						: x.StartDate > today ? 1
						: x.EndDate < today ? 2
						: !x.IsDiscountActive ? 3
						: 0
					).ThenByDescending(x => x.DiscountRate),
				4 => sortDirection == "asc" ? query.OrderBy(x => x.IsFeatured) : query.OrderByDescending(x => x.IsFeatured),
				5 => sortDirection == "asc" ? query.OrderBy(x => x.IsActive) : query.OrderByDescending(x => x.IsActive),
				_ => query.OrderByDescending(x => x.RevisedDate ?? x.CreatedDate),
			};

			// 分頁 + 折扣狀態計算
			var dataRaw = await query
				.Skip(start)
				.Take(length)
				.ToListAsync();

			// 計算折扣狀態
			var data = dataRaw.Select(x => new
			{
				brandId = x.BrandId,
				brandName = x.BrandName,
				supplierId = x.SupplierId,
				supplierName = x.SupplierName,
				discountStatus = GetBrandDiscountStatus(x.StartDate, x.EndDate, x.DiscountRate, x.IsActive),
				isFeatured = x.IsFeatured,
				isActive = x.IsActive,
				sortDate = x.RevisedDate ?? x.CreatedDate
			}).ToList();

			return Ok(new
			{
				draw,
				recordsTotal = totalRecords,
				recordsFiltered = filteredRecords,
				data
			});
		}

		#region 品牌

		// GET: SUP/Brands/Create
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var dto = new BrandDto();

			// 取得所有供應商
			var suppliers = await _supplierService.GetAllSuppliersAsync();

			// 將供應商做成下拉選單，未啟用的加 disabled 屬性
			ViewBag.Suppliers = suppliers.Select(s => new SelectListItem
			{
				Value = s.SupplierId.ToString(),
				Text = s.SupplierName,
				Disabled = !s.IsActive
			}).ToList();

			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandFormPartial.cshtml", dto);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		//public async Task<IActionResult> Create([Bind("BrandName,BrandCode,SupplierId,SeoId,IsActive,IsFeatured")] BrandDto dto)
		public async Task<IActionResult> Create([Bind("BrandName,BrandCode,SupplierId,IsActive,IsFeatured")] BrandDto dto)
		{
			try
			{
				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

				if (user == null)
				{
					Debug.WriteLine("User not found");
					return Json(new { success = false, message = "找不到使用者資料" });
				}

				int currentUserId = user.UserNumberId;
				Debug.WriteLine($"UserId: {currentUserId}, BrandName: {dto.BrandName}, SupplierId: {dto.SupplierId}");

				//int? seoIdInt = null;
				//if (!string.IsNullOrWhiteSpace(dto.SeoId))
				//{
				//	if (int.TryParse(dto.SeoId, out int parsedId))
				//	{
				//		seoIdInt = parsedId;
				//		Debug.WriteLine($"SeoId parsed as {seoIdInt}");
				//	}
				//	else
				//	{
				//		Debug.WriteLine("SeoId parse failed");
				//	}
				//}

				if (ModelState.IsValid)
				{
					var entity = new SupBrand
					{
						BrandName = dto.BrandName,
						BrandCode = dto.BrandCode,
						SupplierId = dto.SupplierId,
						//SeoId = seoIdInt,
						IsActive = dto.IsActive,
						IsFeatured = dto.IsFeatured,
						Creator = currentUserId,
						CreatedDate = DateTime.Now
					};

					_context.SupBrands.Add(entity);
					await _context.SaveChangesAsync();

					Debug.WriteLine($"Brand created: {entity.BrandId}");

					return Json(new
					{
						success = true,
						isCreate = true,
						brand = new
						{
							brandId = entity.BrandId,
							brandName = entity.BrandName,
							brandCode = entity.BrandCode,
							supplierId = entity.SupplierId,
							supplierName = entity.SupplierId.HasValue
								? _context.SupSuppliers.FirstOrDefault(s => s.SupplierId == entity.SupplierId)?.SupplierName ?? ""
								: "",
							//seoId = dto.SeoId,
							isActive = entity.IsActive,
							isFeatured = entity.IsFeatured
						}
					});
				}
				else
				{
					var errors = ModelState.Values.SelectMany(v => v.Errors)
										.Select(e => e.ErrorMessage).ToList();
					Debug.WriteLine("ModelState invalid: " + string.Join(", ", errors));
					return Json(new { success = false, errors });
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Exception: " + ex.ToString());
				return Json(new { success = false, message = "發生錯誤，請聯絡管理員。" });
			}
		}

		// GET: SUP/Brands/Edit/5
		[HttpGet]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var entity = await _context.SupBrands.FindAsync(id);
			if (entity == null) return NotFound();

			var supplier = await _context.SupSuppliers.FindAsync(entity.SupplierId);

			var dto = new BrandDto
			{
				BrandId = entity.BrandId,
				BrandName = entity.BrandName,
				BrandCode = entity.BrandCode,
				SupplierId = entity.SupplierId,
				SupplierName = supplier?.SupplierName ?? "",
				IsActive = entity.IsActive,
				IsFeatured = entity.IsFeatured
			};

			// 設定下拉選單，指定 selectedValue 為品牌的 SupplierId
			ViewBag.Suppliers = new SelectList(
				await _context.SupSuppliers
					.OrderBy(s => s.SupplierName)
					.ToListAsync(),
				"SupplierId",
				"SupplierName",
				dto.SupplierId // 這裡指定選中值
			);

			ViewBag.FormAction = "Edit";
			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandFormPartial.cshtml", dto);
		}

		// POST: SUP/Brands/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, BrandDto dto)
		{
			if (id != dto.BrandId) return NotFound();
			if (ModelState.IsValid)
			{
				try
				{
					var entity = await _context.SupBrands.FindAsync(id);
					if (entity == null)
						return Json(new { success = false, message = "找不到資料" });

					// 檢查是否未變更
					if (EntityComparer.IsUnchanged(
							entity, dto,
							nameof(SupBrand.BrandName),
							nameof(SupBrand.IsDiscountActive),
							nameof(SupBrand.IsFeatured),
							nameof(SupBrand.IsActive)))
					{
						return Json(new { success = false, message = "未變更" });
					}

					var userId = _me.Id;
					var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
					if (user == null)
						return Json(new { success = false, message = "找不到使用者資料" });
					int currentUserId = user.UserNumberId;

					entity.BrandName = dto.BrandName;
					entity.BrandCode = dto.BrandCode;
					entity.SupplierId = dto.SupplierId;
					entity.IsActive = dto.IsActive;
					entity.IsFeatured = dto.IsFeatured;
					entity.Reviser = currentUserId;
					entity.RevisedDate = DateTime.Now;
					_context.Update(entity);
					await _context.SaveChangesAsync();

					return Json(new
					{
						success = true,
						isCreate = false,
						brand = new
						{
							brandId = entity.BrandId,
							brandName = entity.BrandName,
							supplierName = _context.SupSuppliers.FirstOrDefault(s => s.SupplierId == entity.SupplierId)?.SupplierName ?? "",
							isActive = entity.IsActive
						}
					});
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
			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandFormPartial.cshtml", dto);
		}

		// POST: SUP/Brands/ToggleActive/5
		[HttpPost]
		public async Task<IActionResult> ToggleStatus(int brandId, string type, bool status)
		{
			try
			{
				var entity = await _context.SupBrands.FindAsync(brandId);
				if (entity == null)
					return Json(new { success = false, message = "找不到該品牌" });

				//var userId = _me.Id;
				//var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				//if (user == null)
				//	return Json(new { success = false, message = "找不到使用者資料" });
				//int currentUserId = user.UserNumberId;

				switch (type)
				{
					case "featured":
						entity.IsFeatured = status; break;
					case "active":
						entity.IsActive = status; break;
				}

				await _context.SaveChangesAsync();
				return Json(new { success = true });
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

		// GET: SUP/Brands/Details/5
		[HttpGet]
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var entity = await _context.SupBrands.FindAsync(id);
			if (entity == null) return NotFound();
			var supplier = await _context.SupSuppliers.FindAsync(entity.SupplierId);
			var dto = new BrandDto
			{
				BrandId = entity.BrandId,
				BrandName = entity.BrandName,
				BrandCode = entity.BrandCode,
				SupplierId = entity.SupplierId,
				SupplierName = supplier?.SupplierName ?? "",
				IsActive = entity.IsActive,
				IsFeatured = entity.IsFeatured,
				Creator = entity.Creator,
				CreatedDate = entity.CreatedDate,
				Reviser = entity.Reviser,
				RevisedDate = entity.RevisedDate,
			};
			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandInfoPartial.cshtml", dto);
		}

		// POST: SUP/Brands/DeleteAjax/5
		[HttpPost]
		public async Task<IActionResult> DeleteAjax(int id)
		{
			var entity = await _context.SupBrands.FindAsync(id);
			if (entity == null)
				return Json(new { success = false, message = "找不到該品牌" });

			try
			{
				_context.SupBrands.Remove(entity);
				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// 判斷是否存在品牌
		private bool SupBrandExists(int id)
		{
			return _context.SupBrands.Any(e => e.BrandId == id);
		}

		#endregion

		#region 品牌折扣

		// /SUP/Brands/GetBrandNameBySupplier?supplierId=1001
		[HttpGet]
		public async Task<IActionResult> GetBrandNameBySupplier(int supplierId)
		{
			// 全部回傳，即使未啟用
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

		// GET: SUP/Brand/CreateDiscount
		[HttpGet]
		public async Task<IActionResult> CreateDiscount()
		{
			var brands = await _context.SupBrands
				.Include(b => b.Supplier)
				.Select(b => new
				{
					b.BrandId,
					b.BrandName,
					SupplierActive = b.Supplier != null ? (bool?)b.Supplier.IsActive : null
				}).ToListAsync();

			var items = brands.Select(b =>
				new SelectListItem
				{
					Value = b.BrandId.ToString(),
					Text = b.SupplierActive == true
						? b.BrandName
						: $"{b.BrandName} (其供應商未啟用)",
					//Disabled = b.SupplierActive != true // 未啟用則禁用
					Disabled = false // 全部可選
				}
			).ToList();

			ViewBag.BrandSelect = items;
			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandDiscountPartial.cshtml", new BrandDto());
		}

		// POST: SUP/Brand/CreateDiscount
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateDiscount(BrandDiscountDto dto)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.SelectMany(kvp => kvp.Value.Errors.Select(e => $"{kvp.Key} : {e.ErrorMessage}")).ToList();
				Debug.WriteLine("ModelState錯誤 => " + string.Join(" | ", errors));

				// 若驗證失敗，直接回傳原 partial view 方便前端重載
				return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandDiscountPartial.cshtml", dto);
			}
			if (dto.StartDate > dto.EndDate)
			{
				ModelState.AddModelError("EndDate", "折扣結束日期不得早於開始日期");
			}

			try
			{
				// 如有需要異動人員資訊
				var userId = _me.Id;
				var user = await _userMgr.Users
					.AsNoTracking()
					.FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null)
					return Json(new { success = false, message = "找不到使用者資料" });

				int currentUserId = user.UserNumberId;

				// 找到品牌
				var brand = await _context.SupBrands.FindAsync(dto.BrandId);
				if (brand == null)
					return Json(new { success = false, message = "找不到品牌" });

				// 驗證日期
				var today = DateOnly.FromDateTime(DateTime.Today);
				if (dto.StartDate < today)
					ModelState.AddModelError("StartDate", "折扣開始日不可早於今天");
				if (!dto.EndDate.HasValue || !dto.StartDate.HasValue || dto.EndDate < dto.StartDate)
					ModelState.AddModelError("EndDate", "折扣結束日不可早於開始日期");

				// 驗證折扣率
				if (!dto.DiscountRate.HasValue || dto.DiscountRate < 0.01m || dto.DiscountRate > 0.99m)
					ModelState.AddModelError("DiscountRate", "折扣率必須介於0.01-0.99之間");

				// 若有驗證錯誤，回傳 partial view 方便前端重載
				if (!ModelState.IsValid)
				{
					var errorList = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
					Debug.WriteLine("Discount Create *** ModelState Errors: " + string.Join(" | ", errorList));

					return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandDiscountPartial.cshtml", dto);
				}

				var isActive =
					brand.IsActive &&                                         // 必須品牌啟用
					brand.DiscountRate.HasValue && brand.DiscountRate > 0 &&
					brand.StartDate.HasValue && brand.EndDate.HasValue &&
					today >= brand.StartDate.Value && today <= brand.EndDate.Value;

				// 更新品牌折扣資訊
				brand.DiscountRate = dto.DiscountRate;
				brand.StartDate = dto.StartDate;
				brand.EndDate = dto.EndDate;
				brand.Reviser = currentUserId;
				brand.RevisedDate = DateTime.Now;
				brand.IsDiscountActive = isActive;

				await _context.SaveChangesAsync();

				// 回傳成功狀態
				return Json(new { success = true });
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

		// GET: /SUP/Brands/EditDiscount?brandId={id}
		[HttpGet]
		public async Task<IActionResult> EditDiscount(int brandId)
		{
			// 查詢當前品牌
			var entity = await _context.SupBrands
									  .AsNoTracking()
									  .FirstOrDefaultAsync(b => b.BrandId == brandId);
			if (entity == null)
				return NotFound("找不到品牌");

			// 組 DTO
			var dto = new BrandDiscountDto
			{
				BrandId = entity.BrandId,
				BrandName = entity.BrandName,
				DiscountRate = entity.DiscountRate,
				StartDate = entity.StartDate,
				EndDate = entity.EndDate,
				IsDiscountActive = entity.IsDiscountActive,
				// ...其他必要欄位
			};

			//ViewBag.FormAction = "Edit";
			// 回傳 PartialView
			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandDiscountEditPartial.cshtml", dto);

		}

		// POST: SUP/Brands/EditDiscount
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditDiscount(BrandDiscountDto dto)
		{
			if (!ModelState.IsValid)
			{
				// 驗證失敗 → 回傳 partial 方便前端重載
				return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandDiscountEditPartial.cshtml", dto);
			}

			if (dto.StartDate > dto.EndDate)
			{
				ModelState.AddModelError("EndDate", "折扣結束日期不得早於開始日期");
			}

			try
			{
				var entity = await _context.SupBrands.FindAsync(dto.BrandId);
				if (entity == null)
					return Json(new { success = false, message = "找不到品牌資料" });

				// 檢查是否未變更
				if (EntityComparer.IsUnchanged(
						entity, dto,
						nameof(SupBrand.DiscountRate),
						nameof(SupBrand.StartDate),
						nameof(SupBrand.EndDate),
						nameof(SupBrand.IsDiscountActive)))
				{
					return Json(new { success = false, message = "折扣資料未變更" });
				}

				// 取得目前登入使用者 ID
				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null)
					return Json(new { success = false, message = "找不到使用者資料" });
				int currentUserId = user.UserNumberId;

				// ===== 驗證邏輯與活動啟用判斷 =====
				var today = DateOnly.FromDateTime(DateTime.Today);

				if (dto.StartDate < today)
					ModelState.AddModelError("StartDate", "折扣開始日不可早於今天");
				if (!dto.EndDate.HasValue || !dto.StartDate.HasValue || dto.EndDate < dto.StartDate)
					ModelState.AddModelError("EndDate", "折扣結束日不可早於開始日期");
				if (!dto.DiscountRate.HasValue || dto.DiscountRate < 0.01m || dto.DiscountRate > 0.99m)
					ModelState.AddModelError("DiscountRate", "折扣率必須介於0.01-0.99之間");

				if (!ModelState.IsValid)
				{
					// 若有驗證錯誤，回傳表單 partial
					return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandDiscountEditPartial.cshtml", dto);
				}

				bool isActive =
					entity.IsActive &&                                  // 品牌必須啟用
					dto.DiscountRate.HasValue && dto.DiscountRate > 0 &&
					dto.StartDate.HasValue && dto.EndDate.HasValue &&
					today >= dto.StartDate.Value && today <= dto.EndDate.Value;

				// ===== 更新折扣欄位 =====
				entity.DiscountRate = dto.DiscountRate;
				entity.StartDate = dto.StartDate;
				entity.EndDate = dto.EndDate;
				entity.IsDiscountActive = isActive;
				entity.Reviser = currentUserId;
				entity.RevisedDate = DateTime.Now;

				_context.Update(entity);
				await _context.SaveChangesAsync();

				// 成功 JSON 回傳
				return Json(new
				{
					success = true,
					message = "折扣更新成功",
					brand = new
					{
						brandId = entity.BrandId,
						brandName = entity.BrandName,
						discountRate = entity.DiscountRate,
						startDate = entity.StartDate,
						endDate = entity.EndDate,
						isDiscountActive = entity.IsDiscountActive
					}
				});
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

		#endregion

		#region 折扣 API

		// 「計算折扣狀態」API
		// GET: SUP/Brand/GetBrandDiscountStatus
		[HttpGet]
		public static string GetBrandDiscountStatus(DateOnly? startDate, DateOnly? endDate, decimal? discountRate, bool isActive)
		{
			var today = DateOnly.FromDateTime(DateTime.Today);

			if (!startDate.HasValue || !endDate.HasValue || !discountRate.HasValue || discountRate.Value <= 0)
				return "尚未設定折扣";

			if (!isActive) return "未進行（品牌停用）";
			if (today < startDate.Value) return "尚未開始";
			if (today > endDate.Value) return "折扣已結束";
			return $"進行中（{discountRate.Value * 10:0.#}折），{startDate.Value:yyyy/MM/dd}~{endDate.Value:yyyy/MM/dd}";
		}

		// 批次「刷新折扣狀態」API
		// POST: SUP/Brand/RefreshAllBrandDiscountStatus
		[HttpPost]
		public async Task<IActionResult> RefreshAllBrandDiscountStatus()
		{
			var today = DateOnly.FromDateTime(DateTime.Today);
			var brands = await _context.SupBrands.ToListAsync();

			foreach (var b in brands)
			{
				if (!b.StartDate.HasValue || !b.EndDate.HasValue || !b.DiscountRate.HasValue || b.DiscountRate.Value <= 0)
				{
					b.IsDiscountActive = false; // 沒有折扣資訊
					continue;
				}

				// 只有日期在區間，且品牌啟用才算有效折扣
				if (b.IsActive && today >= b.StartDate.Value && today <= b.EndDate.Value)
				{
					b.IsDiscountActive = true;
				}
				else
				{
					b.IsDiscountActive = false;
				}
			}
			await _context.SaveChangesAsync();
			return Ok(new { success = true });
		}

		// GetBrandDiscountDetail API
		// GET: SUP/Brand/GetBrandDiscountDetail
		[HttpGet]
		public async Task<IActionResult> GetBrandDiscountDetail(int brandId)
		{
			var brand = await _context.SupBrands.FirstOrDefaultAsync(b => b.BrandId == brandId);
			if (brand == null)
				return NotFound();

			// 自行呼叫折扣狀態計算api，取得完整字串
			var fullStatus = GetBrandDiscountStatus(
				brand.StartDate,
				brand.EndDate,
				brand.DiscountRate,
				brand.IsActive
			);

			// 簡化顯示的狀態（取前段「進行中」等）
			var shortStatus = fullStatus.Split('（')[0];

			var dto = new
			{
				brandName = brand.BrandName,
				discountRate = brand.DiscountRate,
				isDiscountActive = brand.IsDiscountActive,
				startDate = brand.StartDate,
				endDate = brand.EndDate,
				discountStatus = shortStatus   // 新增簡化狀態
			};
			return Json(dto);
		}

		#endregion

		#region GET 請求：EditLayout Action (渲染編輯器)
		//從 Service 層獲取當前或歷史的版面 JSON 資料，將其反序列化為 BrandLayoutEditViewModel，然後回傳 Partial View

		/// <summary>
		/// 取得所有歷史版本的列表
		/// </summary>
		// GET: SUP/Brands/LayoutVersions/{brandId}
		[HttpGet]
		public async Task<IActionResult> GetLayoutVersions(int id) // 使用 id 接收 brandId
		{
			int brandId = id;
			var brand = await _brandService.GetByIdAsync(brandId);
			if (brand == null) return NotFound("找不到該品牌");

			// 1. 取得 Service 層的 DTO 集合 (List<BrandLayoutDto>)
			var allLayoutDtos = await _layoutService.GetLayoutsByBrandIdAsync(brandId);

			// 【修正點】使用 LINQ 投影將 BrandLayoutDto 轉換為 BrandLayoutVersionDto
			var versionViewModels = allLayoutDtos
				.Select(dto => new BrandLayoutVersionDto
				{
					// 假設 BrandLayoutVersionDto 和 BrandLayoutDto 結構兼容
					LayoutId = dto.LayoutId,
					LayoutVersion = dto.LayoutVersion,
					IsActive = dto.IsActive,
					CreatedDate = dto.CreatedDate,
					RevisedDate = dto.RevisedDate,
					// ... 確保所有需要的版本資訊欄位都已映射
				})
				.ToList();

			var model = new BrandLayoutVersionsViewModel
			{
				BrandId = brandId,
				BrandName = brand.BrandName,
				// 賦值給 List<BrandLayoutVersionDto>
				Layouts = versionViewModels
			};

			// 回傳版本列表 Partial View
			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandLayoutVersionIndexPartial.cshtml", model);
		}

		/// <summary>
		/// 渲染品牌版面編輯器 Partial View
		/// 將版面資料傳遞給 View，並讓 Vue 應用接管
		/// </summary>
		// BrandsController.cs

		// GET: SUP/Brands/EditLayout/{brandId}/{layoutId?}
		[HttpGet]
		// 【修正】接收兩個參數：brandId 仍使用 id 慣例，layoutId 則為可選的 int?
		public async Task<IActionResult> EditLayout(int id, int? layoutId)
		{
			// 慣例式路由修正
			int brandId = id;

			// 1. 取得品牌名稱 (用於標題)
			var brand = await _brandService.GetByIdAsync(brandId);
			if (brand == null) return NotFound("找不到該品牌");

			// 2. 獲取原始 Layout 記錄 (BrandLayoutDto)
			BrandLayoutDto? layoutToEdit = null;

			if (layoutId.HasValue)
			{
				// 2-1. 載入指定 ID 的 Layout (用於編輯歷史版本)
				layoutToEdit = await _layoutService.GetLayoutByLayoutIdAsync(layoutId.Value);

				// 如果找不到指定的版本
				if (layoutToEdit == null)
				{
					return NotFound($"找不到 Layout ID 為 {layoutId.Value} 的版面設定版本。");
				}
			}
			else
			{
				// 2-2. 載入啟用中的版本，作為「新增」或「複製」的基礎
				layoutToEdit = await _layoutService.GetActiveLayoutAsync(brandId);
			}

			// 3. 解析數據：將 LayoutJson 字串反序列化成 C# 區塊物件列表
			var layoutBlocks = new List<BaseLayoutBlockDto>();
			int? activeLayoutId = null;

			if (layoutToEdit != null)
			{
				// 呼叫 Service 解析 JSON 字串 (Service 需處理 JSON 格式錯誤)
				layoutBlocks = _layoutService.DeserializeLayout(layoutToEdit.LayoutJson);
				// 由於我們載入的版本可能不是啟用版，需要知道當前啟用版 ID
				activeLayoutId = await _layoutService.GetActiveLayoutIdAsync(brandId);
			}
			// 如果 layoutToEdit 為 null (表示該品牌從未設定任何版面)，layoutBlocks 保持為空列表。


			// 4. 映射到 ViewModel：將 DTO 轉換為 View 層的 ViewModel
			var layoutBlockViewModels = layoutBlocks
				.Select(b => new BrandLayoutBlockViewModel // 明確指定轉換為 LayoutBlockViewModel
				{
					Id = b.Id,
					Type = b.Type,
					Props = b.Props
				})
				.ToList();


			// 5. 準備最終 ViewModel
			var layoutModel = new BrandLayoutEditViewModel
			{
				BrandId = brandId,
				BrandName = brand.BrandName,

				// 正在編輯的版本 ID：如果是載入特定版本就是該 ID，如果是載入啟用版就是啟用版 ID
				LayoutId = layoutToEdit?.LayoutId,

				// 現行啟用中的版本 ID (用於前端判斷)
				ActiveLayoutId = activeLayoutId,

				LayoutVersion = layoutToEdit?.LayoutVersion,

				// 核心區塊數據
				LayoutBlocks = layoutBlockViewModels,

				// 賦值為空，用於接收前端提交
				LayoutJson = string.Empty
			};

			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandLayoutEditorPartial.cshtml", layoutModel);
		}


		/// <summary>
		/// (輔助 Action) 供前端 AJAX 呼叫以動態新增區塊
		/// </summary>	
		// GET /SUP/Brands/GetLayoutBlockPartial?blockType=banner&brandId=5
		// 注意：如果您的 Vue 採用的是純前端處理，這個 Action 可以簡化甚至移除。
		[HttpGet]
		public IActionResult GetLayoutBlockPartial(string blockType) // 移除 brandId, index 參數 (除非 Partial View 需要)
		{
			// 【修正】使用 BaseLayoutBlockDto 作為基礎 DTO
			var model = new BrandLayoutBlockViewModel
			{
				Id = Guid.NewGuid().ToString(),
				Type = blockType,
				Props = GetDefaultPropsForBlock(blockType)
			};

			// 這裡可以選擇不渲染 Partial View，直接返回 JSON 給 Vue (推薦)
			// 讓 Vue 負責渲染 UI，C# 只負責提供資料結構。
			return Json(model);

			/* 如果堅持使用 PartialView，則：
			ViewData["Index"] = index;
			return PartialView($"~/Areas/SUP/Views/Brands/Partials/_LayoutBlockEditor_{blockType}Partial.cshtml", model);
			*/
		}

		// 輔助方法：根據 Type 取得空的 Props 
		private static object GetDefaultPropsForBlock(string type) // 【修正】回傳類型改為 object
		{
			// 這裡我們直接返回一個匿名物件，其結構模仿前端需要的 props
			return type switch
			{
				"banner" => new { Title = "新增 Banner", Subtitle = "請填寫副標題", FileUrl = "", LinkUrl = "" },
				"accordion" => new { Title = "新增摺疊區塊", Content = "<p>請編輯內容</p>", ImgId = (int?)null },
				_ => new { Title = "新區塊", Content = "" }
			};
		}

		#endregion

		#region POST 請求：SaveLayout Action (處理提交)
		//負責接收前端提交的 LayoutJson 字串，並呼叫 Service 進行儲存（Create 或 Update）。


		/// <summary>
		/// 處理版面編輯器表單提交，儲存 LayoutJson
		/// POST /SUP/Brands/SaveLayout
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SaveLayout([FromBody] BrandLayoutSaveInputDto dto)
		{
			try
			{
				// 1. 模型驗證 
				if (!ModelState.IsValid)
				{
					var errors = ModelState.Values.SelectMany(v => v.Errors)
									   .Select(e => e.ErrorMessage).ToList();
					return BadRequest(new { success = false, message = "輸入資料驗證失敗", errors });
				}

				// 2. 【修正點】安全地獲取 reviserId
				int reviserId = 1004; // 預設使用匿名 ID 1004

				if (_me.IsAuthenticated)
				{
					// 使用您提供的邏輯來安全獲取 UserNumberId
					var userId = _me.Id;
					var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

					if (user != null)
					{
						// 找到使用者後，使用其 UserNumberId
						reviserId = user.UserNumberId;
					}
					// 如果 user == null，則保留預設的 reviserId = 1004
				}
				// 如果 _me.IsAuthenticated 為 false，則跳過上述邏輯，reviserId 保持為 1004。
				// 這避免了在未登入時觸發 _me.UserNumberId 的 InvalidOperationException 或 NotImplementedException。

				// 【新增：版本號重複驗證】
				bool versionExists = await _layoutService.VersionExistsAsync(dto.BrandId, dto.LayoutVersion, dto.ActiveLayoutId);
				if (versionExists)
				{
					return BadRequest(new { success = false, message = $"版本號 '{dto.LayoutVersion}' 已存在，請使用不同的版本號。" });
				}

				// 3. 核心業務邏輯 (Create/Update/Activate)
				int finalLayoutId;
				if (dto.ActiveLayoutId.HasValue && dto.ActiveLayoutId.Value > 0)
				{
					// A. 更新現有 Layout (PUT 邏輯)
					var updateDto = new BrandLayoutUpdateDto
					{
						LayoutJson = dto.LayoutJson,
						LayoutVersion = dto.LayoutVersion,
						Reviser = reviserId // 使用修正後的 reviserId
					};

					var updated = await _layoutService.UpdateLayoutAsync(dto.ActiveLayoutId.Value, updateDto);

					if (!updated)
						return NotFound(new { success = false, message = "更新失敗：找不到指定的 Layout 紀錄。" });

					finalLayoutId = dto.ActiveLayoutId.Value;
				}
				else
				{
					// B. 建立新 Layout (POST 邏輯)
					var createDto = new BrandLayoutCreateDto
					{
						LayoutJson = dto.LayoutJson,
						LayoutVersion = dto.LayoutVersion,
						Creator = reviserId, // 使用修正後的 reviserId
						BrandId = dto.BrandId
					};

					finalLayoutId = await _layoutService.CreateLayoutAsync(dto.BrandId, createDto);
				}

				// 4. 啟用新版本
				try
				{
					await _layoutService.ActivateLayoutAsync(finalLayoutId, reviserId);
				}
				catch (Exception ex)
				{
					return Json(new
					{
						success = true,
						message = $"版面內容已儲存，但自動啟用失敗: {ex.Message}。",
						layoutId = finalLayoutId
					});
				}

				// 5. 成功回傳
				return Json(new
				{
					success = true,
					message = "品牌版面配置已成功儲存並啟用為現行版本。",
					layoutId = finalLayoutId
				});
			}
			catch (Exception ex)
			{
				// 確保任何未預期的 C# 異常都以 JSON 格式返回 500 錯誤
				// 這樣前端就不會遇到 SyntaxError
				Console.Error.WriteLine($"SaveLayout CRITICAL ERROR: {ex}");
				return StatusCode(500, new { success = false, message = "伺服器發生未預期錯誤，請檢查伺服器 Console 日誌。", detail = ex.Message });
			}
		}

		#endregion


	}
}