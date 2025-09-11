using FlexBackend.Infra.Models;
using FlexBackend.SUP.Rcl.Areas.SUP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SUP.Data.Helpers;

namespace FlexBackend.SUP.Rcl.Areas.SUP.Controllers
{
	[Area("SUP")]
	public class SuppliersController : Controller
	{
		private readonly tHerdDBContext _context;

		public SuppliersController(tHerdDBContext context)
		{
			_context = context;
		}

		// GET: Suppliers/Index
		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

		// POST: Suppliers/IndexJson
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

			// 建立查詢
			var query = _context.SupSuppliers.AsQueryable();

			// 搜尋功能
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(s =>
					EF.Functions.Like(s.SupplierName, $"%{searchValue}%") ||
					EF.Functions.Like(s.ContactName, $"%{searchValue}%") ||
					EF.Functions.Like(s.Phone, $"%{searchValue}%") ||
					EF.Functions.Like(s.Email, $"%{searchValue}%")
				);
			}

			// 計算總筆數與過濾後筆數
			var totalRecords = await _context.SupSuppliers.CountAsync();
			var filteredRecords = await query.CountAsync();

			// 排序
			query = sortColumnIndex switch
			{
				0 => sortDirection == "asc" ? query.OrderBy(s => s.SupplierName) : query.OrderByDescending(s => s.SupplierName),
				1 => sortDirection == "asc" ? query.OrderBy(s => s.ContactName) : query.OrderByDescending(s => s.ContactName),
				2 => sortDirection == "asc" ? query.OrderBy(s => s.Phone) : query.OrderByDescending(s => s.Phone),
				3 => sortDirection == "asc" ? query.OrderBy(s => s.Email) : query.OrderByDescending(s => s.Email),
				4 => sortDirection == "asc" ? query.OrderBy(s => s.IsActive) : query.OrderByDescending(s => s.IsActive),
				_ => query.OrderBy(s => s.SupplierId),
			};

			// 分頁與選取欄位
			var data = await query
				.Skip(start)
				.Take(length)
				.Select(s => new
				{
					supplierId = s.SupplierId,
					supplierName = s.SupplierName,
					contactName = s.ContactName,
					phone = s.Phone,
					email = s.Email,
					isActive = s.IsActive
				}).ToListAsync();

			// 回傳給 DataTables
			return Ok(new
			{
				draw,
				recordsTotal = totalRecords,
				recordsFiltered = filteredRecords,
				data
			});
		}

		// GET: Suppliers/Create
		[HttpGet]
		public IActionResult Create()
		{
			// 空物件給 Partial View 使用
			var viewModel = new ContactViewModel();

			ViewBag.FormAction = "Create"; // 告訴 Partial View 這是 Create 動作
			//return PartialView("Partials/_SupplierFormPartial", viewModel); // 回傳 Partial View
			return PartialView("~/Areas/SUP/Views/Suppliers/Partials/_SupplierFormPartial.cshtml", viewModel);
		}

		// POST: Suppliers/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("SupplierName,ContactName,Phone,Email,IsActive")] ContactViewModel supplierVm)
		{
			if (ModelState.IsValid)
			{
				var supEntity = new SupSupplier
				{
					SupplierName = supplierVm.SupplierName,
					ContactName = supplierVm.ContactName,
					Phone = supplierVm.Phone,
					Email = supplierVm.Email,
					IsActive = supplierVm.IsActive,

					Creator = supplierVm.Creator,
					CreatedDate = DateTime.Now,
				};

				_context.SupSuppliers.Add(supEntity);
				await _context.SaveChangesAsync();

				return Json(new { success = true, isCreate = true });
			}

			// 驗證失敗回 Partial
			//return PartialView("Partials/_SupplierFormPartial", supplierVm);
			return PartialView("~/Areas/SUP/Views/Suppliers/Partials/_SupplierFormPartial.cshtml", supplierVm);

		}

		// GET: Suppliers/Edit/5
		[HttpGet]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) { return NotFound(); }

			var supEntity = await _context.SupSuppliers.FindAsync(id);
			if (supEntity == null) { return NotFound(); }

			//  帶入 SupSupplier 的值，Partial View 顯示原本的資料
			var viewModel = new ContactViewModel
			{
				SupplierId = supEntity.SupplierId,
				SupplierName = supEntity.SupplierName,
				ContactName = supEntity.ContactName,
				Phone = supEntity.Phone,
				Email = supEntity.Email,
				IsActive = supEntity.IsActive
			};

			ViewBag.FormAction = "Edit"; // 告訴 Partial View 這是 Edit 動作
			//return PartialView("Partials/_SupplierFormPartial", viewModel); // 回傳 Partial View
			return PartialView("~/Areas/SUP/Views/Suppliers/Partials/_SupplierFormPartial.cshtml", viewModel);

		}

		// POST: Suppliers/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, ContactViewModel model)
		{
			if (id != model.SupplierId) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					var supEntity = await _context.SupSuppliers.FindAsync(id);
					if (supEntity == null)
						return Json(new { success = false, message = "找不到資料" });

					// 檢查是否未變更
					if (EntityComparer.IsUnchanged(
							supEntity, model,
							nameof(SupSupplier.SupplierName),
							nameof(SupSupplier.ContactName),
							nameof(SupSupplier.Phone),
							nameof(SupSupplier.Email),
							nameof(SupSupplier.IsActive)))
					{
						return Json(new { success = false, message = "未變更" });
					}

					// 有變更 → 更新欄位
					supEntity.SupplierName = model.SupplierName;
					supEntity.ContactName = model.ContactName;
					supEntity.Phone = model.Phone;
					supEntity.Email = model.Email;
					supEntity.IsActive = model.IsActive;

					supEntity.Reviser = 111111; // TODO: 取登入ID
					supEntity.RevisedDate = DateTime.Now;

					_context.Update(supEntity);
					await _context.SaveChangesAsync();

					return Json(new { success = true });
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

			// 驗證失敗 → 回傳 PartialView
			//return PartialView("Partials/_SupplierFormPartial", model);
			return PartialView("~/Areas/SUP/Views/Suppliers/Partials/_SupplierFormPartial.cshtml", model);

		}

		// 切換啟用狀態 Action 
		// POST: Suppliers/ToggleActive/5
		[HttpPost]
		public async Task<IActionResult> ToggleActive(int id, bool isActive)
		{
			try
			{
				var supEntity = await _context.SupSuppliers.FindAsync(id);
				if (supEntity == null)
					return Json(new { success = false, message = "找不到該供應商" });

				supEntity.IsActive = isActive;
				supEntity.Reviser = 111111; // TODO: 取登入ID
				supEntity.RevisedDate = DateTime.Now;

				await _context.SaveChangesAsync();
				return Json(new { success = true, newStatus = isActive });
			}
			catch (DbUpdateException dbEx)
			{
				// DB 更新錯誤
				return Json(new { success = false, message = "資料庫更新失敗: " + dbEx.Message });
			}
			catch (Exception ex)
			{
				// 其他錯誤
				return Json(new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}


		//GET: Suppliers/Details/5
		[HttpGet]
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) { return NotFound(); }

			var supEntity = await _context.SupSuppliers
				.FirstOrDefaultAsync(m => m.SupplierId == id);
			if (supEntity == null) { return NotFound(); }

			//  帶入 SupSupplier 的值，Partial View 顯示原本的資料
			var model = new ContactViewModel
			{
				SupplierId = supEntity.SupplierId,
				SupplierName = supEntity.SupplierName,
				ContactName = supEntity.ContactName,
				Phone = supEntity.Phone,
				Email = supEntity.Email,
				IsActive = supEntity.IsActive,

				Creator = supEntity.Creator,
				CreatedDate = supEntity.CreatedDate,
				Reviser = supEntity.Reviser,
				RevisedDate = supEntity.RevisedDate
			};

			//return PartialView("Partials/_SupplierInfoPartial", model); // 回傳 Partial View
			return PartialView("~/Areas/SUP/Views/Suppliers/Partials/_SupplierInfoPartial.cshtml", model);
		}

		// POST: Suppliers/DeleteAjax/5
		[HttpPost]
		public async Task<IActionResult> DeleteAjax(int id)
		{
			var supEntity = await _context.SupSuppliers.FindAsync(id);
			if (supEntity != null)
			{
				_context.SupSuppliers.Remove(supEntity);
				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			return Json(new { success = false, message = "找不到該供應商" });
		}

		private bool SupSupplierExists(int id)
		{
			return _context.SupSuppliers.Any(e => e.SupplierId == id);
		}
	}
}
