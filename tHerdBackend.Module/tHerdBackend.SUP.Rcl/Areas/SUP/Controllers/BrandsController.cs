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
using tHerdBackend.Infra.Models;

[Area("SUP")]
public class BrandsController : Controller
{
	private readonly tHerdDBContext _context;
	private readonly ICurrentUser _me;
	private readonly UserManager<ApplicationUser> _userMgr;
	private readonly ISupplierService _supplierService;

	public BrandsController(
		tHerdDBContext context,
		ICurrentUser me,
		UserManager<ApplicationUser> userMgr,
		ISupplierService supplierService)
	{
		_context = context;
		_me = me;
		_userMgr = userMgr;
		_supplierService = supplierService;
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

		// 排序（0=#, 1=品牌名稱, 2=供應商名稱 ...需對應前端DataTable的欄位索引）
		query = sortColumnIndex switch
		{
			0 => sortDirection == "asc"
				? query.OrderBy(x => x.RevisedDate ?? x.CreatedDate)
				: query.OrderByDescending(x => x.RevisedDate ?? x.CreatedDate),
			1 => sortDirection == "asc" ? query.OrderBy(x => x.BrandName) : query.OrderByDescending(x => x.BrandName),
			2 => sortDirection == "asc" ? query.OrderBy(x => x.SupplierName) : query.OrderByDescending(x => x.SupplierName),
			3 => sortDirection == "asc" ? query.OrderBy(x => x.IsFeatured) : query.OrderByDescending(x => x.IsFeatured),
			// 4 折扣狀態
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
			discountStatus = GetBrandDiscountStatus(x.StartDate, x.EndDate, x.DiscountRate, x.IsActive), // 關鍵！全部後端算好
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
			.Select(b => new {
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
		return $"進行中（{discountRate.Value:0.#}%，至 {endDate.Value:yyyy-MM-dd}）";
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

}
