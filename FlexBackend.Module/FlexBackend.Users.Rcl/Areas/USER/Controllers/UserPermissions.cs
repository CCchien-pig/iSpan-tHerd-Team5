using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Core.Web_Datatables;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.USER.Rcl.Areas.USER.Controllers
{
	[Area("USER")]
	[Controller]
	public class UserPermissions : Controller
	{
		private readonly tHerdDBContext _db;
		private readonly ApplicationDbContext _app;

		public UserPermissions(
			tHerdDBContext db,
			ApplicationDbContext app)
		{
			_db = db;
			_app = app;
		}

		public IActionResult Index() => View();

		// DataTables server-side: 角色清單
		[HttpGet]
		public async Task<IActionResult> Roles([FromQuery] DataTableRequest dt)
		{
			var start = Math.Max(0, dt?.Start ?? 0);
			var length = (dt?.Length ?? 10) > 0 ? dt!.Length : 10;
			var kw = dt?.Search?.Value?.Trim();

			// 1) 來源用 Identity DB
			var q = _app.Roles.AsNoTracking();

			var recordsTotal = await q.CountAsync();

			if (!string.IsNullOrWhiteSpace(kw))
				q = q.Where(r => r.Name!.Contains(kw) || (r.Description ?? "").Contains(kw));

			var recordsFiltered = await q.CountAsync();

			// 2) 排序（可改成依 DataTables order）
			q = q.OrderByDescending(r => r.CreatedDate);

			// 3) 先只投影可被翻譯的欄位，拉回記憶體
			var page = await q
				.Skip(start).Take(length)
				.Select(r => new { r.Id, r.Name, r.Description, r.CreatedDate })
				.ToListAsync();

			// 4) 一次把模組數量算好（避免 N+1）
			var roleIds = page.Select(p => p.Id).ToList();
			var moduleCounts = await _db.UserRoleModules
				.Where(x => roleIds.Contains(x.AdminRoleId))
				.GroupBy(x => x.AdminRoleId)
				.Select(g => new { RoleId = g.Key, Count = g.Count() })
				.ToDictionaryAsync(x => x.RoleId, x => x.Count);

			// 5) 在記憶體格式化時間
			var data = page.Select(r => new {
				r.Id,
				r.Name,
				r.Description,
				CreatedDate = r.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
				ModuleCount = moduleCounts.TryGetValue(r.Id, out var c) ? c : 0
			});

			return Json(new { draw = dt?.Draw ?? 0, recordsTotal, recordsFiltered, data });
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
