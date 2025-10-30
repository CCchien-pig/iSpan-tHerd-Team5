using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using tHerdBackend.Core.DTOs.MKT;
using tHerdBackend.Core.Interfaces.MKT;  // 這邊請改成你實際的 Service namespace
using tHerdBackend.Core.Abstractions; // 要加這行


namespace tHerdBackend.SharedApi.Controllers.Module.MKT
{
    [ApiController]
    [Route("api/mkt/[controller]")]
    [Authorize]
    public class CouponController : ControllerBase
    {
        private readonly IMktCouponService _couponService;
        private readonly ICurrentUser _me;   // ✅ 新增這行


        public CouponController(IMktCouponService couponService, ICurrentUser me)
        {
            _couponService = couponService;
            _me = me;
        }

        /// <summary>
        /// 取得所有啟用中的優惠券
        /// GET /api/mkt/coupon
        /// </summary>
        [Authorize] // ✅ 要求登入
        [HttpGet]
        public IActionResult GetAll()
        {
            int memberId = _me.UserNumberId;  // 🔥 從 JWT 抓會員ID
            var coupons = _couponService.GetAllActiveCouponsWithMemberStatus(memberId);
            return Ok(coupons);
        }



        /// <summary>
        /// 領取優惠券
        /// POST /api/mkt/coupon/receive
        /// </summary>

        [Authorize] // ✅ 確保只能登入會員呼叫
        [HttpPost("receive")]
        public IActionResult Receive([FromBody] ReceiveCouponRequest request)
        {
            try
            {
                int memberId = _me.UserNumberId; // 🔥 從登入者 Token 抓 UserNumberId（int）

                var success = _couponService.ReceiveCoupon(request.CouponId, memberId);
                if (!success)
                    return BadRequest(new { message = "優惠券已領取或無法領取" });

                return Ok(new { message = "領取成功" });
            }
            catch (Exception ex)
            {
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
