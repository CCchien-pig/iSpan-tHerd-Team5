using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SUP.Data.Helpers;
using System.Diagnostics;
using System.Text.Json;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.DTOs.SUP.BrandLayout;
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
		private readonly IContentService _contentService;

		public BrandsController(
			tHerdDBContext context,
			ICurrentUser me,
			UserManager<ApplicationUser> userMgr,
			ISupplierService supplierService,
			IBrandService brandService,
			IBrandLayoutService layoutService,
			IContentService contentService)
		{
			_context = context;
			_me = me;
			_userMgr = userMgr;
			_supplierService = supplierService;
			_brandService = brandService;
			_layoutService = layoutService;
			_contentService = contentService;
		}

		#region Index

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

		#endregion

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
		/// 讀取 JSON 骨架 -> 遍歷 -> 從各 Service 獲取內容 -> 組合成完整的 BaseLayoutBlockDto 列表
		/// 載入品牌版面編輯器。
		/// 支援：編輯特定版本、新增時複製啟用版本、新增時建立空白版本、新增時複製特定版本。
		/// </summary>
		/// <param name="id">品牌 ID (來自路由)</param>
		/// <param name="layoutId">要編輯的版面 ID (來自路由，可選)</param>
		/// <param name="fromScratch">是否建立空白版本 (來自查詢字串)</param>
		/// <param name="copyFromLayoutId">要複製的來源版面 ID (來自查詢字串)</param>		
		// GET: SUP/Brands/EditLayout/{brandId}/{layoutId?}
		[HttpGet("EditLayout/{id}/{layoutId?}")]
		public async Task<IActionResult> EditLayout(
			int id,
			int? layoutId,
			[FromQuery] bool fromScratch = false,
			[FromQuery] int? copyFromLayoutId = null) // 【新增】接收 copyFromLayoutId 參數
		{
			int brandId = id;
			var brand = await _brandService.GetByIdAsync(brandId);
			if (brand == null) return NotFound("找不到該品牌");

			var hydratedBlocks = new List<BaseLayoutBlockDto>();
			BrandLayoutDto? layoutAsTemplate = null;

			// =================================================================
			// 【核心修正點】重構整個條件判斷邏輯，確保流程互斥
			// =================================================================
			if (layoutId.HasValue)
			{
				// 模式 1：編輯現有版本
				layoutAsTemplate = await _layoutService.GetLayoutByLayoutIdAsync(layoutId.Value);
				if (layoutAsTemplate != null)
				{
					// 直接組合 (Hydrate)
					hydratedBlocks = await HydrateBlocksFromJson(layoutAsTemplate.LayoutJson);
				}
			}
			else if (copyFromLayoutId.HasValue)
			{
				// 模式 2：從指定版本複製來新增
				layoutAsTemplate = await _layoutService.GetLayoutByLayoutIdAsync(copyFromLayoutId.Value);
				if (layoutAsTemplate != null)
				{
					hydratedBlocks = await HydrateBlocksFromJson(layoutAsTemplate.LayoutJson);
				}
			}
			else if (fromScratch)
			{
				// 模式 3：建立空白版本
				// 保持 hydratedBlocks 為空列表，不做任何事。
			}
			else
			{
				// 模式 4：預設新增 (先試著複製啟用版，若無則讀取舊資料)
				layoutAsTemplate = await _layoutService.GetActiveLayoutAsync(brandId);
				if (layoutAsTemplate != null)
				{
					// 如果有啟用中的新版資料，組合它
					hydratedBlocks = await HydrateBlocksFromJson(layoutAsTemplate.LayoutJson);
				}
				else
				{
					// 如果連啟用中的新版都沒有，才去讀取舊資料表
					hydratedBlocks = await _layoutService.GetLegacyAccordionBlocksAsync(brandId);
				}
			}

			// 3. 手動映射到 ViewModel (因為不使用 AutoMapper)
			var layoutBlockViewModels = hydratedBlocks.Select(b => new BrandLayoutBlockViewModel
			{
				Id = b.Id,
				Type = b.Type,
				Props = b.Props // 直接傳遞 Props DTO
			}).ToList();

			var layoutModel = new BrandLayoutEditViewModel
			{
				BrandId = brandId,
				BrandName = brand.BrandName,
				LayoutBlocks = layoutBlockViewModels,
				LayoutId = layoutId, // 只有編輯模式才有值
				LayoutVersion = layoutId.HasValue ? layoutAsTemplate?.LayoutVersion : null, // 只有編輯模式才有值
				AllLayoutVersions = (await _layoutService.GetLayoutsByBrandIdAsync(brandId)).Select(l => l.LayoutVersion).ToList()
			};

			return PartialView("~/Areas/SUP/Views/Brands/Partials/_BrandLayoutEditorPartial.cshtml", layoutModel);
		}

		// 【新增輔助函式】將重複的組合 (Hydrate) 邏輯提取出來
		private async Task<List<BaseLayoutBlockDto>> HydrateBlocksFromJson(string layoutJson)
		{
			var hydratedBlocks = new List<BaseLayoutBlockDto>();
			if (string.IsNullOrWhiteSpace(layoutJson)) return hydratedBlocks;

			var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
			var skeletonBlocks = JsonSerializer.Deserialize<List<BrandLayoutSkeletonBlockDto>>(layoutJson, jsonOptions)
				?? new List<BrandLayoutSkeletonBlockDto>();

			foreach (var blockSkeleton in skeletonBlocks)
			{
				if (blockSkeleton == null || string.IsNullOrEmpty(blockSkeleton.Type) || blockSkeleton.ContentId <= 0) continue;

				var uniqueId = $"{blockSkeleton.Type}-{blockSkeleton.ContentId}-{Guid.NewGuid().ToString("N").Substring(0, 4)}";
				BaseLayoutBlockDto newBlock = null;

				switch (blockSkeleton.Type.ToLower())
				{
					case "banner":
						var bannerContent = await _contentService.GetContentByIdAsync<BannerDto>(blockSkeleton.ContentId);
						if (bannerContent != null)
						{
							// 【核心修正點】將骨架中的 linkUrl 寫回 Props DTO
							bannerContent.LinkUrl = blockSkeleton.LinkUrl;

							hydratedBlocks.Add(new BaseLayoutBlockDto
							{
								Id = uniqueId,
								Type = "Banner",
								Props = bannerContent
							});
						}
						break;
					case "accordion":
						var accordionContent = await _contentService.GetContentByIdAsync<BrandAccordionContentDto>(blockSkeleton.ContentId);
						if (accordionContent != null) newBlock = new BaseLayoutBlockDto { Id = uniqueId, Type = "Accordion", Props = accordionContent };
						break;
					case "article":
						var articleContent = await _contentService.GetContentByIdAsync<BrandArticleDto>(blockSkeleton.ContentId);
						if (articleContent != null) newBlock = new BaseLayoutBlockDto { Id = uniqueId, Type = "Article", Props = articleContent };
						break;
				}

				if (newBlock != null) hydratedBlocks.Add(newBlock);
			}

			return hydratedBlocks;
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

				// 2. 安全地獲取 reviserId
				int reviserId = 1004; // 預設使用匿名 ID 1004

				if (_me.IsAuthenticated)
				{
					var userId = _me.Id;
					var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

					if (user != null)
					{
						reviserId = user.UserNumberId;
					}
				}

				// 檢查版本號是否 IsNullOrEmpty
				if (string.IsNullOrEmpty(dto.LayoutVersion))
				{
					return BadRequest(new { success = false, message = "版本號是必填欄位。" });
				}

				// 【版本號重複驗證】
				bool versionExists = await _layoutService.VersionExistsAsync(dto.BrandId, dto.LayoutVersion, dto.ActiveLayoutId);
				if (versionExists)
				{
					return BadRequest(new { success = false, message = $"版本號 '{dto.LayoutVersion}' 已存在，請使用不同的版本號。" });
				}

				// 3. 核心業務邏輯 (Create/Update/Activate)
				// 【核心修正點】呼叫新的交易式儲存方法
				int finalLayoutId = await _layoutService.SaveHybridLayoutAsync(dto, reviserId);
				// 啟用邏輯現在可以安全地放在交易之外

				// 4. 成功回傳
				return Json(new
				{
					success = true,
					//message = "品牌版面配置已成功儲存並啟用為現行版本。",
					message = "版面配置已儲存成功。",
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

		#region 複製一份版面的api - 啟用/停用

		/// <summary>
		/// 啟用指定版型（同品牌僅允許一個 Layout 為啟用狀態）
		/// </summary>
		/// POST /SUP/Brands/ActivateLayout/1004
		[HttpPost("~/SUP/Brands/ActivateLayout/{layoutId}")]
		[ValidateAntiForgeryToken]
		[AllowAnonymous]
		public async Task<IActionResult> ActivateBrandLayout(int layoutId)
		{
			Console.WriteLine($"[PATCH] ActivateBrandLayout 被呼叫 layoutId={layoutId}");

			var userId = _me.Id;
			var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
			{
				Debug.WriteLine("User not found");
				return Json(new { success = false, message = "找不到使用者資料" });
			}

			int currentUserId = user.UserNumberId;

			try
			{
				var result = await _layoutService.ActivateLayoutAsync(layoutId, currentUserId);
				if (!result)
					// 找不到 Layout
					return NotFound(new { success = false, message = $"找不到指定的版面配置 (Layout ID: {layoutId})。" });

				return Ok(new { success = true, message = "品牌版面設定已成功啟用為現行版本。" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = $"執行啟用操作時發生伺服器錯誤: {ex.Message}" });
			}
		}

		/// <summary>
		/// 軟刪除（停用）品牌 Layout
		/// </summary>
		/// DELETE SUP/Brand/layouts/{layoutId}
		[HttpDelete("~/SUP/Brands/layouts/{layoutId}")]
		[ValidateAntiForgeryToken]
		[AllowAnonymous]
		public async Task<IActionResult> DeleteBrandLayout(int layoutId)
		{
			Console.WriteLine($"[DELETE] DeleteBrandLayout 被呼叫 layoutId={layoutId}");

			var userId = _me.Id;
			var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
			{
				Debug.WriteLine("User not found");
				return Json(new { success = false, message = "找不到使用者資料" });
			}

			int currentUserId = user.UserNumberId;

			try
			{
				var result = await _layoutService.SoftDeleteLayoutAsync(layoutId, currentUserId);
				if (!result)
					return NotFound(new { success = false, message = "找不到指定的品牌版面配置 (Layout ID: " + layoutId + ")" });

				return Ok(new { success = true, message = "品牌版面配置已成功停用。" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "執行停用操作時發生伺服器錯誤: " + ex.Message });
			}
		}

		#endregion

		#region 輔助 API：取得版本列表的 JSON
		
		/// <summary>
		/// 只回傳版本列表的 JSON
		/// </summary>sionsAsJson/{id}
		[HttpGet]
		public async Task<IActionResult> GetLayoutVersionsAsJson(int id)
		{
			var layouts = await _layoutService.GetLayoutsByBrandIdAsync(id);

			// 只選擇 ID 和版本號，傳給前端
			var versionList = layouts.Select(l => new {
				layoutId = l.LayoutId,
				layoutVersion = l.LayoutVersion ?? "未命名版本"
			}).ToList();

			return Ok(versionList);
		}

		#endregion
	}
}