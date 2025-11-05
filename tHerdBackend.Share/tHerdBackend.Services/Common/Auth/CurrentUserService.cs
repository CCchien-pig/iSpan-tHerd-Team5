using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.Common
{
	public class CurrentUserService : ICurrentUserService
	{
		public const string ClaimUserNumberId1 = "user_number_id";
		public const string ClaimUserNumberId2 = "userNumberId"; // 備援命名

		private readonly IHttpContextAccessor _http;
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly ApplicationDbContext _appDb;
		private readonly ILogger<CurrentUserService> _logger;

		private int? _cachedUserNumberId;
		private ApplicationUser? _cachedUser;
		private UserDetailDto? _cachedDetail;

		public CurrentUserService(
			IHttpContextAccessor http,
			UserManager<ApplicationUser> userMgr,
			ApplicationDbContext appDb,
			ILogger<CurrentUserService> logger)
		{
			_http = http;
			_userMgr = userMgr;
			_appDb = appDb;
			_logger = logger;
		}

		public ClaimsPrincipal? Principal => _http.HttpContext?.User;

		public string? TryGetUserId()
		{
			var u = Principal;
			if (u == null || !(u.Identity?.IsAuthenticated ?? false)) return null;
			return _userMgr.GetUserId(u);
		}

		public int? TryGetUserNumberId()
		{
			if (_cachedUserNumberId.HasValue) return _cachedUserNumberId;

			var u = Principal;
			if (u == null || !(u.Identity?.IsAuthenticated ?? false)) return null;

			var raw = u.FindFirstValue(ClaimUserNumberId1)
				   ?? u.FindFirstValue(ClaimUserNumberId2);

			if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var n) && n > 0)
			{
				_cachedUserNumberId = n;
				return n;
			}
			return null;
		}

		public string GetRequiredUserId()
		{
			return TryGetUserId() ?? throw new UnauthorizedAccessException("未登入");
		}

		public int GetRequiredUserNumberId()
		{
			var n = TryGetUserNumberId();
			if (n is null || n.Value <= 0)
				throw new UnauthorizedAccessException("未登入或缺少 userNumberId");
			return n.Value;
		}

		public async Task<ApplicationUser> GetRequiredUserAsync(CancellationToken ct = default)
		{
			if (_cachedUser is not null) return _cachedUser;

			var userId = GetRequiredUserId();
			var user = await _appDb.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
					   ?? throw new UnauthorizedAccessException("未登入");
			_cachedUser = user;

			// 可選：一致性檢查（claims 的 userNumberId 要與 DB 一致）
			var claimNum = TryGetUserNumberId();
			if (claimNum.HasValue && claimNum.Value != user.UserNumberId)
			{
				_logger.LogWarning("Token userNumberId({Claim}) 與 DB({Db}) 不一致：UserId={UserId}",
					claimNum.Value, user.UserNumberId, user.Id);
				// 視需求：改成 throw new UnauthorizedAccessException("Token 資訊與資料庫不一致");
			}

			_cachedUserNumberId ??= user.UserNumberId;
			return user;
		}

		public async Task<UserDetailDto> GetRequiredUserDetailAsync(CancellationToken ct = default)
		{
			if (_cachedDetail is not null) return _cachedDetail;

			var userId = GetRequiredUserId();

			var dto = await _appDb.Users.AsNoTracking()
				.Where(x => x.Id == userId)
				.Select(x => new UserDetailDto
				{
					Id = x.Id,
					UserNumberId = x.UserNumberId,
					Email = x.Email!,
					LastName = x.LastName,
					FirstName = x.FirstName,
					Name = x.LastName + x.FirstName,
					ImgId = x.ImgId,
					Gender = x.Gender,
					BirthDate = x.BirthDate,
					Address = x.Address,
					MemberRankId = x.MemberRankId,
					ReferralCode = x.ReferralCode,
					UsedReferralCode = x.UsedReferralCode,
					CreatedDate = x.CreatedDate,
					RevisedDate = x.RevisedDate,
					LastLoginDate = x.LastLoginDate,
					IsActive = x.IsActive,
					ActivationDate = x.ActivationDate,
					PhoneNumber = x.PhoneNumber,
					EmailConfirmed = x.EmailConfirmed,
					TwoFactorEnabled = x.TwoFactorEnabled
				})
				.FirstOrDefaultAsync(ct)
				?? throw new KeyNotFoundException("找不到使用者");

			// 可選一致性檢查
			var claimNum = TryGetUserNumberId();
			if (claimNum.HasValue && claimNum.Value != dto.UserNumberId)
			{
				_logger.LogWarning("Token userNumberId({Claim}) 與 UserDetailDto({Db}) 不一致：UserId={UserId}",
					claimNum.Value, dto.UserNumberId, dto.Id);
			}

			_cachedDetail = dto;
			_cachedUserNumberId ??= dto.UserNumberId;
			return dto;
		}
	}
}
