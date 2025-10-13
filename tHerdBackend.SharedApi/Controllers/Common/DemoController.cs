using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.SharedApi.Controllers.Common
{
    [ApiController]
    [Route("api/demo")]
    public class DemoController : ControllerBase
    {
        // 任何人都能存取
        [HttpGet("public")]
        [AllowAnonymous]
        public IActionResult PublicPage()
        {
            return Ok("這是公開頁面，不需要登入");
        }

        // 需要 JWT token 才能存取
        [HttpGet("secure")]
        [Authorize]
        public IActionResult SecurePage()
        {
            return Ok("這是受保護頁面，需要登入才能看到");
        }

        // 需要特定角色才能存取
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminPage()
        {
            return Ok("只有 Admin 角色能看到");
        }
    }
}
