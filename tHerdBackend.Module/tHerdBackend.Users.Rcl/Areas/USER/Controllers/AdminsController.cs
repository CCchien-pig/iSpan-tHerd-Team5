using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Web_Datatables;
using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.USER.Rcl.Areas.USER.Controllers
{
	[Area("USER")]
	[Controller]
	public class AdminsController : Controller
	{
		private readonly ApplicationDbContext _app;
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly RoleManager<ApplicationRole> _roleMgr;

		public AdminsController(
			ApplicationDbContext app,
			UserManager<ApplicationUser> userMgr,
			RoleManager<ApplicationRole> roleMgr)
		{
			_app = app;
			_userMgr = userMgr;
			_roleMgr = roleMgr;
		}

		public IActionResult Index() => View();

		// 角色下拉（排除 Member）
		[HttpGet]
		public async Task<IActionResult> RolesCombo()
		{
			var roles = await _roleMgr.Roles
				.Where(r => r.Name != "Member")
				.OrderBy(r => r.Name)
				.Select(r => new { id = r.Id, name = r.Name })
				.ToListAsync();
			return Json(roles);
		}

		// 員工清單（排除只有 Member 的帳號；可依角色篩選）
		[HttpGet]
		public async Task<IActionResult> Users([FromQuery] DataTableRequest? dt, [FromQuery] string? roleId)
		{
			var draw = dt?.Draw ?? 0;
			var start = Math.Max(0, dt?.Start ?? 0);
			var length = (dt?.Length ?? 10) > 0 ? dt!.Length : 10;

			var kw = dt?.Search?.Value;
			if (string.IsNullOrWhiteSpace(kw))
				kw = Request.Query["search[value]"].ToString();
			kw = kw?.Trim();

			// 有非 Member 角色的使用者
			var nonMemberRoleIds = await _roleMgr.Roles
	.Where(r => r.Name != "Member")
	.Select(r => r.Id)
	.ToListAsync();                         // ★ 新增

			var userIdsWithNonMember = _app.UserRoles
				.Where(ur => nonMemberRoleIds.Contains(ur.RoleId))
				.Select(ur => ur.UserId);

			var q = _app.Users.AsNoTracking().Where(u => userIdsWithNonMember.Contains(u.Id));

			// 角色篩選
			if (!string.IsNullOrEmpty(roleId))
				q = q.Where(u => _app.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));

			var recordsTotal = await q.CountAsync();

			// 搜尋（姓名/Email/電話）
			if (!string.IsNullOrEmpty(kw))
			{
				q = q.Where(u =>
					(u.LastName + " " + u.FirstName).Contains(kw) ||
					(u.Email ?? "").Contains(kw) ||
					(u.PhoneNumber ?? "").Contains(kw));
			}

			var recordsFiltered = await q.CountAsync();

			// ---- 排序（以 DataTables columns[n][data] 名稱為準；若缺就預設 CreatedDate DESC）----
			string? orderColIdxStr = Request.Query["order[0][column]"];
			string? orderDir = Request.Query["order[0][dir]"];
			bool desc = string.Equals(orderDir, "desc", StringComparison.OrdinalIgnoreCase);

			IOrderedQueryable<ApplicationUser> ordered; // 外層宣告

			if (int.TryParse(orderColIdxStr, out var orderIdx))
			{
				// e.g. number / lastName / firstName / email / phoneNumber / isActive / createdDate / (空字串為操作欄)
				var colDataKey = Request.Query[$"columns[{orderIdx}][data]"].ToString();

				ordered = colDataKey switch
				{
					"number" => desc ? q.OrderByDescending(u => u.UserNumberId).ThenByDescending(u => u.LastName)
										  : q.OrderBy(u => u.UserNumberId).ThenBy(u => u.LastName),
					"lastName" => desc ? q.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName)
										  : q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
					"firstName" => desc ? q.OrderByDescending(u => u.FirstName)
										  : q.OrderBy(u => u.FirstName),
					"email" => desc ? q.OrderByDescending(u => u.Email)
										  : q.OrderBy(u => u.Email),
					"phoneNumber" => desc ? q.OrderByDescending(u => u.PhoneNumber)
										  : q.OrderBy(u => u.PhoneNumber),
					"isActive" => desc ? q.OrderByDescending(u => u.IsActive)
										  : q.OrderBy(u => u.IsActive),
					"createdDate" => desc ? q.OrderByDescending(u => u.CreatedDate)
										  : q.OrderBy(u => u.CreatedDate),
					_ => desc ? q.OrderByDescending(u => u.CreatedDate)   // roleId / 操作欄 或未知欄位
										  : q.OrderBy(u => u.CreatedDate),
				};
			}
			else
			{
				// 沒給排序參數 → 預設
				ordered = q.OrderByDescending(u => u.CreatedDate);
			}

			// ↓ 之後用 ordered 取頁
			var page = await ordered
	.Skip(start).Take(length)
	.Select(u => new
	{
		u.Id,
		u.UserNumberId,
		u.LastName,
		u.FirstName,
		u.Email,
		u.PhoneNumber,
		u.IsActive,
		u.CreatedDate
	})
	.ToListAsync();

			var ids = page.Select(p => p.Id).ToList();

			var roles = await (from ur in _app.UserRoles
							   join r in _app.Roles on ur.RoleId equals r.Id
							   where ids.Contains(ur.UserId) && r.Name != "Member"
							   select new { ur.UserId, r.Id, r.Name })
							   .ToListAsync();

			var primaryRole = roles
				.GroupBy(x => x.UserId)
				.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).First());

			var data = page.Select(u => new
			{
				id = u.Id,
				number = u.UserNumberId,
				lastName = u.LastName,
				firstName = u.FirstName,
				email = u.Email,
				phoneNumber = u.PhoneNumber,
				roleId = primaryRole.TryGetValue(u.Id, out var rr) ? rr.Id : null,
				roleName = primaryRole.TryGetValue(u.Id, out var rr2) ? rr2.Name : "",
				isActive = u.IsActive,
				createdDate = u.CreatedDate.ToString("yyyy-MM-dd HH:mm")
			});

			return Json(new { draw, recordsTotal, recordsFiltered, data });
		}

		// 就地編輯欄位
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateField([FromForm] string userId, [FromForm] string field, [FromForm] string? value)
		{
			var u = await _userMgr.Users.FirstOrDefaultAsync(x => x.Id == userId);
			if (u == null) return NotFound(new { ok = false, message = "找不到使用者" });

			switch (field)
			{
				case "lastName": u.LastName = value?.Trim() ?? ""; break;
				case "firstName": u.FirstName = value?.Trim() ?? ""; break;
				case "email":
					if (string.IsNullOrWhiteSpace(value)) return BadRequest(new { ok = false, message = "Email 必填" });
					var email = value.Trim();
					u.Email = email;
					u.UserName = email;
					u.NormalizedEmail = _userMgr.NormalizeEmail(email);
					u.NormalizedUserName = _userMgr.NormalizeName(email);
					break;
				case "phoneNumber": u.PhoneNumber = value?.Trim(); break;
				case "isActive":
					u.IsActive = value == "true" || value == "1" || value?.ToLowerInvariant() == "on";
					if (u.IsActive && u.ActivationDate == null) u.ActivationDate = DateTime.UtcNow;
					break;
				default:
					return BadRequest(new { ok = false, message = "不允許的欄位" });
			}

			u.RevisedDate = DateTime.UtcNow;
			var res = await _userMgr.UpdateAsync(u);
			if (!res.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", res.Errors.Select(e => e.Description)) });

			return Json(new { ok = true });
		}

		// 更換角色（單一主要角色；排除 Member）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangeRole([FromForm] string userId, [FromForm] string roleId)
		{
			var user = await _userMgr.FindByIdAsync(userId);
			if (user == null) return NotFound(new { ok = false, message = "找不到使用者" });

			var role = await _roleMgr.FindByIdAsync(roleId);
			if (role == null || role.Name == "Member")
				return BadRequest(new { ok = false, message = "角色錯誤" });

			var currRoleIds = await _app.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync();
			var toRemove = await _roleMgr.Roles.Where(r => currRoleIds.Contains(r.Id) && r.Name != "Member")
											   .Select(r => r.Name!).ToListAsync();
			if (toRemove.Count > 0)
			{
				var rm = await _userMgr.RemoveFromRolesAsync(user, toRemove);
				if (!rm.Succeeded) return BadRequest(new { ok = false, message = string.Join("; ", rm.Errors.Select(e => e.Description)) });
			}

			var addRes = await _userMgr.AddToRoleAsync(user, role.Name!);
			if (!addRes.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", addRes.Errors.Select(e => e.Description)) });

			return Json(new { ok = true });
		}

		// ★ 詳細資料（帶出目前角色）
		[HttpGet]
		public async Task<IActionResult> Details(string id)
		{
			var u = await _app.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
			if (u == null) return NotFound();

			var roleId = await (from ur in _app.UserRoles
								join r in _app.Roles on ur.RoleId equals r.Id
								where ur.UserId == id && r.Name != "Member"
								select r.Id).FirstOrDefaultAsync();

			return Json(new
			{
				id = u.Id,
				number = u.UserNumberId,
				lastName = u.LastName,
				firstName = u.FirstName,
				email = u.Email,
				phoneNumber = u.PhoneNumber,
				gender = u.Gender,
				birthDate = u.BirthDate?.ToString("yyyy-MM-dd"),
				address = u.Address,
				isActive = u.IsActive,
				roleId
			});
		}

		// ★ 詳細資料儲存（含角色）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateDetails(
			[FromForm] string id,
			[FromForm] string lastName,
			[FromForm] string firstName,
			[FromForm] string email,
			[FromForm] string? phoneNumber,
			[FromForm] string? gender,
			[FromForm] DateTime? birthDate,
			[FromForm] string? address,
			[FromForm] bool isActive,
			[FromForm] string roleId)
		{
			var user = await _userMgr.FindByIdAsync(id);
			if (user == null) return BadRequest(new { ok = false, message = "使用者不存在" });

			var role = await _roleMgr.FindByIdAsync(roleId);
			if (role == null || role.Name == "Member")
				return BadRequest(new { ok = false, message = "角色無效" });

			if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(email))
				return BadRequest(new { ok = false, message = "必填欄位不足" });

			// 基本欄位
			user.LastName = lastName.Trim();
			user.FirstName = firstName.Trim();
			user.Gender = string.IsNullOrWhiteSpace(gender) ? (user.Gender ?? "N/A") : gender.Trim(); // 避免 NOT NULL 失敗
			user.BirthDate = birthDate;
			user.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
			user.IsActive = isActive;

			// Email / UserName 正規化
			var newEmail = email.Trim();
			user.Email = newEmail;
			user.UserName = newEmail;
			user.NormalizedEmail = _userMgr.NormalizeEmail(newEmail);
			user.NormalizedUserName = _userMgr.NormalizeName(newEmail);

			user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
			user.RevisedDate = DateTime.UtcNow;

			var res = await _userMgr.UpdateAsync(user);
			if (!res.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", res.Errors.Select(e => e.Description)) });

			// 更新角色（同 ChangeRole）
			var currRoleIds = await _app.UserRoles.Where(ur => ur.UserId == id).Select(ur => ur.RoleId).ToListAsync();
			var toRemove = await _roleMgr.Roles.Where(r => currRoleIds.Contains(r.Id) && r.Name != "Member")
											   .Select(r => r.Name!).ToListAsync();
			if (toRemove.Count > 0)
			{
				var rm = await _userMgr.RemoveFromRolesAsync(user, toRemove);
				if (!rm.Succeeded) return BadRequest(new { ok = false, message = string.Join("; ", rm.Errors.Select(e => e.Description)) });
			}
			var addRes = await _userMgr.AddToRoleAsync(user, role.Name!);
			if (!addRes.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", addRes.Errors.Select(e => e.Description)) });

			return Json(new { ok = true });
		}

		// ★ 刪除員工（僅允許刪除具有非 Member 角色者）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete([FromForm] string userId)
		{
			var user = await _userMgr.FindByIdAsync(userId);
			if (user == null) return NotFound(new { ok = false, message = "使用者不存在" });

			// 確認是否為「員工」（有非 Member 角色）
			var hasNonMemberRole = await (from ur in _app.UserRoles
										  join r in _app.Roles on ur.RoleId equals r.Id
										  where ur.UserId == userId && r.Name != "Member"
										  select ur).AnyAsync();
			if (!hasNonMemberRole)
				return BadRequest(new { ok = false, message = "此帳號非員工，無法在此刪除" });

			var res = await _userMgr.DeleteAsync(user);
			if (!res.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", res.Errors.Select(e => e.Description)) });

			return Json(new { ok = true });
		}

		// 新增員工（修正：補 Gender 避免 DB NOT NULL；Email/UserName 正規化）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([FromForm] string email,
												[FromForm] string lastName,
												[FromForm] string firstName,
												[FromForm] string roleId,
												[FromForm] string? phone)
		{
			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
				return BadRequest(new { ok = false, message = "必填欄位缺失" });

			var role = await _roleMgr.FindByIdAsync(roleId);
			if (role == null || role.Name == "Member")
				return BadRequest(new { ok = false, message = "角色錯誤" });

			if (await _userMgr.FindByEmailAsync(email) != null)
				return BadRequest(new { ok = false, message = "Email 已存在" });

			var normalizedEmail = _userMgr.NormalizeEmail(email.Trim());
			var normalizedUserName = _userMgr.NormalizeName(email.Trim());

			var user = new ApplicationUser
			{
				Id = Guid.NewGuid().ToString(),
				UserNumberId = 0,
				UserName = email.Trim(),
				NormalizedUserName = normalizedUserName,
				Email = email.Trim(),
				NormalizedEmail = normalizedEmail,
				LastName = lastName.Trim(),
				FirstName = firstName.Trim(),
				PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
				// ★ 關鍵：DB 欄位 Gender NOT NULL，給預設值
				Gender = "N/A",
				IsActive = true,
				CreatedDate = DateTime.UtcNow,
				ActivationDate = DateTime.UtcNow,
				EmailConfirmed = true,
				SecurityStamp = Guid.NewGuid().ToString()
			};

			// 密碼須符合 Identity Policy（大寫/小寫/數字/特殊字元）
			var tempPwd = "Init@12345";
			var createRes = await _userMgr.CreateAsync(user, tempPwd);
			if (!createRes.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", createRes.Errors.Select(e => e.Description)) });

			var addRoleRes = await _userMgr.AddToRoleAsync(user, role.Name!);
			if (!addRoleRes.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", addRoleRes.Errors.Select(e => e.Description)) });

			return Json(new { ok = true });
		}

		// NEW: 修改密碼（管理員重設）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword([FromForm] string userId, [FromForm] string newPassword)
		{
			var strong = System.Text.RegularExpressions.Regex.IsMatch(
				newPassword ?? "", @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$");
			if (!strong) return BadRequest(new { ok = false, message = "密碼需至少 8 碼，且含大小寫與數字。" });

			var user = await _userMgr.FindByIdAsync(userId);
			if (user == null) return NotFound(new { ok = false, message = "使用者不存在" });

			var token = await _userMgr.GeneratePasswordResetTokenAsync(user);
			var res = await _userMgr.ResetPasswordAsync(user, token, newPassword);
			if (!res.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", res.Errors.Select(e => e.Description)) });

			return Json(new { ok = true });
		}
	}

}
