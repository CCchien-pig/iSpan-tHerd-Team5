using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.PROD.Rcl.Areas.PROD.Controllers
{
	[Area("PROD")]
	[Route("api/[area]/[controller]")]
	public class ImagesController : Controller  // 或 ControllerBase
	{

		// GET: api/PROD/Products/Image?fileName=1178_main.avif
		[HttpGet("Image")]
		public async Task<IActionResult> GetImage([FromQuery] string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return BadRequest("檔案名稱不可為空");

			// 統一根目錄 (建議寫到設定檔 appsettings.json)
			string rootPath = @"C:\Images\PROD\";

			// 組合完整路徑
			string filePath = Path.Combine(rootPath, fileName);

			if (!System.IO.File.Exists(filePath))
				return NotFound($"找不到檔案: {fileName}");

			// 讀取檔案
			var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

			// 判斷 MIME Type
			var ext = Path.GetExtension(filePath).ToLowerInvariant();
			var mime = ext switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".webp" => "image/webp",
				".avif" => "image/avif",
				_ => "application/octet-stream"
			};

			return File(bytes, mime);
		}
	}
}
