using System.Security.Claims;
using tHerdBackend.Core.DTOs.USER;

namespace tHerdBackend.Services.Common
{
	public interface ICurrentUserService
	{
		ClaimsPrincipal? Principal { get; }

		// Claims 讀取（不丟例外）
		string? TryGetUserId();
		int? TryGetUserNumberId();

		// Claims 讀取（缺少就丟 401）
		string GetRequiredUserId();
		int GetRequiredUserNumberId();

		// 只有需要 Identity 實體/明細時才查 ApplicationDbContext（Scoped 內快取）
		Task<ApplicationUser> GetRequiredUserAsync(CancellationToken ct = default);
		Task<UserDetailDto> GetRequiredUserDetailAsync(CancellationToken ct = default);
	}
}
