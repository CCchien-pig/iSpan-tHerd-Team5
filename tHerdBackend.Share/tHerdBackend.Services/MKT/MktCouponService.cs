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
            // 1️⃣ 檢查優惠券是否存在且啟用
            var coupon = _db.MktCoupons.FirstOrDefault(c => c.CouponId == couponId && c.IsActive);
            if (coupon == null) return false;

            // 2️⃣ 剩餘數量不足
            if (coupon.LeftQty <= 0) return false;

            // 3️⃣ 檢查會員是否已領取過、或達上限
            var receiveCount = _db.UserCouponWallets
                .Count(w => w.CouponId == couponId && w.UserNumberId == memberId);
            if (receiveCount >= coupon.UserLimit)
                return false;

            // 4️⃣ 建立領券紀錄
            var wallet = new UserCouponWallet
            {
                CouponId = coupon.CouponId,
                UserNumberId = memberId,
                ClaimedDate = DateTime.Now,
                Status = "unuse"
            };

            _db.UserCouponWallets.Add(wallet);

            // 5️⃣ 扣除剩餘數量
            coupon.LeftQty -= 1;

            // 6️⃣ 寫入資料庫
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
                    // ✅ 加入判斷會員是否已領取
                    IsReceived = _db.UserCouponWallets
                        .Any(w => w.CouponId == c.CouponId && w.UserNumberId == memberId)
                })
                .OrderByDescending(c => c.CreatedDate)
                .ToList();

            return coupons;
        }


    }
}
