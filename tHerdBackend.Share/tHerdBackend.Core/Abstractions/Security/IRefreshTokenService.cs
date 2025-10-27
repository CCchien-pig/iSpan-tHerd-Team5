using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.Abstractions.Security;

namespace tHerdBackend.Core.Abstractions.Security
{
	public interface IRefreshTokenService
	{
		// 建立 refresh：回傳「明文 token」(給前端) 與 保存後的資訊
		Task<(string plainToken, RefreshTokenInfo saved)> IssueAsync(
			string userId, string jwtId, TimeSpan? lifetime = null);

		// 由明文 refresh 驗證有效性：回傳 userId 與目前這顆 token 的資訊
		Task<(string userId, RefreshTokenInfo current)> ValidateAsync(string refreshTokenPlain);

		// 旋轉：撤銷 current + 產生新 refresh，回傳新明文與保存後資訊
		Task<(string plainToken, RefreshTokenInfo saved)> RotateAsync(
			RefreshTokenInfo current, string newJwtId, string userId, TimeSpan? lifetime = null);

		// 撤銷：用 tokenId（或你要改用 Hash/明文都可以）
		Task RevokeAsync(long tokenId, string reason = "revoked");
	}
}
