using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tHerdBackend.Core.Abstractions.Security;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;
using tHerdBackend.Services.Common;

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

		public AuthController(
			SignInManager<ApplicationUser> signInMgr,
			UserManager<ApplicationUser> userMgr,
			IJwtTokenService jwt,
			IRefreshTokenService refreshSvc)
		{
			_signInMgr = signInMgr;
			_userMgr = userMgr;
			_jwt = jwt;
			_refreshSvc = refreshSvc;
		}

		[AllowAnonymous]
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDto dto)
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

			var (refreshPlain, refreshEntity) = await _refreshSvc.IssueAsync(user, jti); // 新增 refresh
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

			// 1) 驗證舊 refresh token（含過期/撤銷/重放偵測）
			var (user, current) = await _refreshSvc.ValidateAsync(dto.RefreshToken);

			// 2) 發新的 access token（產生新 jti）
			var roles = await _userMgr.GetRolesAsync(user);
			var (accessToken, accessExpiresUtc, newJti) = _jwt.Generate(user, roles);

			// 3) 旋轉 refresh（撤銷舊 + 建立新）
			var rotated = await _refreshSvc.RotateAsync(current, newJti, user);

			// 取出新 refresh 明文（我放在 TokenHash 的特殊標記內）
			var newRefreshPlain = rotated.TokenHash.StartsWith("__PLAIN__:")
				? rotated.TokenHash["__PLAIN__:".Length..]
				: throw new InvalidOperationException("旋轉 refresh 失敗");

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

			var (_, current) = await _refreshSvc.ValidateAsync(dto.RefreshToken);
			await _refreshSvc.RevokeAsync(current, "user logout");

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
	}
}