using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;                 // 引用 JsonSerializer
using tHerdBackend.Core.DTOs;          // 引用 AssetFileUploadDto 和 AssetFileDetailsDto
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.Models; // 引用 ISysAssetFileService

/// <summary>
/// 接收 TinyMCE 的請求，將其轉換為您後端服務所需的格式，然後使用 JSON 解析技巧來處理回傳結果。
/// 使用 JSON 序列化/反序列化 的技巧。將 SysAssetFileRepository 回傳的未知 object 轉換為一個 JSON 字串，然後再將這個字串解析為一個我們可以安全操作的結構 (JsonDocument)
/// </summary>
[Area("SUP")]
[Route("api/assets")] // API 基礎路由
[ApiController]
public class AssetsController : ControllerBase
{
	private readonly ISysAssetFileService _assetFileService;
	private readonly tHerdDBContext _context;

	// 【核心】注入您現有的 ISysAssetFileService
	public AssetsController(ISysAssetFileService assetFileService, tHerdDBContext context)
	{
		_assetFileService = assetFileService;
		_context = context;
	}

	/// <summary>
	/// 接收 TinyMCE 上傳的圖片，存入資料庫並回傳 URL。
	/// </summary>
	/// <param name="file">TinyMCE 自動發送的圖片檔案，參數名必須是 "file"。</param>
	[HttpPost("upload-content-image")]
	[AllowAnonymous] // 暫時允許匿名上傳，未來應加入權限驗證
	public async Task<IActionResult> UploadForTinyMce(
		[FromForm] IFormFile file,
		[FromForm] string blockType,
		[FromForm] string altText,
		[FromForm] string caption,
		[FromForm] bool isActive)
	{
		if (file == null || file.Length == 0)
		{
			return BadRequest(new { error = new { message = "沒有上傳任何檔案。" } });
		}

		try
		{
			// 【核心修正點 1】根據 blockType 查詢 FolderId
			int? folderId = await _context.SysFolders
				.Where(f => f.FolderName == blockType)
				.Select(f => (int?)f.FolderId)
				.FirstOrDefaultAsync();

			// 1. 準備上傳 DTO
			var uploadDto = new AssetFileUploadDto
			{
				ModuleId = "Brand",
				//ProgId = "ArticleEditor",
				ProgId = "ContentEditor",
				FolderId = folderId, // 使用查詢到的 FolderId
				Meta = new List<AssetFileDetailsDto>
				{
					new AssetFileDetailsDto
					{
						File = file,
						// 【核心修正點 2】使用前端傳來的元數據
						AltText = altText,
						Caption = caption,
						IsActive = isActive
					}
				}
			};

			// 2. 呼叫您現有的 Service 執行上傳，接收回傳的 object
			object resultObject = await _assetFileService.AddImages(uploadDto);

			// 3. 【核心修正點】使用 JSON 序列化/反序列化技巧，安全地解析 object
			var jsonString = JsonSerializer.Serialize(resultObject);
			using var doc = JsonDocument.Parse(jsonString);
			var root = doc.RootElement;

			if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
			{
				if (root.TryGetProperty("data", out var dataElement) &&
					dataElement.TryGetProperty("files", out var filesElement) &&
					filesElement.GetArrayLength() > 0)
				{
					var firstFile = filesElement[0];

					// 【核心修正點】將 "fileUrl" 改為 "FileUrl"
					if (firstFile.TryGetProperty("FileUrl", out var fileUrlElement) &&
					firstFile.TryGetProperty("FileId", out var fileIdElement))
					{
						// 4. 回傳 location 和 fileId
						return Ok(new {
							location = fileUrlElement.GetString(),
							fileId = fileIdElement.GetInt32()
						});
					}
				}
				return BadRequest(new { error = new { message = "上傳成功，但找不到回傳的檔案 URL。" } });
			}
			else
			{
				string errorMessage = "上傳失敗";
				if (root.TryGetProperty("message", out var messageElement))
				{
					errorMessage = messageElement.GetString();
				}
				return BadRequest(new { error = new { message = errorMessage } });
			}
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = new { message = $"伺服器錯誤: {ex.Message}" } });
		}
	}
}