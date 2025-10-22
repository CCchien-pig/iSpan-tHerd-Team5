using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.Abstractions.Security
{
	public record RefreshTokenInfo(
		long Id,
		string UserId,
		string JwtId,
		DateTime ExpiresAtUtc,
		DateTime? RevokedAtUtc,
		long? ReplacedByTokenId
	);
}
