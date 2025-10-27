using System.Security.Claims;
using tHerdBackend.Core.Abstractions;

namespace tHerdBackend.SharedApi.Infrastructure.Auth
{
    /// <summary>
    /// 前台版本的 CurrentUser，不依賴 Identity，只支援 JWT。
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _http;

        public CurrentUser(IHttpContextAccessor http)
        {
            _http = http;
        }

		private ClaimsPrincipal? P => _http.HttpContext?.User;
		private string? Get(string t) => P?.FindFirst(t)?.Value;

		public bool IsAuthenticated => P?.Identity?.IsAuthenticated ?? false;
		public string Id => Get("sub") ?? "0";
		public string? Email => Get("email");
		public string? FullName => Get("name");
		public int UserNumberId => int.TryParse(Get("user_number_id"), out var n) ? n : 0;
	}
}
