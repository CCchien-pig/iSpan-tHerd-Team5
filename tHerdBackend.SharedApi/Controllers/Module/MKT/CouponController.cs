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

        [Authorize]
        [HttpPost("receive")]
        public IActionResult Receive([FromBody] ReceiveCouponRequest request)
        {
            if (!_me.IsAuthenticated || _me.UserNumberId <= 0)
            {
                return Unauthorized(new { message = "請先登入會員再領取優惠券" });
            }

            try
            {
                int memberId = _me.UserNumberId;

                var success = _couponService.ReceiveCoupon(request.CouponId, memberId);
                if (!success)
                    return BadRequest(new { message = "優惠券已領取或無法領取" });

                return Ok(new { message = "領取成功" });
            }
            catch (Exception ex)
            {
                // 🔍 詳細錯誤輸出
                return StatusCode(500, new
                {
                    message = "伺服器錯誤",
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace,
                    user = new
                    {
                        _me.IsAuthenticated,
                        _me.UserNumberId,
                        
                    }
                });
            }
        }




        public class ReceiveCouponRequest
        {
            public int CouponId { get; set; }
        }
    }
}
