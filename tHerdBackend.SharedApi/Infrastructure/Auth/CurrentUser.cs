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

        private string? GetClaim(string type)
        {
            return _http.HttpContext?.User?.FindFirst(type)?.Value;
        }

        public bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public string Id => GetClaim("sub") ?? "0"; // JWT 的 subject (sub)
        public string? Email => GetClaim("email");
        public string? FullName => GetClaim("name");

        // 因為前台沒有 Identity，不會有 UserNumberId，固定回傳 0。
        public int UserNumberId => 0;
    }
}
