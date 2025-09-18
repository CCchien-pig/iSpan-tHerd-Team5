using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.PROD.Rcl.Areas.PROD.Controllers
{
	[Area("PROD")]
	[Route("api/[area]/[controller]")]
	public class ImagesController : Controller  // 或 ControllerBase
	{
        // GET: api/PROD/Products/Image?fileName=1178_main.avif
        //[HttpGet("Image")]
        //public async Task<IActionResult> GetImage([FromQuery] string fileName)
        //{
        //	if (string.IsNullOrWhiteSpace(fileName))
        //		return BadRequest("檔案名稱不可為空");

        //	// 統一根目錄 (建議寫到設定檔 appsettings.json)
        //	string rootPath = @"C:\Images\PROD\";

        //	// 組合完整路徑
        //	string filePath = Path.Combine(rootPath, fileName);

        //	if (!System.IO.File.Exists(filePath))
        //		return NotFound($"找不到檔案: {fileName}");

        //	// 讀取檔案
        //	var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

        //	// 判斷 MIME Type
        //	var ext = Path.GetExtension(filePath).ToLowerInvariant();
        //	var mime = ext switch
        //	{
        //		".jpg" or ".jpeg" => "image/jpeg",
        //		".png" => "image/png",
        //		".gif" => "image/gif",
        //		".webp" => "image/webp",
        //		".avif" => "image/avif",
        //		_ => "application/octet-stream"
        //	};

        //	return File(bytes, mime);
        //}

        // 模組代號（可依 Area 自動判斷或從設定檔取出）
        private readonly string _moduleCode = "PROD";

        // 統一圖片根目錄：C:/tHerd_Img/PROD
        private string RootImagePath => $@"C:\tHerd_Img\{_moduleCode}";

        /// <summary>
        /// 讀取圖片（GET）
        /// 路徑範例：/api/PROD/Images/Image?fileName=123.jpg
        /// </summary>
        [HttpGet("Image")]
        public async Task<IActionResult> GetImage([FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("檔案名稱不可為空");

            string filePath = Path.Combine(RootImagePath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound($"找不到檔案: {fileName}");

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var mime = GetMimeType(Path.GetExtension(fileName));
            return File(bytes, mime);
        }

        /// <summary>
        /// 上傳圖片（POST）會自動建立資料夾與覆蓋舊檔
        /// </summary>
        [HttpPost("Image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("未選擇檔案");

            string module = "PROD";
            string rootPath = $@"C:\tHerd_Img\{module}\";
            Directory.CreateDirectory(rootPath);

            // 自動生成新檔名
            string ext = Path.GetExtension(file.FileName); // 取副檔名

            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext.ToLower()))
                return BadRequest("不支援的圖片格式");

            string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
            string filePath = Path.Combine(rootPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { message = "圖片上傳成功", fileName = fileName, path = filePath });
        }

        /// <summary>
        /// 刪除圖片（DELETE）
        /// 路徑範例：/api/PROD/Images/Image?fileName=123.jpg
        /// </summary>
        [HttpDelete("Image")]
        public IActionResult DeleteImage([FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("檔案名稱不可為空");

            string filePath = Path.Combine(RootImagePath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("找不到指定檔案");

            System.IO.File.Delete(filePath);

            return Ok(new
            {
                message = "圖片已刪除",
                fileName
            });
        }

        /// <summary>
        /// 判斷 MIME 類型
        /// </summary>
        private string GetMimeType(string extension) =>
            extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".avif" => "image/avif",
                _ => "application/octet-stream"
            };
    }
}
