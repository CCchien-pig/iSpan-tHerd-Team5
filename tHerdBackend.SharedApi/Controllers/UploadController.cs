using tHerdBackend.Core.Interfaces.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.SharedApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IImageStorage _imageStorage;

        public UploadController(IImageStorage imageStorage)
        {
            _imageStorage = imageStorage;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var url = await _imageStorage.UploadImageAsync(file, "uploads");
            return Ok(new { imageUrl = url });
        }
    }
}
