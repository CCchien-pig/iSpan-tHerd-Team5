using FlexBackend.Core.Abstractions;
using FlexBackend.Core.DTOs.USER;
using FlexBackend.Infra.Models;
// Areas/USER/Controllers/MeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.USER.Rcl.Areas.USER.Controllers
{
	[Area("USER")]
	//[Authorize]
	[Route("USER/[controller]/[action]")]   // ← 這行讓你在任何地方都可用 /USER/Me/Details 等固定路徑
	public class MeController : Controller
	{
		private readonly tHerdDBContext _db;
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly SignInManager<ApplicationUser> _signInMgr;
		private readonly ICurrentUser _me;

		public MeController(
			tHerdDBContext db,
			UserManager<ApplicationUser> userMgr,
			SignInManager<ApplicationUser> signInMgr,
			ICurrentUser me)
		{
			_db = db;
			_userMgr = userMgr;
			_signInMgr = signInMgr;
			_me = me;
		}

		// 讀取目前登入者資料（給前端 Modal 帶值）
		[HttpGet]
		public async Task<IActionResult> Details()
		{
			var u = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == _me.Id);
			if (u == null) return NotFound();

			return Json(new
			{
				id = u.Id,
				number = u.UserNumberId,
				lastName = u.LastName,
				firstName = u.FirstName,
				email = u.Email,
				phoneNumber = u.PhoneNumber,
				gender = u.Gender ?? "N/A",
				birthDate = u.BirthDate?.ToString("yyyy-MM-dd"),
				address = u.Address,
				isActive = u.IsActive
			});
		}

		// 更新自己的基本資料
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateProfile(
			[FromForm] string lastName,
			[FromForm] string firstName,
			[FromForm] string email,
			[FromForm] string? phoneNumber,
			[FromForm] string? gender,
			[FromForm] DateTime? birthDate,
			[FromForm] string? address
		)
		{
			var user = await _userMgr.FindByIdAsync(_me.Id);
			if (user == null) return BadRequest(new { ok = false, message = "使用者不存在" });
			if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(email))
				return BadRequest(new { ok = false, message = "必填欄位不足" });

			user.LastName = lastName.Trim();
			user.FirstName = firstName.Trim();
			user.Gender = string.IsNullOrWhiteSpace(gender) ? (user.Gender ?? "N/A") : gender.Trim();
			user.BirthDate = birthDate;
			user.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

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

			// 更新 Cookie 內的自訂 Claims（FullName / UserNumberId）
			await _signInMgr.RefreshSignInAsync(user);

			return Json(new { ok = true });
		}

		// 自己改密碼（需要舊密碼；新密碼強度檢查）
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword(
			[FromForm] string oldPassword,
			[FromForm] string newPassword)
		{
			if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
				return BadRequest(new { ok = false, message = "請輸入舊密碼與新密碼" });

			var strong = System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$");
			if (!strong)
				return BadRequest(new { ok = false, message = "新密碼需至少 8 碼且含英文大小寫與數字" });

			var user = await _userMgr.FindByIdAsync(_me.Id);
			if (user == null) return BadRequest(new { ok = false, message = "使用者不存在" });

			var res = await _userMgr.ChangePasswordAsync(user, oldPassword, newPassword);
			if (!res.Succeeded)
				return BadRequest(new { ok = false, message = string.Join("; ", res.Errors.Select(e => e.Description)) });

			await _signInMgr.RefreshSignInAsync(user);
			return Json(new { ok = true });
		}
	}
}


