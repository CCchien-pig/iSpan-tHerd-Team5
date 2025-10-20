using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
    [ApiController]
    [Route("api/ord/cart")]   // ✅ 全小寫，與前端完全一致
    [Produces("application/json")]
    [AllowAnonymous]          // ✅ 暫時關閉授權檢查，確保不被攔截
    public class CartController : ControllerBase
    {
        private readonly tHerdDBContext _context;

        public CartController(tHerdDBContext context)
        {
            _context = context;
        }

        [HttpPost("checkout")]
        public IActionResult Checkout([FromBody] CheckoutRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "❌ 後端收到 null，請確認 Content-Type 與 JSON 格式"
                });
            }

            return Ok(new
            {
                success = true,
                message = "✅ Checkout API 成功接收資料！",
                request
            });
        }
    }

    public class CheckoutRequest
    {
        public string SessionId { get; set; }
        public int? UserNumberId { get; set; }
        public List<CartItemRequest> CartItems { get; set; }
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
