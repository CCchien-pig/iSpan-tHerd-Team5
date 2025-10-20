using tHerdBackend.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.Common;

namespace tHerdBackend.SharedApi.Controllers.Common
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;

        public AuthController(AuthService auth)
        {
            _auth = auth;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            // TODO: 這裡請改成你自己的帳號驗證邏輯（DB、Config…）
            if (dto.Email == "admin@therd.com" && dto.Password == "123456")
            {
                var token = _auth.GenerateToken("1", dto.Email, "User");
                return Ok(new { token });
            }

            return Unauthorized("帳號或密碼錯誤");
        }
    }
}