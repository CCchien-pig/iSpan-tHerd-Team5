using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using tHerdBackend.Core.Abstractions.Referral;
using tHerdBackend.Core.Abstractions.Security;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.USER;

namespace tHerdBackend.SharedApi.Controllers.Common
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private readonly SignInManager<ApplicationUser> _signInMgr;
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly IJwtTokenService _jwt;
		private readonly IRefreshTokenService _refreshSvc;
		private readonly IReferralCodeGenerator _refGen;
		private readonly IConfiguration _cfg;

		public AuthController(
			SignInManager<ApplicationUser> signInMgr,
			UserManager<ApplicationUser> userMgr,
			IJwtTokenService jwt,
			IRefreshTokenService refreshSvc,
			IReferralCodeGenerator refGen,
			IConfiguration cfg)

		{
			_signInMgr = signInMgr;
			_userMgr = userMgr;
			_jwt = jwt;
			_refreshSvc = refreshSvc;
			_refGen = refGen;
			_cfg = cfg;
		}

		[AllowAnonymous]
		[HttpGet("ExternalLogin")]
		public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] bool rememberMe = true, [FromQuery] string? redirect = "/")
		{
			var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/auth/ExternalLoginCallback" +
		$"?rememberMe={rememberMe}&redirect={Uri.EscapeDataString(redirect ?? "/")}";
			var props = _signInMgr.ConfigureExternalAuthenticationProperties(provider, callbackUrl);
			return Challenge(props, provider);
		}

		// GET /auth/Account/ExternalLoginCallback?rememberMe=true&redirect=/user/me
		[AllowAnonymous]
		[HttpGet("ExternalLoginCallback")]
		public async Task<IActionResult> ExternalLoginCallback([FromQuery] bool rememberMe = true, [FromQuery] string? redirect = "/")
		{
			var info = await _signInMgr.GetExternalLoginInfoAsync();
			if (info == null)
				return Redirect($"{(redirect ?? "/")}?login=failed");

			// 取外部提供者 claims
			var email = info.Principal.FindFirstValue(ClaimTypes.Email);
			var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
			var familyName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";
			var displayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? (email ?? "");

			if (string.IsNullOrWhiteSpace(email))
				return Redirect($"{(redirect ?? "/")}?login=failed");

			var signInResult = await _signInMgr.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: rememberMe);
			ApplicationUser user;

			if (!signInResult.Succeeded)
			{
				// 沒綁定過 → 用 email 找；沒有就新建（對齊 register：IsActive / MemberRankId / ReferralCode）
				user = await _userMgr.FindByEmailAsync(email);
				if (user == null)
				{
					// ★ 產生推薦碼（與 register 對齊）
					var referralCode = await GenerateUniqueReferralCodeAsync();

					user = new ApplicationUser
					{
						UserName = email,
						Email = email,
						EmailConfirmed = true,   // Google 基本驗證過 email
						LastName = familyName,
						FirstName = givenName,
						IsActive = true,         // ★ 對齊 register
						MemberRankId = "MR001",  // ★ 對齊 register 預設等級
						ReferralCode = referralCode,   // ★ 對齊 register
					};
					var createRes = await _userMgr.CreateAsync(user);
					if (!createRes.Succeeded)
						return Redirect($"{(redirect ?? "/")}?login=failed");

					// 與 register 對齊：預設加入 Member 角色
					try { await _userMgr.AddToRoleAsync(user, "Member"); } catch { /* 忽略失敗，不影響登入 */ }
				}
				else
				{
					// 帳號已存在但未有推薦碼 → 回填一次（與 register 規則一致）
					if (string.IsNullOrEmpty(user.ReferralCode))
					{
						user.ReferralCode = await GenerateUniqueReferralCodeAsync();
						await _userMgr.UpdateAsync(user);
					}
					// 若帳號被停用
					if (!user.IsActive)
						return Redirect($"{(redirect ?? "/")}?login=disabled");
				}

				// 綁定外部登入
				var bindRes = await _userMgr.AddLoginAsync(user, info);
				if (!bindRes.Succeeded)
					return Redirect($"{(redirect ?? "/")}?login=failed");
			}
			else
			{
				// 已綁定 → 取回 user
				user = await _userMgr.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
					   ?? await _userMgr.FindByEmailAsync(email);
				if (user == null)
					return Redirect($"{(redirect ?? "/")}?login=failed");

				// 若已存在但沒推薦碼 → 也補上
				if (string.IsNullOrEmpty(user.ReferralCode))
				{
					user.ReferralCode = await GenerateUniqueReferralCodeAsync();
					await _userMgr.UpdateAsync(user);
				}
				if (!user.IsActive)
					return Redirect($"{(redirect ?? "/")}?login=disabled");
			}

			// 發 JWT + Refresh（沿用既有）
			var roles = await _userMgr.GetRolesAsync(user);
			var (accessToken, accessExpiresUtc, jti) = _jwt.Generate(user, roles);
			var (refreshPlain, _) = await _refreshSvc.IssueAsync(user.Id, jti);

			// 回前端
			var frontCallback = _cfg["Auth:FrontCallback"] ?? "/user/login/callback";
			var target =
				$"{frontCallback}" +
				$"?token={Uri.EscapeDataString(accessToken)}" +
				$"&refresh={Uri.EscapeDataString(refreshPlain)}" +
				$"&exp={Uri.EscapeDataString(accessExpiresUtc.ToString("o"))}" +
				$"&rememberMe={(rememberMe ? "1" : "0")}" +
				$"&redirect={Uri.EscapeDataString(redirect ?? "/")}";

			return Redirect(target);
		}
		public record RecaptchaVerifyResponse(bool success, decimal score, string action, DateTime challenge_ts, string hostname, string[]? errorCodes);
		
		[AllowAnonymous]
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDto dto, [FromServices] IHttpClientFactory httpClientFactory, IConfiguration cfg)
		{
			if (string.IsNullOrWhiteSpace(dto?.Email) || string.IsNullOrWhiteSpace(dto?.Password))
				return BadRequest(new { error = "請輸入帳號與密碼" });

			var user = await _userMgr.FindByEmailAsync(dto.Email.Trim());
			if (user == null || !user.IsActive)
				return Unauthorized(new { error = "帳號不存在或已停用" });

			var signIn = await _signInMgr.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
			if (signIn.IsLockedOut) return Unauthorized(new { error = "帳號已被鎖定" });
			if (!signIn.Succeeded) return Unauthorized(new { error = "帳號或密碼錯誤" });

			var roles = await _userMgr.GetRolesAsync(user);
			var (accessToken, accessExpiresUtc, jti) = _jwt.Generate(user, roles);

			var secret = cfg["Recaptcha:Secret"]; // 請從組態讀取
			using var http = httpClientFactory.CreateClient();
			var content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["secret"] = secret,
				["response"] = dto.RecaptchaToken
				// 可加 ["remoteip"] = HttpContext.Connection.RemoteIpAddress?.ToString()
			});
			var resp = await http.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
			var json = await resp.Content.ReadFromJsonAsync<RecaptchaVerifyResponse>();

			if (json is null || !json.success)
				return BadRequest(new { error = "reCAPTCHA 驗證失敗" });
			// ⚠ 介面已改：IssueAsync 使用 userId（string），不再傳 ApplicationUser / EF 實體
			var (refreshPlain, _) = await _refreshSvc.IssueAsync(user.Id, jti);

			return Ok(new
			{
				accessToken,
				accessExpiresAt = accessExpiresUtc,
				refreshToken = refreshPlain,
				user = new
				{
					id = user.Id,
					email = user.Email,
					name = $"{user.LastName}{user.FirstName}",
					userNumberId = user.UserNumberId,
					roles
				}
			});
		}

		public class RefreshDto { public string? RefreshToken { get; set; } }

		/// <summary>
		/// 使用 refresh token 旋轉/續期：返回新的 access & refresh
		/// </summary>
		[AllowAnonymous]
		[HttpPost("refresh")]
		public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto?.RefreshToken))
				return BadRequest(new { error = "缺少 refresh token" });

			// 1) 先驗證 refresh（新介面：回傳 userId 與 RefreshTokenInfo）
			var (userId, current) = await _refreshSvc.ValidateAsync(dto.RefreshToken);

			// 2) 依 userId 取 Identity 使用者與角色，產新 access（新 jti）
			var user = await _userMgr.FindByIdAsync(userId) ?? throw new InvalidOperationException("User not found");
			if (!user.IsActive) return Unauthorized(new { error = "帳號不存在或已停用" });

			var roles = await _userMgr.GetRolesAsync(user);
			var (accessToken, accessExpiresUtc, newJti) = _jwt.Generate(user, roles);

			// 3) 旋轉 refresh（新介面：傳入 RefreshTokenInfo + userId）
			var (newRefreshPlain, _) = await _refreshSvc.RotateAsync(current, newJti, user.Id);

			return Ok(new
			{
				accessToken,
				accessExpiresAt = accessExpiresUtc,
				refreshToken = newRefreshPlain
			});
		}

		/// <summary>
		/// 登出：撤銷傳入的 refresh token（access token 仍會自然過期）
		/// </summary>
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpPost("logout")]
		public async Task<IActionResult> Logout([FromBody] RefreshDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto?.RefreshToken))
				return BadRequest(new { error = "缺少 refresh token" });

			// 先驗證拿到 RefreshTokenInfo，再用其 Id 撤銷（介面不暴露 EF 實體）
			var (_, current) = await _refreshSvc.ValidateAsync(dto.RefreshToken);
			await _refreshSvc.RevokeAsync(current.Id, "user logout");

			return Ok(new { ok = true });
		}

		/// <summary>
		/// 取得目前登入者（從 access token）
		/// </summary>
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpGet("me")]
		public IActionResult Me()
		{
			var sub = User.FindFirstValue("sub");
			var mail = User.FindFirstValue("email");
			var name = User.FindFirstValue("name");
			var num = User.FindFirstValue("user_number_id");
			var roles = User.FindAll("role").Select(c => c.Value).ToArray();

			if (string.IsNullOrEmpty(sub)) return Unauthorized(new { error = "Token 無效或已過期" });

			return Ok(new
			{
				id = sub,
				email = mail,
				name,
				userNumberId = int.TryParse(num, out var n) ? n : 0,
				roles
			});
		}

		[AllowAnonymous]
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterDto dto)
		{
			if (dto is null) return BadRequest(new { error = "payload 不可為空" });
			if (string.IsNullOrWhiteSpace(dto.Email) ||
				string.IsNullOrWhiteSpace(dto.Password) ||
				string.IsNullOrWhiteSpace(dto.LastName) ||
				string.IsNullOrWhiteSpace(dto.FirstName) ||
				string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
				string.IsNullOrWhiteSpace(dto.Gender))
			{
				return BadRequest(new { error = "請完整填寫必填欄位" });
			}

			var gender = dto.Gender.Trim();
			if (gender != "男" && gender != "女")
				return BadRequest(new { error = "性別僅接受「男」或「女」" });

			var existed = await _userMgr.FindByEmailAsync(dto.Email);
			if (existed != null) return BadRequest(new { error = "此 Email 已被註冊" });

			// ★ 產生推薦碼（可選擇性做唯一檢查）
			string referralCode = await GenerateUniqueReferralCodeAsync();

			var user = new ApplicationUser
			{
				UserName = dto.Email.Trim(),
				Email = dto.Email.Trim(),
				EmailConfirmed = false,
				PhoneNumber = dto.PhoneNumber.Trim(),
				LastName = dto.LastName.Trim(),
				FirstName = dto.FirstName.Trim(),
				Gender = gender,
				IsActive = true,
				MemberRankId = "MR001",
				UsedReferralCode = dto.UsedReferralCode?.Trim(),
				ReferralCode = referralCode // ★ 指派
			};

			var createRes = await _userMgr.CreateAsync(user, dto.Password);
			if (!createRes.Succeeded)
			{
				var msg = string.Join("; ", createRes.Errors.Select(e => e.Description));
				return StatusCode(500, new { error = "建立帳號失敗：" + msg });
			}

			await _userMgr.AddToRoleAsync(user, "Member");

			return Ok(new { ok = true, userId = user.Id, referralCode = user.ReferralCode });
		}

		// ★ 極低機率碰撞時的唯一性檢查（最多重試 5 次）
		//    若你的 DB 有 UNIQUE 索引也可在異常時重試一次。
		private async Task<string> GenerateUniqueReferralCodeAsync(int maxRetry = 5)
		{
			for (int i = 0; i < maxRetry; i++)
			{
				var code = _refGen.Generate();
				var exists = await _userMgr.Users.AnyAsync(u => u.ReferralCode == code);
				if (!exists) return code;
			}
			// 退而求其次，最後仍回傳一個新碼（碰撞機率極低）
			return _refGen.Generate();
		}
	}
}