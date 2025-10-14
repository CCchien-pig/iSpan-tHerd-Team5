using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Core.Web_Datatables;
using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace tHerdBackend.USER.Rcl.Areas.USER.Controllers
{
	[Area("USER")]
	[Controller]
	public class UserPermissionsController : Controller
	{
		private readonly tHerdDBContext _db;
		private readonly ApplicationDbContext _app;
		private readonly RoleManager<ApplicationRole> _roleMgr;   // ★ 加入

		public UserPermissionsController(
			tHerdDBContext db,
			ApplicationDbContext app,
			RoleManager<ApplicationRole> roleMgr)                 // ★ 注入
		{
			_db = db;
			_app = app;
			_roleMgr = roleMgr;
		}

		public IActionResult Index() => View();

		// DataTables 角色清單（排除 Member）
		[HttpGet]
		public async Task<IActionResult> Roles([FromQuery] DataTableRequest? dt)
		{
			var draw = dt?.Draw ?? 0;
			var start = Math.Max(0, dt?.Start ?? 0);
			var length = (dt?.Length ?? 10) > 0 ? dt!.Length : 10;

			// ---- 搜尋 fallback ----
			var kw = dt?.Search?.Value;
			if (string.IsNullOrWhiteSpace(kw))
				kw = Request.Query["search[value]"].ToString();
			kw = kw?.Trim();

			// 來源：Identity Roles（排除 Member）
			var q = _app.Roles.AsNoTracking().Where(r => r.Name != "Member");
			var recordsTotal = await q.CountAsync();

			if (!string.IsNullOrEmpty(kw))
				q = q.Where(r => r.Name!.Contains(kw) || (r.Description ?? "").Contains(kw));

			var recordsFiltered = await q.CountAsync();

			// ---- 排序 fallback（優先用 columns[x][data] 名稱）----
			// DataTables 會送：order[0][column]=<idx>、order[0][dir]=asc/desc、columns[idx][data]=<欄位名>
			string? orderColIdxStr = Request.Query["order[0][column]"];
			string? orderDir = Request.Query["order[0][dir]"];
			bool desc = string.Equals(orderDir, "desc", StringComparison.OrdinalIgnoreCase);

			IOrderedQueryable<ApplicationRole> ordered;
			if (int.TryParse(orderColIdxStr, out var orderIdx))
			{
				var colDataKey = Request.Query[$"columns[{orderIdx}][data]"].ToString(); // e.g. "name" / "description" / "createdDate"
				ordered = colDataKey switch
				{
					"name" => (desc ? q.OrderByDescending(r => r.Name) : q.OrderBy(r => r.Name)),
					"description" => (desc ? q.OrderByDescending(r => r.Description) : q.OrderBy(r => r.Description)),
					"createdDate" => (desc ? q.OrderByDescending(r => r.CreatedDate) : q.OrderBy(r => r.CreatedDate)),
					_ => q.OrderByDescending(r => r.CreatedDate)
				};
			}
			else
			{
				// 如果沒有帶排序參數，就用預設：建立時間 DESC
				ordered = q.OrderByDescending(r => r.CreatedDate);
			}

			// 先把頁面資料拉出來（只用 _app）
			var page = await ordered
				.Skip(start).Take(length)
				.Select(r => new { r.Id, r.Name, r.Description, r.CreatedDate })
				.ToListAsync();

			// 再用 _db 算該頁面角色的 moduleCount（獨立查詢，不混 DbContext）
			var roleIds = page.Select(p => p.Id).ToList();
			var moduleCounts = await _db.UserRoleModules
				.Where(x => roleIds.Contains(x.AdminRoleId))
				.GroupBy(x => x.AdminRoleId)
				.Select(g => new { RoleId = g.Key, Count = g.Count() })
				.ToDictionaryAsync(x => x.RoleId, x => x.Count);

			var data = page.Select(r => new
			{
				id = r.Id,
				name = r.Name,
				description = r.Description,
				createdDate = r.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
				moduleCount = moduleCounts.TryGetValue(r.Id, out var c) ? c : 0
			});

			return Json(new { draw, recordsTotal, recordsFiltered, data });
		}
		// ★ 新增角色
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateRole([FromForm] string name, [FromForm] string? description)
		{
			if (string.IsNullOrWhiteSpace(name))
				return BadRequest(new { ok = false, message = "角色名稱必填" });

			if (await _roleMgr.RoleExistsAsync(name.Trim()))
				return BadRequest(new { ok = false, message = "角色名稱已存在" });

			var role = new ApplicationRole
			{
				Name = name.Trim(),
				Description = description?.Trim(),
				CreatedDate = DateTime.UtcNow,
				Creator = 0
			};
			var res = await _roleMgr.CreateAsync(role);
			if (!res.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", res.Errors.Select(e => e.Description)) });

			return Json(new { ok = true });
		}

		// ★ 刪除角色（保護 Member；有使用者時不允許）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteRole([FromForm] string roleId)
		{
			var role = await _roleMgr.FindByIdAsync(roleId);
			if (role == null) return NotFound(new { ok = false, message = "角色不存在" });
			if (string.Equals(role.Name, "Member", StringComparison.OrdinalIgnoreCase))
				return BadRequest(new { ok = false, message = "不可刪除預設角色 Member" });

			var hasUsers = await _app.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
			if (hasUsers)
				return BadRequest(new { ok = false, message = "已有使用者屬於此角色，請先移除" });

			// 先清掉角色 × 模組對應
			var binds = _db.UserRoleModules.Where(x => x.AdminRoleId == roleId);
			_db.UserRoleModules.RemoveRange(binds);
			await _db.SaveChangesAsync();

			var res = await _roleMgr.DeleteAsync(role);
			if (!res.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", res.Errors.Select(e => e.Description)) });

			return Json(new { ok = true });
		}

		// 讀取：某角色擁有哪些模組（給設定用）
		[HttpGet]
		public async Task<IActionResult> RoleModules(string roleId)
		{
			var allModules = await _db.SysCodes.AsNoTracking()
				.Where(c => c.ModuleId == "SYS" && c.IsActive)
				.OrderBy(c => c.CodeNo)
				.Select(c => new { code = c.CodeNo, name = c.CodeDesc })
				.ToListAsync();

			var owned = await _db.UserRoleModules.AsNoTracking()
				.Where(x => x.AdminRoleId == roleId)
				.Select(x => x.ModuleId).ToListAsync();

			return Json(new { all = allModules, owned });
		}

		// 儲存：角色×模組
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SaveRoleModules([FromForm] string roleId,
												 [FromForm] List<string>? modules)
		{
			if (string.IsNullOrWhiteSpace(roleId))
				return BadRequest(new { ok = false, message = "roleId is required." });

			modules ??= new();

			try
			{
				using var tx = await _db.Database.BeginTransactionAsync();

				var old = _db.UserRoleModules.Where(x => x.AdminRoleId == roleId);
				_db.UserRoleModules.RemoveRange(old);

				if (modules.Count > 0)
				{
					var now = DateTime.UtcNow;
					_db.UserRoleModules.AddRange(
						modules.Distinct(StringComparer.OrdinalIgnoreCase)
							   .Select(m => new UserRoleModule
							   {
								   AdminRoleId = roleId,
								   ModuleId = m,
								   Creator = 0,
								   CreatedDate = now
							   }));
				}

				await _db.SaveChangesAsync();
				await tx.CommitAsync();
				return Json(new { ok = true });
			}
			catch (Exception ex)
			{
				return BadRequest(new { ok = false, message = ex.Message });
			}
		}

	}
}
