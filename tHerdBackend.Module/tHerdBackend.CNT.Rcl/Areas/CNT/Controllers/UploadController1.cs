using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[NonController]
	[Area("CNT")]
    [Route("CNT/upload")]   // 路徑會是 /CNT/upload/image、/CNT/upload/media
    public class UploadController1 : Controller
    {
        private readonly IWebHostEnvironment _env;

        public UploadController1(IWebHostEnvironment env)
        {
            _env = env;
        }

        // ✅ 圖片上傳
        [HttpPost("image")]
        // [IgnoreAntiforgeryToken] // 如果前端沒帶 token，可以先放開，正式上線建議驗證
        public async Task<IActionResult> Image(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file" });

            // 簡單副檔名白名單
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Invalid image type" });

			// 存到 wwwroot/uploads/images
			var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");
			Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);
            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

			// 回傳完整網址
			var request = HttpContext.Request;
			var baseUrl = $"{request.Scheme}://{request.Host}";
			var url = $"{baseUrl}/uploads/images/{fileName}";

			return Ok(new { location = url });
		}

		// ✅ 影片上傳
		[HttpPost("media")]
        // [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Media(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file" });

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			var allowed = new[] { ".mp4", ".webm", ".ogg" };
			if (!allowed.Contains(ext))
				return BadRequest(new { message = "Invalid video type" });

			var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "media");
			Directory.CreateDirectory(uploadsRoot);

			var fileName = $"{Guid.NewGuid():N}{ext}";
			var fullPath = Path.Combine(uploadsRoot, fileName);
			using (var stream = System.IO.File.Create(fullPath))
			{
				await file.CopyToAsync(stream);
			}

			var request = HttpContext.Request;
			var baseUrl = $"{request.Scheme}://{request.Host}";
			var url = $"{baseUrl}/uploads/media/{fileName}";

			return Ok(new { location = url });
		}
    }
}
