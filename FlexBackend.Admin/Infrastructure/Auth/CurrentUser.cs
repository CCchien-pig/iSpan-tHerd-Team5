using FlexBackend.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Security.Claims;

namespace FlexBackend.Admin.Infrastructure.Auth
{
	public class CurrentUser : ICurrentUser
	{
		private readonly IHttpContextAccessor _http;
		public CurrentUser(IHttpContextAccessor http) => _http = http;

		private ClaimsPrincipal? User => _http.HttpContext?.User;

		private string? Get(string type) => User?.FindFirstValue(type);

		public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
		public string? Id => Get(ClaimTypes.NameIdentifier);
		public string? Email => Get(ClaimTypes.Email);

        // 一定要有值：若 claim 缺失（理論上不會），就丟例外提示登入態需刷新
        int ICurrentUser.UserNumberId
        {
			get
			{
				var raw = Get(AppClaimTypes.UserNumberId);
				if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
					return n;
				throw new InvalidOperationException("Missing UserNumberId claim. Please refresh sign-in.");
			}
		}

		public string? FirstName => Get(AppClaimTypes.FirstName);
		public string? LastName => Get(AppClaimTypes.LastName);

		public string? FullName
		{
			get
			{
				var full = Get(AppClaimTypes.FullName);
				if (!string.IsNullOrWhiteSpace(full)) return full;

				var first = FirstName ?? string.Empty;
				var last = LastName ?? string.Empty;
				var combo = $"{last} {first}".Trim();
				return string.IsNullOrWhiteSpace(combo) ? (Email ?? User?.Identity?.Name) : combo;
			}
		}
    }
}
