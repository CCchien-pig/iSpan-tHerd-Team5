using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
    [ApiController]
    [Route("api/ord/cart")]   // ✅ 小寫，與前端一致
    [Produces("application/json")]
    [AllowAnonymous]          // ✅ 暫時關閉驗證
    public class CartController : ControllerBase
    {
        private readonly tHerdDBContext _context;

        public CartController(tHerdDBContext context)
        {
            _context = context;
        }

        // ✅ 簡易測試用
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { success = true, message = "✅ Cart API is working" });
        }

        // ✅ 模擬結帳邏輯（能正常接收 JSON）
        [HttpPost("checkout")]
        public IActionResult Checkout([FromBody] CheckoutRequest request)
        {
            // 檢查模型
            if (request == null)
                return BadRequest(new { success = false, message = "❌ 請傳入有效 JSON" });

            if (request.CartItems == null || !request.CartItems.Any())
                return BadRequest(new { success = false, message = "❌ 購物車是空的" });

            // ✅ 模擬計算總價
            decimal subtotal = request.CartItems.Sum(i => i.SalePrice * i.Quantity);
            decimal discount = request.DiscountAmount ?? 0;
            decimal total = subtotal - discount;

            // ✅ 成功回傳
            return Ok(new
            {
                success = true,
                message = "✅ 結帳成功（測試用）",
                data = new
                {
                    orderNo = $"ORD{DateTime.Now:yyyyMMddHHmmss}",
                    subtotal,
                    discount,
                    total,
                    coupon = request.CouponCode ?? "(無優惠券)"
                }
            });
        }
    }

    // ✅ 模型設計
    public class CheckoutRequest
    {
        public string SessionId { get; set; }
        public int? UserNumberId { get; set; }
        public List<CartItemRequest> CartItems { get; set; }

        [BindRequired(false)]           // ✅ 可為 null
        public string CouponCode { get; set; }

        public decimal? DiscountAmount { get; set; }
    }

    public class CartItemRequest
    {
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public string ProductName { get; set; }
        public string OptionName { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
    }
}
