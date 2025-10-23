using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SUP.Data.Helpers;
using System.Diagnostics;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.Services.SUP;
using tHerdBackend.Infra.Models;
using tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels;

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
							? (_context.SupSuppliers.FirstOrDefault(s => s.SupplierId == entity.SupplierId)?.SupplierName ?? "")
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
				Text = (b.SupplierActive == true)
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
	public string GetBrandDiscountStatus(DateOnly? startDate, DateOnly? endDate, decimal? discountRate, bool isActive)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);

		if (!startDate.HasValue || !endDate.HasValue || !discountRate.HasValue || discountRate.Value <= 0)
			return "尚未設定折扣";

		if (!isActive) return "未進行（品牌停用）";
		if (today < startDate.Value) return "尚未開始";
		if (today > endDate.Value) return "折扣已結束";
		return $"進行中（{(discountRate.Value * 10):0.#}折），{startDate.Value:yyyy/MM/dd}~{endDate.Value:yyyy/MM/dd}";
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
	/// 渲染品牌版面編輯器 Partial View
	/// GET /SUP/Brands/EditLayout/{id}
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> EditLayout(int id)
	{
		// 1. 檢查品牌存在性 (業務驗證)
		var brand = await _brandService.GetByIdAsync(id);
		if (brand == null)
		{
			return NotFound($"品牌 ID {id} 不存在。");
		}

		// 2. 取得啟用中的 Layout DTO
		var activeLayoutDto = await _layoutService.GetActiveLayoutAsync(id);

		// 3. 反序列化 LayoutJson 為 ViewModel 列表
		var layoutBlocks = new List<LayoutBlockViewModel>();
		string layoutJson = activeLayoutDto?.LayoutJson;

		if (!string.IsNullOrEmpty(layoutJson))
		{
			try
			{
				// 使用 System.Text.Json 進行反序列化
				// 由於 LayoutJson 包含嵌套屬性 (Props)，我們必須確保能正確處理
				var blocks = System.Text.Json.JsonSerializer.Deserialize<List<LayoutBlockViewModel>>(
					layoutJson,
					new System.Text.Json.JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true,
						// 確保能處理內容中的 HTML 字元，如 <p>
						Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
					}
				);
				layoutBlocks = blocks ?? new List<LayoutBlockViewModel>();

			}
			catch (Exception ex)
			{
				// 如果 JSON 格式錯誤，回傳錯誤訊息或空列表
				ModelState.AddModelError("", $"版面資料解析失敗: {ex.Message}");
				// 實際項目中應記錄此錯誤
			}
		}

		// 4. 組裝主 ViewModel
		var viewModel = new BrandLayoutEditViewModel
		{
			BrandId = id,
			BrandName = brand.BrandName,
			ActiveLayoutId = activeLayoutDto?.LayoutId,
			LayoutVersion = activeLayoutDto?.LayoutVersion ?? "v1.0",
			LayoutBlocks = layoutBlocks
		};

		// 5. 回傳 Partial View
		return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandLayoutEditorPartial.cshtml", viewModel);
	}

	/// <summary>
	/// (輔助 Action) 供前端 AJAX 呼叫以動態新增區塊
	/// GET /SUP/Brands/GetLayoutBlockPartial?blockType=banner&index=0
	/// </summary>
	public IActionResult GetLayoutBlockPartial(int brandId, string blockType, int index)
	{
		// 根據 Type 建立新的 ViewModel 實例
		var model = new LayoutBlockViewModel
		{
			Id = Guid.NewGuid().ToString(),
			Type = blockType,
			Props = GetDefaultPropsForBlock(blockType) // 從一個輔助方法取得預設 Props
		};

		ViewData["Index"] = index;
		// 渲染對應的 Partial View
		return PartialView($"~/Areas/SUP/Views/Brands/Partials/_LayoutBlockEditor_{blockType}Partial.cshtml", model);
	}

	// 輔助方法：根據 Type 取得空的 Props (定義在 Controller 內部或一個 Helper 類別)
	private LayoutBlockPropsViewModel GetDefaultPropsForBlock(string type)
	{
		// TODO: 實作邏輯來回傳對應的 LayoutBlockPropsViewModel 實例
		if (type == "banner")
			return new LayoutBlockPropsViewModel { Title = "新增 Banner", Subtitle = "請填寫副標題" };
		if (type == "accordion")
			return new LayoutBlockPropsViewModel { Title = "新增摺疊區塊", Content = "<p>請編輯內容</p>" };
		return new LayoutBlockPropsViewModel();
	}

	#endregion

	#region POST 請求：SaveLayout Action (處理提交)
	//負責接收前端提交的 LayoutJson 字串，並呼叫 Service 進行儲存（Create 或 Update）。

	/// <summary>
	/// 處理版面編輯器表單提交，儲存 LayoutJson
	/// POST /SUP/Brands/SaveLayout
	/// </summary>
	[HttpPost]
	[ValidateAntiForgeryToken] // 建議加上 CSRF 保護
	public async Task<IActionResult> SaveLayout(BrandLayoutEditViewModel model)
	{
		// 1. 模型驗證 (含 LayoutJson 的 Required 驗證)
		if (!ModelState.IsValid)
		{
			// 如果驗證失敗，重新渲染 Partial View 並帶回錯誤訊息
			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandLayoutEditorPartial.cshtml", model);
		}

		// 2. 準備 DTO
		var reviserId = _me.IsAuthenticated ? _me.UserNumberId : 1004; // 假設使用當前用戶或預設 ID

		// 注意：這裡的 model.LayoutJson 是前端傳來的標準 JSON 字串

		if (model.ActiveLayoutId.HasValue && model.ActiveLayoutId.Value > 0)
		{
			// A. 更新現有 Layout (PUT)
			var updateDto = new BrandLayoutUpdateDto
			{
				LayoutJson = model.LayoutJson,
				LayoutVersion = model.LayoutVersion,
				Reviser = reviserId
			};

			var updated = await _layoutService.UpdateLayoutAsync(model.ActiveLayoutId.Value, updateDto);

			if (!updated)
			{
				// 如果更新失敗 (例如找不到 LayoutId)
				ModelState.AddModelError("", "更新失敗：找不到指定的 Layout 紀錄。");
				return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandLayoutEditorPartial.cshtml", model);

			}
		}
		else
		{
			// B. 建立新 Layout (POST)
			var createDto = new BrandLayoutCreateDto
			{
				LayoutJson = model.LayoutJson,
				LayoutVersion = model.LayoutVersion,
				Creator = reviserId // 首次建立，使用 Creator
			};

			// 雖然是新增，但邏輯上我們是建立一個新的版本
			int newLayoutId = await _layoutService.CreateLayoutAsync(model.BrandId, createDto);

			// 為了讓使用者在儲存後能立即啟用新版面
			model.ActiveLayoutId = newLayoutId;
		}

		// 3. (可選) 自動啟用新儲存的版本 - **業務邏輯**
		// 這是常見的流程：編輯完畢後，預設啟用新版本
		try
		{
			await _layoutService.ActivateLayoutAsync(model.ActiveLayoutId!.Value, reviserId);
		}
		catch (Exception ex)
		{
			// 記錄啟用失敗，但仍視為儲存成功
			ModelState.AddModelError("", $"版面內容已儲存，但自動啟用失敗: {ex.Message}");
		}

		// 4. 成功回傳 (返回 JSON 讓 AJAX 處理)
		return Json(new
		{
			success = true,
			message = "版面配置已儲存並啟用。",
			layoutId = model.ActiveLayoutId // 傳回 ID 供前端參考
		});
	}
	
	#endregion

}