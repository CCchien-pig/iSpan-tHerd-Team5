using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.Abstractions.Security;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.Common.Auth
{
	public class RefreshTokenService : IRefreshTokenService
	{
		private readonly ApplicationDbContext _db;

		public RefreshTokenService(ApplicationDbContext db)
		{
			_db = db;
		}

		public async Task<(string plainToken, RefreshTokenInfo saved)> IssueAsync(
			string userId, string jwtId, TimeSpan? lifetime = null)
		{
			var plain = GenerateSecureRandom(64);
			var hash = Sha256(plain);

			var entity = new RefreshToken
			{
				UserId = userId,
				TokenHash = hash,
				JwtId = jwtId,
				ExpiresAtUtc = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromDays(14))
			};

			_db.RefreshTokens.Add(entity);
			await _db.SaveChangesAsync();

			return (plain, ToInfo(entity));
		}

		public async Task<(string userId, RefreshTokenInfo current)> ValidateAsync(string refreshTokenPlain)
		{
			var hash = Sha256(refreshTokenPlain);
			var now = DateTime.UtcNow;

			var entity = await _db.RefreshTokens.AsTracking()
				.FirstOrDefaultAsync(x => x.TokenHash == hash)
				?? throw new SecurityTokenException("Refresh token 不存在");

			if (entity.RevokedAtUtc.HasValue)
				throw new SecurityTokenException("Refresh token 已撤銷（疑似重放）");

			if (entity.ExpiresAtUtc < now)
				throw new SecurityTokenException("Refresh token 已過期");

			return (entity.UserId, ToInfo(entity));
		}

		public async Task<(string plainToken, RefreshTokenInfo saved)> RotateAsync(
			RefreshTokenInfo current, string newJwtId, string userId, TimeSpan? lifetime = null)
		{
			var entity = await _db.RefreshTokens.FindAsync(current.Id)
				?? throw new InvalidOperationException("Refresh token 不存在");

			// 撤銷舊 token
			entity.RevokedAtUtc = DateTime.UtcNow;

			// 建新 token
			var (plain, saved) = await IssueAsync(userId, newJwtId, lifetime);

			// chain 關聯
			entity.ReplacedByTokenId = saved.Id;
			await _db.SaveChangesAsync();

			return (plain, saved);
		}

		public async Task RevokeAsync(long tokenId, string reason = "revoked")
		{
			var entity = await _db.RefreshTokens.FindAsync(tokenId);
			if (entity is null) return;
			if (!entity.RevokedAtUtc.HasValue)
			{
				entity.RevokedAtUtc = DateTime.UtcNow;
				await _db.SaveChangesAsync();
			}
		}

		// ===== helpers =====
		private static string GenerateSecureRandom(int bytes)
		{
			var buffer = RandomNumberGenerator.GetBytes(bytes);
			return Convert.ToBase64String(buffer);
		}

		private static string Sha256(string input)
		{
			using var sha = SHA256.Create();
			var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
			return Convert.ToHexString(bytes);
		}

		private static RefreshTokenInfo ToInfo(RefreshToken e) =>
			new(e.Id, e.UserId, e.JwtId, e.ExpiresAtUtc, e.RevokedAtUtc, e.ReplacedByTokenId);
	}
}
