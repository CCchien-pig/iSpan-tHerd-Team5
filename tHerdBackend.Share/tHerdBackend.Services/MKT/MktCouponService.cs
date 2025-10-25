using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.MKT;
using tHerdBackend.Core.Interfaces.MKT;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Services.MKT
{
    public class MktCouponService : IMktCouponService
    {
        private readonly tHerdDBContext _db;

        public MktCouponService(tHerdDBContext db)
        {
            _db = db;
        }

        public List<MktCouponDto> GetAllActiveCoupons()
        {
            return _db.MktCoupons
                .Where(c => c.IsActive && c.LeftQty > 0)
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new MktCouponDto
                {
                    CouponId = c.CouponId,
                    CampaignId = c.CampaignId,
                    RuleId = c.RuleId,
                    CouponName = c.CouponName,
                    CouponCode = c.CouponCode,
                    Status = c.Status,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    DiscountAmount = c.DiscountAmount,
                    DiscountPercent = c.DiscountPercent,
                    TotQty = c.TotQty,
                    LeftQty = c.LeftQty,
                    UserLimit = c.UserLimit,
                    ValidHours = c.ValidHours,
                    IsActive = c.IsActive,
                    Creator = c.Creator,
                    CreatedDate = c.CreatedDate
                })
                .ToList();
        }

        public bool ReceiveCoupon(int couponId, int memberId)
        {
            // ✅ 撈優惠券
            var coupon = _db.MktCoupons.FirstOrDefault(c => c.CouponId == couponId && c.IsActive);
            if (coupon == null) return false;

            // ✅ 檢查庫存
            if (coupon.LeftQty <= 0) return false;

            // ✅ 檢查是否超過領取上限
            var receiveCount = _db.UserCouponWallets
                .Count(w => w.CouponId == couponId && w.UserNumberId == memberId);

            if (receiveCount >= coupon.UserLimit)
                return false;

            // ✅ 建立領券紀錄
            var wallet = new UserCouponWallet
            {
                CouponId = coupon.CouponId,
                UserNumberId = memberId,
                ClaimedDate = DateTime.Now,
                Status = "未使用"
            };

            _db.UserCouponWallets.Add(wallet);

            // ✅ 扣庫存
            coupon.LeftQty -= 1;

            _db.SaveChanges();
            return true;
        }
        public List<MktCouponDto> GetAllActiveCouponsWithMemberStatus(int memberId)
        {
            var today = DateTime.Now;

            var coupons = _db.MktCoupons
                .Where(c =>
                    c.IsActive &&
                    c.LeftQty > 0 &&
                    // ✅ 新增有效期限判斷
                    c.StartDate <= today &&
                    (c.EndDate == null || c.EndDate >= today)
                )
                .Select(c => new MktCouponDto
                {
                    CouponId = c.CouponId,
                    CampaignId = c.CampaignId,
                    RuleId = c.RuleId,
                    CouponName = c.CouponName,
                    CouponCode = c.CouponCode,
                    Status = c.Status,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    DiscountAmount = c.DiscountAmount,
                    DiscountPercent = c.DiscountPercent,
                    TotQty = c.TotQty,
                    LeftQty = c.LeftQty,
                    UserLimit = c.UserLimit,
                    ValidHours = c.ValidHours,
                    IsActive = c.IsActive,
                    Creator = c.Creator,
                    CreatedDate = c.CreatedDate,
                    IsReceived = _db.UserCouponWallets
                        .Any(w => w.CouponId == c.CouponId && w.UserNumberId == memberId)
                })
                .OrderByDescending(c => c.CreatedDate)
                .ToList();

            return coupons;
        }

    }
}
