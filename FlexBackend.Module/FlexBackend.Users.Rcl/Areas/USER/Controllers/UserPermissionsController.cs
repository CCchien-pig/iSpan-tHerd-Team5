using FlexBackend.Core.DTOs.USER;
using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Core.Web_Datatables;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.USER.Rcl.Areas.USER.Controllers
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
		public async Task<IActionResult> Roles([FromQuery] DataTableRequest dt)
		{
			var start = Math.Max(0, dt?.Start ?? 0);
			var length = (dt?.Length ?? 10) > 0 ? dt!.Length : 10;
			var kw = dt?.Search?.Value?.Trim();

			var q = _app.Roles
						.AsNoTracking()
						.Where(r => r.Name != "Member");   // ★ 排除 Member

			var recordsTotal = await q.CountAsync();

			if (!string.IsNullOrWhiteSpace(kw))
				q = q.Where(r => r.Name!.Contains(kw) || (r.Description ?? "").Contains(kw));

			var recordsFiltered = await q.CountAsync();

			q = q.OrderByDescending(r => r.CreatedDate);

			var page = await q.Skip(start).Take(length)
							  .Select(r => new { r.Id, r.Name, r.Description, r.CreatedDate })
							  .ToListAsync();

			var roleIds = page.Select(p => p.Id).ToList();
			var moduleCounts = await _db.UserRoleModules
				.Where(x => roleIds.Contains(x.AdminRoleId))
				.GroupBy(x => x.AdminRoleId)
				.Select(g => new { RoleId = g.Key, Count = g.Count() })
				.ToDictionaryAsync(x => x.RoleId, x => x.Count);

			var data = page.Select(r => new
			{
				r.Id,
				r.Name,
				r.Description,
				CreatedDate = r.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
				ModuleCount = moduleCounts.TryGetValue(r.Id, out var c) ? c : 0
			});

			return Json(new { draw = dt?.Draw ?? 0, recordsTotal, recordsFiltered, data });
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
