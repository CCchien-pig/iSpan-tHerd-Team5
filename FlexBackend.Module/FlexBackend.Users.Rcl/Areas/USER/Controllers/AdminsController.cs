using FlexBackend.Core.DTOs.USER;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FlexBackend.USER.Rcl.Areas.USER.Controllers
{
	public class AdminsController : Controller
	{
		private readonly ApplicationDbContext _app;
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly IWebHostEnvironment _env;

		public AdminsController(
			ApplicationDbContext app,
			UserManager<ApplicationUser> userMgr,
			IWebHostEnvironment env)
		{
			_app = app;
			_userMgr = userMgr;
			_env = env;
		}

		public IActionResult Index() => View();

		// DataTables 來源：只列出具有「非 Member」角色的使用者
		[HttpGet]
		public async Task<IActionResult> Users([FromQuery] int draw, [FromQuery] int start = 0, [FromQuery] int length = 10)
		{
			// search[value]
			var kw = (Request.Query["search[value]"].ToString() ?? string.Empty).Trim();

			// 先找出有非 Member 角色的使用者 Id
			var nonMemberUserIds = await (
				from ur in _app.UserRoles
				join r in _app.Roles on ur.RoleId equals r.Id
				where r.Name != "Member"
				select ur.UserId
			).Distinct().ToListAsync();

			// 基礎查詢：只列出非 Member 使用者
			var q = _app.Users.AsNoTracking()
							  .Where(u => nonMemberUserIds.Contains(u.Id));

			var recordsTotal = await q.CountAsync();

			if (!string.IsNullOrEmpty(kw))
			{
				q = q.Where(u =>
					(u.FirstName + " " + u.LastName).Contains(kw) ||
					u.Email!.Contains(kw) ||
					u.UserName!.Contains(kw) ||
					u.PhoneNumber!.Contains(kw)
				);
			}

			var recordsFiltered = await q.CountAsync();

			// 排序（依 DataTables 第一個排序欄位）
			// columns: 0=displayName, 1=email, 2=phoneNumber, 3=isActive, 4=createdDate, 5=操作
			var orderCol = int.TryParse(Request.Query["order[0][column]"], out var col) ? col : 4;
			var orderDir = Request.Query["order[0][dir]"].ToString();
			var desc = string.Equals(orderDir, "desc", StringComparison.OrdinalIgnoreCase);

			q = orderCol switch
			{
				0 => (desc ? q.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName)
						   : q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)),
				1 => (desc ? q.OrderByDescending(u => u.Email) : q.OrderBy(u => u.Email)),
				2 => (desc ? q.OrderByDescending(u => u.PhoneNumber) : q.OrderBy(u => u.PhoneNumber)),
				3 => (desc ? q.OrderByDescending(u => u.IsActive) : q.OrderBy(u => u.IsActive)),
				4 => (desc ? q.OrderByDescending(u => u.CreatedDate) : q.OrderBy(u => u.CreatedDate)),
				_ => q.OrderByDescending(u => u.CreatedDate)
			};

			// 分頁 + 先抓基本欄位
			var page = await q.Skip(start).Take(length)
				.Select(u => new
				{
					u.Id,
					u.FirstName,
					u.LastName,
					u.Email,
					u.PhoneNumber,
					u.IsActive,
					u.CreatedDate
				})
				.ToListAsync();

			var userIds = page.Select(p => p.Id).ToList();

			// 把每位使用者的角色（排除 Member）查出來
			var roleMap = await (
				from ur in _app.UserRoles
				join r in _app.Roles on ur.RoleId equals r.Id
				where userIds.Contains(ur.UserId) && r.Name != "Member"
				group r.Name by ur.UserId into g
				select new { UserId = g.Key, Roles = g.ToList() }
			).ToDictionaryAsync(x => x.UserId, x => x.Roles);

			var data = page.Select(p => new
			{
				id = p.Id,
				displayName = $"{p.LastName}{p.FirstName}",
				email = p.Email,
				phoneNumber = p.PhoneNumber,
				isActive = p.IsActive,
				createdDate = p.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
				roles = roleMap.TryGetValue(p.Id, out var roles) ? string.Join(", ", roles) : ""
			});

			return Json(new { draw, recordsTotal, recordsFiltered, data });
		}

		// 詳細頁
		[HttpGet]
		public async Task<IActionResult> Details(string id)
		{
			var u = await _app.Users.AsNoTracking()
				.Where(x => x.Id == id)
				.Select(x => new AdminUserVm
				{
					Id = x.Id,
					FirstName = x.FirstName,
					LastName = x.LastName,
					Email = x.Email!,
					PhoneNumber = x.PhoneNumber,
					IsActive = x.IsActive,
					CreatedDate = x.CreatedDate
				}).FirstOrDefaultAsync();

			if (u == null) return NotFound();

			// 角色（排除 Member）
			u.Roles = await (
				from ur in _app.UserRoles
				join r in _app.Roles on ur.RoleId equals r.Id
				where ur.UserId == id && r.Name != "Member"
				select r.Name
			).ToListAsync();

			return View(u);
		}

		// 上傳員工照片
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadPhoto([FromForm] string id, [FromForm] IFormFile photo)
		{
			var user = await _app.Users.FirstOrDefaultAsync(u => u.Id == id);
			if (user == null) return NotFound();

			if (photo == null || photo.Length == 0)
				return BadRequest("未選擇檔案");

			// 基本驗證：類型與大小
			var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
			if (!allowed.Contains(photo.ContentType))
				return BadRequest("僅接受 JPG / PNG / WEBP");

			if (photo.Length > 2 * 1024 * 1024) // 2MB
				return BadRequest("檔案過大（上限 2MB）");

			var root = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
			var dir = Path.Combine(root, "uploads", "users", id);
			Directory.CreateDirectory(dir);

			// 先刪除舊檔
			foreach (var f in Directory.EnumerateFiles(dir))
				System.IO.File.Delete(f);

			// 以固定檔名 profile.副檔名 儲存
			var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
			var filePath = Path.Combine(dir, "profile" + ext);
			using (var fs = System.IO.File.Create(filePath))
			{
				await photo.CopyToAsync(fs);
			}

			// 你有 ImgId 欄位，但未提供影像表結構；這裡不動 DB 結構，只存檔到 wwwroot
			// 可視需要在此更新 RevisedDate 等欄位
			user.RevisedDate = DateTime.UtcNow;
			await _app.SaveChangesAsync();

			return RedirectToAction(nameof(Details), new { id });
		}

		// 供 <img src> 使用：若無檔案，動態輸出 SVG（帶姓名縮寫）
		[HttpGet]
		public async Task<IActionResult> Photo(string id)
		{
			// 找實體檔案
			var root = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
			var dir = Path.Combine(root, "uploads", "users", id);
			if (Directory.Exists(dir))
			{
				var file = Directory.EnumerateFiles(dir, "profile.*").FirstOrDefault();
				if (file != null)
				{
					var ct = GetContentType(file);
					return PhysicalFile(file, ct ?? "application/octet-stream");
				}
			}

			// 沒檔案 → 產生 SVG 頭像（姓名縮寫）
			var user = await _app.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
			var initials = user == null ? "U" : GetInitials(user.LastName + user.FirstName);
			var svg = $@"<svg xmlns='http://www.w3.org/2000/svg' width='240' height='240'>
  <rect width='100%' height='100%' fill='#e5e7eb'/>
  <text x='50%' y='55%' font-family='Segoe UI,Arial' font-size='96' fill='#374151' text-anchor='middle'>{initials}</text>
</svg>";
			return Content(svg, "image/svg+xml");
		}

		private static string GetInitials(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return "U";
			var s = new string(name.Where(char.IsLetterOrDigit).ToArray());
			return s.Length <= 2 ? s.ToUpperInvariant() : s.Substring(0, 2).ToUpperInvariant();
		}

		private static string? GetContentType(string path)
		{
			var ext = Path.GetExtension(path).ToLowerInvariant();
			return ext switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".webp" => "image/webp",
				_ => null
			};
		}

	}
	public sealed class AdminUserVm
	{
		public string Id { get; set; } = default!;
		[Display(Name = "姓")] public string LastName { get; set; } = "";
		[Display(Name = "名")] public string FirstName { get; set; } = "";
		public string Email { get; set; } = "";
		public string? PhoneNumber { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedDate { get; set; }
		public List<string> Roles { get; set; } = new();
	}
	
}
