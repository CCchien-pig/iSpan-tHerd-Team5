using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;          // ApplicationDbContext, ApplicationUser, tHerdDBContext

namespace tHerdBackend.SharedApi.Controllers.Module.MKT
{
	[ApiController]
	[Route("api/mkt/referral")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public class MktReferralController : ControllerBase
	{
		private readonly ApplicationDbContext _appDb;
		private readonly tHerdDBContext _herdDb;
		private readonly IConfiguration _cfg;
		private readonly UserManager<ApplicationUser> _userMgr;

		public MktReferralController(
			ApplicationDbContext appDb,
			tHerdDBContext herdDb,
			IConfiguration cfg,
			UserManager<ApplicationUser> userMgr)
		{
			_appDb = appDb;
			_herdDb = herdDb;
			_cfg = cfg;
			_userMgr = userMgr;
		}

		public record ClaimReferralDto([Required] string Code);

		[HttpPost("claim")]
		public async Task<IActionResult> ClaimReferral([FromBody] ClaimReferralDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(new { error = "缺少必要參數" });

			// 1) 取目前使用者 & UserNumberId
			var userId = _userMgr.GetUserId(User);
			if (string.IsNullOrEmpty(userId))
				return Unauthorized(new { error = "未登入" });

			var me = await _appDb.Users.AsNoTracking()
				.Where(u => u.Id == userId)
				.Select(u => new { u.UserNumberId, u.UsedReferralCode })
				.FirstOrDefaultAsync();

			if (me is null || me.UserNumberId == 0)
				return NotFound(new { error = "找不到使用者或 UserNumberId 無效" });

			// 2) 驗證此使用者確實「有使用過」此推薦碼
			if (string.IsNullOrWhiteSpace(me.UsedReferralCode) ||
				!string.Equals(me.UsedReferralCode.Trim(), dto.Code.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return BadRequest(new { error = "您尚未綁定此推薦碼，無法領取" });
			}

			// 3) 取推薦回饋券的券碼（建議設定）
			var inviteCouponCode = _cfg["MKT:Referral:InviteCouponCode"] ?? "INV800";

			// 4) 讀取可用的券
			var now = DateTime.UtcNow;
			var coupon = await _herdDb.MktCoupons
				.Where(c =>
					c.CouponCode == inviteCouponCode &&
					c.IsActive == true &&
					c.Status == "pActive" &&
					c.StartDate <= now &&
					(c.EndDate == null || c.EndDate >= now))
				.FirstOrDefaultAsync();

			if (coupon == null)
				return BadRequest(new { error = "推薦回饋優惠券尚未開放或已下架" });

			// 5) 檢查該使用者是否已達上限 / 是否已領取
			var myClaimCount = await _herdDb.UserCouponWallets
				.CountAsync(w => w.UserNumberId == me.UserNumberId && w.CouponId == coupon.CouponId);

			if (myClaimCount >= coupon.UserLimit)
				return BadRequest(new { error = "已達此券領取上限" });

			// 6) 交易：先扣 LeftQty，再插入 Wallet（確保 LeftQty>0 時才能成功）
			using var tx = await _herdDb.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

			// 6.1) 嘗試扣庫存（LeftQty > 0）
#if NET7_0_OR_GREATER
			var updated = await _herdDb.MktCoupons
				.Where(c => c.CouponId == coupon.CouponId && c.LeftQty > 0)
				.ExecuteUpdateAsync(s => s.SetProperty(c => c.LeftQty, c => c.LeftQty - 1));
#else
            var target = await _herdDb.MktCoupons
                .Where(c => c.CouponId == coupon.CouponId)
                .FirstOrDefaultAsync();
            if (target == null || target.LeftQty <= 0)
                return BadRequest(new { error = "優惠券已被領完" });
            target.LeftQty -= 1;
            var updated = await _herdDb.SaveChangesAsync();
#endif
			if (updated <= 0)
			{
				await tx.RollbackAsync();
				return BadRequest(new { error = "優惠券已被領完" });
			}

			// 6.2) 插入錢包
			var wallet = new UserCouponWallet
			{
				UserNumberId = me.UserNumberId,
				CouponId = coupon.CouponId,
				ClaimedDate = DateTime.UtcNow,
				Status = "unuse"
			};
			_herdDb.UserCouponWallets.Add(wallet);
			await _herdDb.SaveChangesAsync();

			await tx.CommitAsync();

			// 7) 回傳（最小欄位；如需 CouponName 一併回）
			return Ok(new
			{
				couponWalletId = wallet.CouponWalletId,
				couponId = coupon.CouponId,
				couponName = coupon.CouponName,
				status = wallet.Status,
				claimedAt = wallet.ClaimedDate
			});
		}
	}
}
