using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Abstractions.Security;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SharedApi.Controllers.Common
{
	[ApiController]
	[Route("api/auth")]
	public class DevAuthController : ControllerBase
	{
		private readonly IWebHostEnvironment _env;
		private readonly IConfiguration _config;
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly IJwtTokenService _jwt;
		private readonly IRefreshTokenService _refresh;

		public DevAuthController(
			IWebHostEnvironment env,
			IConfiguration config,
			UserManager<ApplicationUser> userMgr,
			IJwtTokenService jwt,
			IRefreshTokenService refresh)
		{
			_env = env;
			_config = config;
			_userMgr = userMgr;
			_jwt = jwt;
			_refresh = refresh;
		}

		/// <summary>
		/// 只在 Development 啟用：用固定測試帳號直接簽發 access/refresh。請勿上線。
		/// </summary>
		[HttpPost("dev-token")]
		[AllowAnonymous]
		public async Task<IActionResult> DevToken()
		{
			// 嚴格限制：僅 Development 或旗標開啟時可用
			var enabled = _config.GetValue<bool>("DevAuth:Enabled");
			if (!_env.IsDevelopment() && !enabled)
				return NotFound(); // 避免被誤用，直接偽裝不存在

			var email = _config["DevAuth:Email"];
			if (string.IsNullOrWhiteSpace(email))
				return BadRequest(new { error = "DevAuth:Email 未設定" });

			var user = await _userMgr.FindByEmailAsync(email);
			if (user == null || !user.IsActive)
				return BadRequest(new { error = "測試帳號不存在或已停用" });

			var roles = await _userMgr.GetRolesAsync(user);
			var (accessToken, accessExpiresUtc, jti) = _jwt.Generate(user, roles);

			// 可選：同時發 refresh，方便長測
			var (refreshPlain, _) = await _refresh.IssueAsync(user.Id, jti);

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
	}
}
