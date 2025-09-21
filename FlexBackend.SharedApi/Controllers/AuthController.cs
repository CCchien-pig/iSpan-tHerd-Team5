using FlexBackend.Core.DTOs.USER;
using FlexBackend.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.SharedApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;           // 負責產 token（我們做好的）
        private readonly UserManager<ApplicationUser> _users; // 若你用 Identity

        public AuthController(AuthService auth, UserManager<ApplicationUser> users)
        {
            _auth = auth;
            _users = users;
        }

        [AllowAnonymous]                               // 重要：拿 token 不需要先登入
        [HttpPost("token")]
        public async Task<IActionResult> GetToken([FromBody] LoginDto dto)
        {
            // (1) DEMO 版本：純字串比對（你要用這個就要輸入這組）
            // if (dto.Email == "admin@therd.com" && dto.Password == "123456")
            // {
            //     var token = _auth.GenerateToken("1", dto.Email, "Admin");
            //     return Ok(new { token });
            // }
            // return Unauthorized();

            // (2) Identity 版本（建議）
            var user = await _users.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized();

            var ok = await _users.CheckPasswordAsync(user, dto.Password);
            if (!ok) return Unauthorized();

            // 也可以把使用者角色查出來塞進 claims
            var token = _auth.GenerateToken(user.Id, user.Email!, "User");
            return Ok(new { token });
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}