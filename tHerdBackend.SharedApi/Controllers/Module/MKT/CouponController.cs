using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using tHerdBackend.Core.DTOs.MKT;
using tHerdBackend.Core.Interfaces.MKT;  // 這邊請改成你實際的 Service namespace

namespace tHerdBackend.SharedApi.Controllers.Module.MKT
{
    [ApiController]
    [Route("api/mkt/[controller]")]
    public class CouponController : ControllerBase
    {
        private readonly IMktCouponService _couponService;

        public CouponController(IMktCouponService couponService)
        {
            _couponService = couponService;
        }

        /// <summary>
        /// 取得所有啟用中的優惠券
        /// GET /api/mkt/coupon
        /// </summary>
        [HttpGet]
        public IActionResult GetAll()
        {
            int memberId = 1001; // ⚠️ 先寫死，之後會改 JWT
            var coupons = _couponService.GetAllActiveCouponsWithMemberStatus(memberId);
            return Ok(coupons); // 直接丟 DTO 結構
        }


        /// <summary>
        /// 領取優惠券
        /// POST /api/mkt/coupon/receive
        /// </summary>
        [HttpPost("receive")]
        public IActionResult Receive([FromBody] ReceiveCouponRequest request)
        {
            try
            {
                // TODO: 實務上這裡會從 Token 拿會員ID
                int memberId = 1001;

                var success = _couponService.ReceiveCoupon(request.CouponId, memberId);

                if (!success)
                {
                    return BadRequest(new { message = "優惠券已領取或無法領取" });
                }

                return Ok(new { message = "領取成功" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Coupon receive error: " + ex);
                Console.WriteLine("🔥 Inner exception: " + ex.InnerException?.Message);

                return StatusCode(500, new
                {
                    message = "伺服器錯誤",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }

        }

        public class ReceiveCouponRequest
        {
            public int CouponId { get; set; }
        }
    }
}
