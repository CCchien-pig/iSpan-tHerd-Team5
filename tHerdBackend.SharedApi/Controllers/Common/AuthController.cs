using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using tHerdBackend.Core.Abstractions.Referral;
using tHerdBackend.Core.Abstractions.Security;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Services.USER;

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
		private readonly IEmailSender _email;
		private readonly IDistributedCache _cache;

		public AuthController(
			SignInManager<ApplicationUser> signInMgr,
			UserManager<ApplicationUser> userMgr,
			IJwtTokenService jwt,
			IRefreshTokenService refreshSvc,
			IReferralCodeGenerator refGen,
			IConfiguration cfg,
			IEmailSender email,
			IDistributedCache cache)

		{
			_signInMgr = signInMgr;
			_userMgr = userMgr;
			_jwt = jwt;
			_refreshSvc = refreshSvc;
			_refGen = refGen;
			_cfg = cfg;
			_email = email;
			_cache = cache;
		}

		public record TwoFactorSessionPayload(string UserId, bool RememberMe, DateTime CreatedUtc);

		private static string TwoFaCacheKey(string sessionId) => $"2fa:session:{sessionId}";

		private async Task<string> CreateTwoFaSessionAsync(string userId, bool rememberMe)
		{
			var sessionId = Guid.NewGuid().ToString("N");
			var payload = new TwoFactorSessionPayload(userId, rememberMe, DateTime.UtcNow);
			var json = JsonSerializer.Serialize(payload);
			var options = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
			};
			await _cache.SetStringAsync(TwoFaCacheKey(sessionId), json, options);
			return sessionId;
		}

		private async Task<TwoFactorSessionPayload?> ReadTwoFaSessionAsync(string sessionId)
		{
			var json = await _cache.GetStringAsync(TwoFaCacheKey(sessionId));
			if (string.IsNullOrEmpty(json)) return null;
			return JsonSerializer.Deserialize<TwoFactorSessionPayload>(json);
		}

		private Task DeleteTwoFaSessionAsync(string sessionId)
			=> _cache.RemoveAsync(TwoFaCacheKey(sessionId));

		[AllowAnonymous]
		[HttpGet("guest")]
		public IActionResult Guest()
		{
			var id = HttpContext.Session.GetString("guestId");
			if (string.IsNullOrEmpty(id))
			{
				id = Guid.NewGuid().ToString("N");
				HttpContext.Session.SetString("guestId", id);
			}
			return Ok(new { guestId = id });
		}

		[AllowAnonymous]
		[HttpGet("ExternalLogin")]
		public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] bool rememberMe = true, [FromQuery] string? redirect = "/")
		{
			var callbackUrl = Url.ActionLink(
		action: nameof(ExternalLoginCallback),
		controller: "Auth",
		values: new { rememberMe, redirect }  // query string
	);
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
						Gender = "未填"
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
		public async Task<IActionResult> Login([FromBody] LoginDto dto,
	[FromServices] IHttpClientFactory httpClientFactory, IConfiguration cfg)
		{
			if (string.IsNullOrWhiteSpace(dto?.Email) || string.IsNullOrWhiteSpace(dto?.Password))
				return BadRequest(new { error_code = "missing_fields", message = "請輸入帳號與密碼" });

			var user = await _userMgr.FindByEmailAsync(dto.Email.Trim());
			if (user == null || !user.IsActive)
				return Unauthorized(new { error_code = "not_found_or_inactive", message = "帳號不存在或已停用" });

			// ---- 建議：先驗 recaptcha ----
			var secret = cfg["Recaptcha:Secret"];
			using (var http = httpClientFactory.CreateClient())
			{
				var content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["secret"] = secret,
					["response"] = dto.RecaptchaToken
				});
				var resp = await http.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
				var json = await resp.Content.ReadFromJsonAsync<RecaptchaVerifyResponse>();
				if (json is null || !json.success)
					return BadRequest(new { error_code = "recaptcha_failed", message = "reCAPTCHA 驗證失敗" });
			}

			// ---- 驗密碼（會計數與鎖定）----
			var signIn = await _signInMgr.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

			if (signIn.IsLockedOut)
			{
				var lockoutEnd = await _userMgr.GetLockoutEndDateAsync(user); // UTC
				Response.Headers["Retry-After"] = TimeSpan.FromMinutes(1).TotalSeconds.ToString("0");
				return StatusCode(StatusCodes.Status423Locked, new
				{
					error_code = "account_locked",
					message = "帳號已被鎖定，請稍後再試",
					unlockAt = lockoutEnd?.UtcDateTime.ToString("o")
				});
			}

			if (signIn.IsNotAllowed)
			{
				var resendUrl = Url.ActionLink(nameof(ResendConfirmEmail), "Auth", new { email = user.Email }, Request.Scheme, Request.Host.ToString());
				return Unauthorized(new
				{
					error_code = "email_unconfirmed",
					message = "請先完成信箱驗證",
					resendUrl
				});
			}

			if (!signIn.Succeeded)
			{
				var max = _signInMgr.Options.Lockout.MaxFailedAccessAttempts;
				var remaining = Math.Max(0, max - user.AccessFailedCount);
				return Unauthorized(new
				{
					error_code = "bad_credentials",
					message = "帳號或密碼錯誤",
					remainingAttempts = remaining
				});
			}

			// ★ 密碼正確 → 需要 2FA？
			if (signIn.RequiresTwoFactor || user.TwoFactorEnabled)
			{
				// 建立臨時 twoFactorSession（保存 userId 與 rememberMe）
				var sessionId = await CreateTwoFaSessionAsync(user.Id, dto.RememberMe);
				return Ok(new
				{
					requiresTwoFactor = true,
					twoFactorSession = sessionId
				});
			}

			// ---- 不需要 2FA → 直接發 token ----
			var roles = await _userMgr.GetRolesAsync(user);
			var (accessToken, accessExpiresUtc, jti) = _jwt.Generate(user, roles);
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

		//Forgot password
		public record ForgotDto(string Email);
		public record ForgotVerifyDto(string Email, string Code);

		private static string ForgotOtpKey(string email) => $"forgot:otp:{email.ToLower()}";
		private static string ForgotThrottleKey(string email) => $"forgot:otp:throttle:{email.ToLower()}";

		[AllowAnonymous]
		[HttpPost("forgot")]
		public async Task<IActionResult> Forgot([FromBody] ForgotDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto?.Email))
				return BadRequest(new { error = "缺少 Email" });

			var email = dto.Email.Trim();
			var user = await _userMgr.FindByEmailAsync(email);
			// 為避免洩漏存在與否，一律回 200，但若存在才真的寄
			if (user is not null && user.IsActive)
			{
				// 節流：60 秒內只允許重寄一次
				var throttleKey = ForgotThrottleKey(email);
				var throttle = await _cache.GetStringAsync(throttleKey);
				if (!string.IsNullOrEmpty(throttle))
					return Ok(new { ok = true }); // 靜默略過

				var code = GenerateMailPassword.GenerateNumericCode(6);
				var options = new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
				};
				await _cache.SetStringAsync(ForgotOtpKey(email), code, options);

				// 節流 key，存活 60 秒
				await _cache.SetStringAsync(throttleKey, "1",
					new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) });

				var html = $@"
<p>您正在重設 tHerd 帳號的密碼。</p>
<p>請在 10 分鐘內輸入以下 6 位數驗證碼：</p>
<h2 style=""letter-spacing:2px;"">{code}</h2>
<p>若非您本人操作，請忽略本郵件。</p>";
				try { await _email.SendEmailAsync(email, "tHerd 密碼重設驗證碼", html); } catch { /* 忽略寄信異常以免洩漏 */ }
			}
			return Ok(new { ok = true });
		}

		[AllowAnonymous]
		[HttpPost("forgot/verify")]
		public async Task<IActionResult> ForgotVerify([FromBody] ForgotVerifyDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto?.Email) || string.IsNullOrWhiteSpace(dto?.Code))
				return BadRequest(new { error = "缺少 Email 或 驗證碼" });

			var email = dto.Email.Trim();
			var user = await _userMgr.FindByEmailAsync(email);
			if (user is null || !user.IsActive)
				return Unauthorized(new { error = "帳號不存在或已停用" });

			var cached = await _cache.GetStringAsync(ForgotOtpKey(email));
			if (string.IsNullOrEmpty(cached) || !string.Equals(cached, dto.Code.Trim(), StringComparison.Ordinal))
				return BadRequest(new { error = "驗證碼錯誤或已過期" });

			// 產生臨時密碼 & 重設
			var tempPwd = GenerateMailPassword.GenerateTempPassword(12);
			var resetToken = await _userMgr.GeneratePasswordResetTokenAsync(user);
			var reset = await _userMgr.ResetPasswordAsync(user, resetToken, tempPwd);
			if (!reset.Succeeded)
			{
				var msg = string.Join("; ", reset.Errors.Select(e => e.Description));
				return StatusCode(500, new { error = "重設密碼失敗：" + msg });
			}

			// 成功後可清除失敗次數與 Lockout
			await _userMgr.ResetAccessFailedCountAsync(user);
			await _userMgr.SetLockoutEndDateAsync(user, null);

			// 刪除 OTP（避免重放）
			await _cache.RemoveAsync(ForgotOtpKey(email));

			// 寄送臨時密碼
			var html = $@"
<p>您的密碼已重設成功。</p>
<p>臨時密碼如下（請妥善保管，並於登入後立刻修改）：</p>
<pre style=""font-size:16px"">{tempPwd}</pre>
<p>登入位置：<a href=""http://localhost:5173/user/login"">http://localhost:5173/user/login</a></p>";
			try { await _email.SendEmailAsync(email, "tHerd 臨時密碼", html); } catch { /* 可記 log */ }

			return Ok(new { ok = true });
		}

		public record Login2FaDto(string TwoFactorSession, string Code);

		[AllowAnonymous]
		[HttpPost("login-2fa")]
		public async Task<IActionResult> Login2Fa([FromBody] Login2FaDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto?.TwoFactorSession) || string.IsNullOrWhiteSpace(dto.Code))
				return BadRequest(new { error = "缺少 twoFactorSession 或 code" });

			var sess = await ReadTwoFaSessionAsync(dto.TwoFactorSession);
			if (sess is null)
				return Unauthorized(new { error = "2FA 會話已失效或不存在" });

			var user = await _userMgr.FindByIdAsync(sess.UserId);
			if (user == null || !user.IsActive)
				return Unauthorized(new { error = "帳號不存在或已停用" });

			var code = dto.Code.Replace(" ", "").Replace("-", "");
			var valid = await _userMgr.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code);
			if (!valid)
				return BadRequest(new { error = "驗證碼錯誤或已過期" });

			// （可選）成功後重置密碼失敗次數
			await _userMgr.ResetAccessFailedCountAsync(user);

			var roles = await _userMgr.GetRolesAsync(user);
			var (accessToken, accessExpiresUtc, jti) = _jwt.Generate(user, roles);
			var (refreshPlain, _) = await _refreshSvc.IssueAsync(user.Id, jti);

			// 用完即丟，避免重放
			await DeleteTwoFaSessionAsync(dto.TwoFactorSession);

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

			// 產生 Email 確認 token（URL-safe）
			var token = await _userMgr.GenerateEmailConfirmationTokenAsync(user);
			var tokenBytes = Encoding.UTF8.GetBytes(token);
			var tokenEncoded = WebEncoders.Base64UrlEncode(tokenBytes);

			// 回到 SharedApi 的確認端點（下面會新增 ConfirmEmail）
			var confirmUrl = Url.ActionLink(
				action: nameof(ConfirmEmail),
				controller: "Auth",
				values: new { userId = user.Id, token = tokenEncoded },
				protocol: Request.Scheme,
				host: Request.Host.ToString()
			);

			// 寄信
			var subject = "請完成 tHerd 帳號的信箱驗證";
			var html = $@"
    <p>親愛的 {user.LastName}{user.FirstName} 您好：</p>
    <p>請點擊以下連結完成信箱驗證：</p>
    <p><a href=""{confirmUrl}"">{confirmUrl}</a></p>
    <p>若不是您本人操作，請忽略本郵件。</p>";
			await _email.SendEmailAsync(user.Email!, subject, html);

			return Ok(new { ok = true, userId = user.Id, referralCode = user.ReferralCode });
		}

		[AllowAnonymous]
		[HttpGet("confirm-email")]
		public async Task<IActionResult> ConfirmEmail(
	[FromQuery] string userId,
	[FromQuery] string token,
	[FromQuery] string? redirect = "/")
		{
			// —— 工具函式：只允許相對路徑（避免外站跳轉）——
			static string SafeRedirect(string? raw)
				=> !string.IsNullOrWhiteSpace(raw) && raw.StartsWith("/") ? raw : "/";

			// 記錄 rememberMe（支援 1/0/true/false），預設視為 true
			var remember =
				string.Equals(Request.Query["rememberMe"], "1", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Request.Query["rememberMe"], "true", StringComparison.OrdinalIgnoreCase);
			var rememberStr = remember ? "1" : "0";

			// 你原本的落地頁 HTML（保留）
			string Html(string title, string bodyHtml, string goUrl)
			{
				return $@"<!doctype html>
<html lang=""zh-Hant"">
<head>
  <meta charset=""utf-8"">
  <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>{title}</title>
  <meta http-equiv=""refresh"" content=""2;url={goUrl}"">
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Noto Sans TC', 'Helvetica Neue', Arial, 'PingFang TC', 'Microsoft JhengHei', sans-serif; padding: 40px; color: #333; }}
    .card {{ max-width: 560px; margin: 0 auto; border: 1px solid #eee; border-radius: 10px; padding: 24px; box-shadow: 0 6px 16px rgba(0,0,0,.06); }}
    h1 {{ font-size: 22px; margin: 0 0 12px; }}
    p {{ margin: 8px 0; line-height: 1.6; }}
    a.btn {{ display: inline-block; margin-top: 10px; padding: 8px 14px; border-radius: 6px; text-decoration: none; background: #2e7d32; color: #fff; }}
  </style>
</head>
<body>
  <div class=""card"">
    {bodyHtml}
    <p>2 秒後將自動導向：<br><code>{goUrl}</code></p>
    <p><a class=""btn"" href=""{goUrl}"">立即前往</a></p>
  </div>
</body>
</html>";
			}

			// 前端資訊頁（導轉目標，用於 emailVerified 展示）
			var frontInfo = _cfg["Auth:EmailConfirmRedirect"] ?? "http://localhost:5173/user/me";
			// 構成「前端資訊頁 + emailVerified」的完整 URL（給失敗頁自動導轉用）
			string InfoWithVerified(string v) => $"{frontInfo}?emailVerified={v}";

			// 前端 callback（重用你外登邏輯來收 token）
			var frontCallback = _cfg["Auth:FrontCallback"] ?? "http://localhost:5173/user/login/callback";

			// 驗參數：失敗則回 HTML 落地頁（保留你現有體驗）
			if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
			{
				var html = Html("驗證失敗", "<h1>驗證失敗</h1><p>缺少必要參數。</p>", InfoWithVerified("0"));
				return Content(html, "text/html; charset=utf-8");
			}

			var user = await _userMgr.FindByIdAsync(userId);
			if (user == null)
			{
				var html = Html("驗證失敗", "<h1>驗證失敗</h1><p>找不到使用者。</p>", InfoWithVerified("0"));
				return Content(html, "text/html; charset=utf-8");
			}

			// 還原 token
			string realToken;
			try
			{
				var decoded = WebEncoders.Base64UrlDecode(token);
				realToken = Encoding.UTF8.GetString(decoded);
			}
			catch
			{
				var html = Html("驗證失敗", "<h1>驗證失敗</h1><p>驗證連結無效或已損毀。</p>", InfoWithVerified("0"));
				return Content(html, "text/html; charset=utf-8");
			}

			// 尚未確認才執行確認；已確認視為成功（避免重複點擊造成不友善）
			if (!user.EmailConfirmed)
			{
				var result = await _userMgr.ConfirmEmailAsync(user, realToken);
				if (!result.Succeeded)
				{
					var html = Html("驗證失敗", "<h1>驗證失敗</h1><p>連結無效或已過期，請重新索取驗證信。</p>", InfoWithVerified("0"));
					return Content(html, "text/html; charset=utf-8");
				}
			}

			// ✅ 確認成功 → 簽發 JWT + Refresh，讓前端 callback 儲存後自動導回個資頁
			var roles = await _userMgr.GetRolesAsync(user);
			var (accessToken, accessExpiresUtc, jti) = _jwt.Generate(user, roles);
			var (refreshPlain, _) = await _refreshSvc.IssueAsync(user.Id, jti);

			// 將 redirect（若寄信時帶了）限制為相對路徑，並附帶 emailVerified=1
			var safeRedirect = SafeRedirect(redirect);
			// 在 redirect 後面加上 emailVerified=1（用最簡單方式拼接；若有現成 UrlHelper 可改用）
			var redirectWithVerify = safeRedirect.Contains("?")
				? $"{safeRedirect}&emailVerified=1"
				: $"{safeRedirect}?emailVerified=1";

			// 組 callback URL（前端 UserLoginCallback.vue 會收下並登入，然後 replace 到 redirect）
			var target =
				$"{frontCallback}" +
				$"?token={Uri.EscapeDataString(accessToken)}" +
				$"&refresh={Uri.EscapeDataString(refreshPlain)}" +
				$"&exp={Uri.EscapeDataString(accessExpiresUtc.ToString("o"))}" +
				$"&rememberMe=1" +
				$"&redirect={Uri.EscapeDataString(redirectWithVerify)}";

			// ↪️ 最終仍「Redirect」到前端 callback（維持你原本的導轉結構，並保留上方的 HTML 失敗頁）
			return Redirect(target);
		}

		[AllowAnonymous]
		[HttpPost("resend-confirm")]
		public async Task<IActionResult> ResendConfirmEmail([FromBody] EmailDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto?.Email))
				return BadRequest(new { error_code = "missing_fields", message = "缺少 Email" });

			var user = await _userMgr.FindByEmailAsync(dto.Email.Trim());
			if (user == null) return Ok(new { ok = true }); // 為了避免暴露使用者資訊，回 ok

			if (user.EmailConfirmed) return Ok(new { ok = true });

			var token = await _userMgr.GenerateEmailConfirmationTokenAsync(user);
			var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

			var confirmUrl = Url.ActionLink(nameof(ConfirmEmail), "Auth", new { userId = user.Id, token = encoded }, Request.Scheme, Request.Host.ToString());
			await _email.SendEmailAsync(user.Email!, "請完成信箱驗證", $@"請點擊以下連結完成驗證：<a href=""{confirmUrl}"">{confirmUrl}</a>");

			return Ok(new { ok = true });
		}
		public record EmailDto(string Email);

		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpGet("2fa/setup")]
		public async Task<IActionResult> TwoFaSetup()
		{
			var user = await _userMgr.GetUserAsync(User);
			if (user == null) return Unauthorized();

			var key = await _userMgr.GetAuthenticatorKeyAsync(user);
			if (string.IsNullOrEmpty(key))
			{
				await _userMgr.ResetAuthenticatorKeyAsync(user);
				key = await _userMgr.GetAuthenticatorKeyAsync(user);
				
			}
			var secret = Uri.EscapeDataString(key);
			var email = user.Email ?? user.UserName ?? user.Id;
			var issuer = "tHerd"; // 與前端 issuerHint 一致
			string uri = $"otpauth://totp/{UrlEncoder.Default.Encode(issuer)}:{UrlEncoder.Default.Encode(email)}?secret={secret}&issuer={UrlEncoder.Default.Encode(issuer)}&digits=6";

			return Ok(new { sharedKey = key, authenticatorUri = uri });
		}

		public record TwoFaEnableDto(string Code);

		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpPost("2fa/enable")]
		public async Task<IActionResult> TwoFaEnable([FromBody] TwoFaEnableDto dto)
		{
			var user = await _userMgr.GetUserAsync(User);
			if (user == null) return Unauthorized();
			if (string.IsNullOrWhiteSpace(dto?.Code)) return BadRequest(new { error = "請輸入驗證碼" });

			var code = dto.Code.Replace(" ", "").Replace("-", "");
			var isValid = await _userMgr.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code);
			if (!isValid) return BadRequest(new { error = "驗證碼錯誤，請重試" });

			await _userMgr.SetTwoFactorEnabledAsync(user, true);
			var recoveryCodes = await _userMgr.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
			return Ok(new { ok = true, recoveryCodes });
		}

		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpPost("2fa/disable")]
		public async Task<IActionResult> TwoFaDisable()
		{
			var user = await _userMgr.GetUserAsync(User);
			if (user == null) return Unauthorized();
			await _userMgr.SetTwoFactorEnabledAsync(user, false);
			return Ok(new { ok = true });
		}

		public record TwoFaVerifyDto(string userIdOrEmail, string code, bool rememberMe);

		

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