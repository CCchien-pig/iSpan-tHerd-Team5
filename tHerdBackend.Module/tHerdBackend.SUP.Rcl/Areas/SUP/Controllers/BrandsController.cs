using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;

[Area("SUP")]
public class BrandsController : Controller
{
	private readonly tHerdDBContext _context;
	private readonly ICurrentUser _me;
	private readonly UserManager<ApplicationUser> _userMgr;

	public BrandsController(
		tHerdDBContext context,
		ICurrentUser me,
		UserManager<ApplicationUser> userMgr)
	{
		_context = context;
		_me = me;
		_userMgr = userMgr;
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

		var query = _context.SupBrands
			.Join(_context.SupSuppliers, b => b.SupplierId, s => s.SupplierId, (b, s) => new { b, s })
			.Select(x => new
			{
				x.b.BrandId,
				x.b.BrandName,
				x.b.BrandCode,
				x.b.DiscountRate,
				x.b.IsDiscountActive,
				x.b.IsFeatured,
				x.b.IsActive,
				x.b.LikeCount,
				x.b.RevisedDate,
				x.b.CreatedDate,
				SupplierName = x.s != null ? x.s.SupplierName : "",
				// Banner、折扣狀態等如需複雜顯示可在這裡加欄位運算
			})
			.AsQueryable();

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
			3 => sortDirection == "asc" ? query.OrderBy(x => x.IsDiscountActive) : query.OrderByDescending(x => x.IsDiscountActive),
			4 => sortDirection == "asc" ? query.OrderBy(x => x.IsActive) : query.OrderByDescending(x => x.IsActive),
			_ => query.OrderByDescending(x => x.RevisedDate ?? x.CreatedDate),
		};

		// 分頁/欄位
		var data = await query
			.Skip(start)
			.Take(length)
			.Select(x => new
			{
				brandId = x.BrandId,
				brandName = x.BrandName,
				supplierName = x.SupplierName,
				discountStatus = x.IsDiscountActive
					? (x.DiscountRate.HasValue ? $"折扣{x.DiscountRate}%中" : "折扣中")
					: "無折扣",
				bannerStatus = "未設定", // 你可自行補Banner判斷欄位
				isActive = x.IsActive,
				sortDate = x.RevisedDate ?? x.CreatedDate
			}).ToListAsync();

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
	public IActionResult Create()
	{
		var viewModel = new BrandDto();
		ViewBag.FormAction = "Create";
		return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandsFormPartial.cshtml", viewModel);
	}

	// POST: SUP/Brands/Create
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create([Bind("BrandName,BrandCode,SupplierId,IsActive,IsFeatured")] BrandDto dto)
	{
		var userId = _me.Id;
		var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
			return Json(new { success = false, message = "找不到使用者資料" });
		int currentUserId = user.UserNumberId;

		if (ModelState.IsValid)
		{
			var entity = new SupBrand
			{
				BrandName = dto.BrandName,
				BrandCode = dto.BrandCode,
				SupplierId = dto.SupplierId,
				IsActive = dto.IsActive,
				IsFeatured = dto.IsFeatured,
				Creator = currentUserId,
				CreatedDate = DateTime.Now
			};
			_context.SupBrands.Add(entity);
			await _context.SaveChangesAsync();
			return Json(new
			{
				success = true,
				isCreate = true,
				brand = new
				{
					brandId = entity.BrandId,
					brandName = entity.BrandName,
					supplierName = _context.SupSuppliers.FirstOrDefault(s => s.SupplierId == entity.SupplierId)?.SupplierName ?? "",
					isActive = entity.IsActive
				}
			});
		}
		return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandsFormPartial.cshtml", dto);
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
		ViewBag.FormAction = "Edit";
		return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandsFormPartial.cshtml", dto);
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

				// TODO: 檢查 entity 屬性是否有異動，可用 EntityComparer 判斷（如供應商版本）

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
		return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandsFormPartial.cshtml", dto);
	}

	// POST: SUP/Brands/ToggleActive/5
	[HttpPost]
	public async Task<IActionResult> ToggleActive(int id, bool isActive)
	{
		try
		{
			var entity = await _context.SupBrands.FindAsync(id);
			if (entity == null)
				return Json(new { success = false, message = "找不到該品牌" });

			var userId = _me.Id;
			var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
			if (user == null)
				return Json(new { success = false, message = "找不到使用者資料" });
			int currentUserId = user.UserNumberId;

			entity.IsActive = isActive;
			entity.Reviser = currentUserId;
			entity.RevisedDate = DateTime.Now;
			await _context.SaveChangesAsync();
			return Json(new { success = true, newStatus = isActive });
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
}
