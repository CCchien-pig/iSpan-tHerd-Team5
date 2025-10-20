using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[NonController]
	[Area("CNT")]
	[Route("CNT/upload")]   // /CNT/upload/image、/CNT/upload/media
	public class UploadController1 : Controller
	{
		private readonly IWebHostEnvironment _env;

		public UploadController1(IWebHostEnvironment env)
		{
			_env = env;
		}

		// 圖片上傳
		[HttpPost("image")]
		public async Task<IActionResult> Image(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { message = "No file" });

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
			if (!allowed.Contains(ext))
				return BadRequest(new { message = "Invalid image type" });

			var uploadsRoot = Path.Combine(@"C:\圖片\tHerd-Image\CNT", "images");
			Directory.CreateDirectory(uploadsRoot);

			var fileName = $"{Guid.NewGuid():N}{ext}";
			var fullPath = Path.Combine(uploadsRoot, fileName);
			using (var stream = System.IO.File.Create(fullPath))
			{
				await file.CopyToAsync(stream);
			}

			// ⚡ 回傳完整網址
			var request = HttpContext.Request;
			var baseUrl = $"{request.Scheme}://{request.Host}";
			var relativePath = Path.Combine("images", fileName).Replace("\\", "/");
			var url = $"{baseUrl}/CNT/file?id={relativePath}";

			return Ok(new { location = url });
		}

		// 影片上傳
		[HttpPost("media")]
		public async Task<IActionResult> Media(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { message = "No file" });

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			var allowed = new[] { ".mp4", ".webm", ".ogg" };
			if (!allowed.Contains(ext))
				return BadRequest(new { message = "Invalid video type" });

			var uploadsRoot = Path.Combine(@"C:\圖片\tHerd-Image\CNT", "media");
			Directory.CreateDirectory(uploadsRoot);

			var fileName = $"{Guid.NewGuid():N}{ext}";
			var fullPath = Path.Combine(uploadsRoot, fileName);
			using (var stream = System.IO.File.Create(fullPath))
			{
				await file.CopyToAsync(stream);
			}

			// ⚡ 回傳完整網址
			var request = HttpContext.Request;
			var baseUrl = $"{request.Scheme}://{request.Host}";
			var relativePath = Path.Combine("media", fileName).Replace("\\", "/");
			var url = $"{baseUrl}/CNT/file?id={relativePath}";

			return Ok(new { location = url });
		}
	}
}
