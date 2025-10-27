using System.Collections.Generic;
using tHerdBackend.Core.DTOs.MKT;

namespace tHerdBackend.Core.Interfaces.MKT
{
    public interface IMktCouponService
    {
        /// <summary>
        /// 取得所有啟用中的優惠券
        /// </summary>
        List<MktCouponDto> GetAllActiveCoupons();

        /// <summary>
        /// 取得所有啟用中的優惠券，並包含會員是否已領取
        /// </summary>
        List<MktCouponDto> GetAllActiveCouponsWithMemberStatus(int memberId);

        /// <summary>
        /// 領取優惠券
        /// </summary>
        bool ReceiveCoupon(int couponId, int memberId);
    }
}
