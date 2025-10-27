using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.USER;

namespace tHerdBackend.Infra.Models
{
	public class RefreshToken
	{
		[Key]
		public long Id { get; set; }

		[Required]
		public string UserId { get; set; } = default!;         // AspNetUsers.Id

		[Required]
		public string TokenHash { get; set; } = default!;      // 儲存雜湊，不存明文

		[Required]
		public DateTime ExpiresAtUtc { get; set; }             // 到期時間（UTC）

		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

		public DateTime? RevokedAtUtc { get; set; }            // 被撤銷時間（旋轉或登出）

		public long? ReplacedByTokenId { get; set; }           // 旋轉後指向新 token

		[Required]
		public string JwtId { get; set; } = default!;          // access token 的 jti（用於重放/關聯）

		[ForeignKey(nameof(UserId))]
		public ApplicationUser? User { get; set; }
	}
}
