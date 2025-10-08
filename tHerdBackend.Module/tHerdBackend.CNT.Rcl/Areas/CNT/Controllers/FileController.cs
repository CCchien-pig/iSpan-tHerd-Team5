using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[Area("CNT")]
	[Route("CNT/file")]
	public class FileController : Controller
	{
		private readonly string _basePath = @"C:\圖片\tHerd-Image\CNT";

		[HttpGet]
		public IActionResult Get(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return BadRequest("缺少檔案 id");

			// 防呆 1: 禁止跳脫目錄
			if (id.Contains(".."))
				return BadRequest("非法路徑");

			// 組出完整路徑
			var fullPath = Path.Combine(_basePath, id);

			// 防呆 2: 確保仍在 _basePath 內
			var normalizedBase = Path.GetFullPath(_basePath);
			var normalizedPath = Path.GetFullPath(fullPath);

			if (!normalizedPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
				return BadRequest("非法路徑");

			if (!System.IO.File.Exists(normalizedPath))
				return NotFound("找不到檔案");

			if (!System.IO.File.Exists(normalizedPath))
			{
				var placeholder = Path.Combine(_basePath, "placeholder.png");
				if (System.IO.File.Exists(placeholder))
				{
					return PhysicalFile(placeholder, "image/png");
				}
				return NotFound("找不到檔案");
			}

			// 判斷 MIME type
			var ext = Path.GetExtension(normalizedPath).ToLowerInvariant();
			var contentType = ext switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".webp" => "image/webp",
				".mp4" => "video/mp4",
				".webm" => "video/webm",
				".ogg" => "video/ogg",
				_ => "application/octet-stream"
			};

			var fileBytes = System.IO.File.ReadAllBytes(normalizedPath);
			return File(fileBytes, contentType);
		}
	}
}
