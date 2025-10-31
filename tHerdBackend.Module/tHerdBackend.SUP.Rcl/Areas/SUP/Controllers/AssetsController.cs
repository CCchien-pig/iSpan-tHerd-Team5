using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;                 // 引用 JsonSerializer
using tHerdBackend.Core.DTOs;          // 引用 AssetFileUploadDto 和 AssetFileDetailsDto
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.DTOs.SYS;
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
	private readonly IHttpClientFactory _httpClientFactory; // 【新增】用於發送 HTTP 請求

	// 【核心】注入您現有的 ISysAssetFileService
	public AssetsController(ISysAssetFileService assetFileService, tHerdDBContext context, IHttpClientFactory httpClientFactory)
	{
		_assetFileService = assetFileService;
		_context = context;
		_httpClientFactory = httpClientFactory;
	}

	#region === 檔案上傳 (Upload) ===

	/// <summary>
	/// 接收 TinyMCE 上傳的圖片，存入資料庫並回傳 URL。
	/// </summary>
	/// <param name="file">TinyMCE 自動發送的圖片檔案，參數名必須是 "file"。</param>
	[HttpPost("upload-content-image")]
	[AllowAnonymous] // 暫時允許匿名上傳，未來應加入權限驗證
	public async Task<IActionResult> UploadForTinyMce([FromForm] TinyMceUploadDto dto)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		try
		{
			// 【簡化】不再需要查詢 FolderId，直接將 ModuleId 和 ProgId 傳給 Service
			var uploadDto = new AssetFileUploadDto
			{
				ModuleId = "Brand",       // 根據業務邏輯設定
				ProgId = dto.BlockType,   // ProgId 直接對應前端傳來的 BlockType (例如: ContentEditor)
				Meta = new List<AssetFileDetailsDto>
				{
					new AssetFileDetailsDto
					{
						File = dto.File,
						AltText = dto.AltText,
						Caption = dto.Caption,
						IsActive = dto.IsActive
					}
				}
			};

			object resultObject = await _assetFileService.AddFilesAsync(uploadDto);
			return ParseUploadResult(resultObject);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = new { message = $"伺服器內部錯誤: {ex.Message}" } });
		}
	}


	/// <summary>
	/// 從指定的 URL 下載圖片，上傳到 Cloudinary 並存入資料庫。
	/// </summary>
	[HttpPost("upload-by-url")]
	[AllowAnonymous]
	public async Task<IActionResult> UploadByUrl([FromBody] UploadByUrlDto dto)
	{
		if (!ModelState.IsValid || !Uri.TryCreate(dto.ImageUrl, UriKind.Absolute, out _))
		{
			return BadRequest(new { error = new { message = "請提供有效的圖片 URL。" } });
		}

		try
		{
            // 1. 從 URL 下載圖片
			var client = _httpClientFactory.CreateClient();
			var response = await client.GetAsync(dto.ImageUrl);
			if (!response.IsSuccessStatusCode)
			{
				return BadRequest(new { error = new { message = "無法從指定的 URL 下載圖片。" } });
			}

			await using var imageStream = await response.Content.ReadAsStreamAsync();
			var fileName = Path.GetFileName(new Uri(dto.ImageUrl).AbsolutePath);

            // 2. 模擬 IFormFile
			var formFile = new FormFile(imageStream, 0, imageStream.Length, "file", fileName)
			{
				Headers = new HeaderDictionary(),
				ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream"
			};

			// 3. 獲取 FolderId
			//int? folderId = await GetFolderIdByBlockType(dto.BlockType);
			// 【簡化】同樣不再需要查詢 FolderId

			// 4. 準備上傳 DTO
			var uploadDto = new AssetFileUploadDto
			{
				ModuleId = "Brand",
				//ProgId = "ContentEditor",
				//FolderId = folderId,
				ProgId = dto.BlockType, // ProgId 對應 BlockType
				Meta = new List<AssetFileDetailsDto>
				{
					new AssetFileDetailsDto
					{
						File = formFile,
						AltText = dto.AltText,
						Caption = dto.Caption,
						IsActive = dto.IsActive
					}
				}
			};

            // 5. 呼叫現有的上傳服務
			object resultObject = await _assetFileService.AddFilesAsync(uploadDto);
			
            // 6. 解析結果並回傳
			return ParseUploadResult(resultObject);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = new { message = $"伺服器內部錯誤: {ex.Message}" } });
		}
	}

	#endregion

	#region === 資料夾與檔案管理 (Folder & File Management) ===

	/// <summary>
	/// 取得指定資料夾下的內容 (子資料夾與檔案)
	/// </summary>
	[HttpGet("folder-items")]
	[AllowAnonymous]
	public async Task<IActionResult> GetFolderItems(
		[FromQuery] int? parentId,
		[FromQuery] string? keyword,
		[FromQuery] int? start,
		[FromQuery] int? length,
		[FromQuery] int draw = 1,
		[FromQuery] string? orderColumn = "Name",
		[FromQuery] string? orderDir = "asc")
	{
		// 【修正】方法名稱 GetPagedFolderItemsAsync -> GetPagedFolderItems
		var result = await _assetFileService.GetPagedFolderItems(parentId, keyword, start, length, draw, orderColumn, orderDir);
		return Ok(result);
	}

	/// <summary>
	/// 建立新資料夾
	/// </summary>
	[HttpPost("create-folder")]
	[AllowAnonymous]
	public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.FolderName))
		{
			return BadRequest(new { success = false, message = "資料夾名稱不可為空。" });
		}
		var result = await _assetFileService.CreateFolderAsync(dto.FolderName, dto.ParentId);
		return Ok(result);
	}

	/// <summary>
	/// 重新命名資料夾
	/// </summary>
	[HttpPost("rename-folder")]
	[AllowAnonymous]
	public async Task<IActionResult> RenameFolder([FromBody] SysFolderDto dto)
	{
		if (dto.FolderId == 0 || string.IsNullOrWhiteSpace(dto.FolderName))
		{
			return BadRequest(new { success = false, message = "缺少必要的資料夾資訊。" });
		}
		var result = await _assetFileService.RenameFolder(dto);
		return Ok(result);
	}

	/// <summary>
	/// 刪除空資料夾
	/// </summary>
	[HttpDelete("delete-folder/{folderId}")]
	[AllowAnonymous]
	public async Task<IActionResult> DeleteFolder(int folderId)
	{
		var result = await _assetFileService.DeleteFolder(folderId);
		return Ok(result);
	}

	/// <summary>
	/// 移動檔案或資料夾
	/// </summary>
	[HttpPost("move-items")]
	[AllowAnonymous]
	public async Task<IActionResult> MoveItems([FromBody] MoveRequestDto dto)
	{
		if (dto.Ids == null || !dto.Ids.Any())
		{
			return BadRequest(new { success = false, message = "請提供要移動的項目 ID。" });
		}
		var result = await _assetFileService.MoveToFolder(dto);
		return Ok(result);
	}

	/// <summary>
	/// 批次刪除檔案 (軟刪除)
	/// </summary>
	[HttpPost("batch-delete-files")]
	[AllowAnonymous]
	public async Task<IActionResult> BatchDeleteFiles([FromBody] List<int> ids)
	{
		if (ids == null || !ids.Any())
		{
			return BadRequest(new { success = false, message = "請提供要刪除的檔案 ID。" });
		}
		// 【修正】方法名稱 BatchDeleteFilesAsync -> BatchDelete
		var result = await _assetFileService.BatchDelete(ids);
		return Ok(result);
	}

	/// <summary>
	/// 取得指定資料夾的麵包屑路徑
	/// </summary>
	[HttpGet("breadcrumb/{folderId}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetBreadcrumb(int folderId)
	{
		var result = await _assetFileService.GetBreadcrumbPath(folderId);
		return Ok(result);
	}

	#endregion

	#region === 私有輔助方法 (Private Helper Methods) ===

	/// <summary>
	/// 根據前端傳來的區塊類型字串（"Banner", "Accordion", "Article"），去 SYS_Folders 資料表中查詢對應的 FolderId
	/// </summary>
	private async Task<int?> GetFolderIdByBlockType(string blockType)
	{
		if (string.IsNullOrWhiteSpace(blockType)) return null;

		// 根據 blockType 查詢 FolderId
		return await _context.SysFolders
			.Where(f => f.FolderName.Equals(blockType, StringComparison.OrdinalIgnoreCase))
			.Select(f => (int?)f.FolderId)
			.FirstOrDefaultAsync();
	}

	/// <summary>
	/// 【核心變更】解析 AddFilesAsync 服務的回傳結果。
	/// 現在它解析的是 Repository 回傳的標準格式 { success, message, data: { files: [...] } }
	/// </summary>
	private IActionResult ParseUploadResult(object resultObject)
	{
		var jsonString = JsonSerializer.Serialize(resultObject);
		using var doc = JsonDocument.Parse(jsonString);
		var root = doc.RootElement;

		if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
		{
			// 注意路徑變為 data -> files
			if (root.TryGetProperty("data", out var dataElement) &&
				dataElement.TryGetProperty("files", out var filesElement) && // <--- 這裡的屬性名稱是 'files'
				filesElement.GetArrayLength() > 0)
			{
				var firstFile = filesElement[0];
				// Repository 回傳的屬性是大寫開頭 (FileUrl, FileId)
				if (firstFile.TryGetProperty("FileUrl", out var fileUrlElement) &&
					firstFile.TryGetProperty("FileId", out var fileIdElement))
				{
					return Ok(new
					{
						location = fileUrlElement.GetString(),
						fileId = fileIdElement.GetInt32()
					});
				}
			}
			return BadRequest(new { error = new { message = "上傳成功，但解析回傳資料時發生錯誤。" } });
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
	#endregion

}