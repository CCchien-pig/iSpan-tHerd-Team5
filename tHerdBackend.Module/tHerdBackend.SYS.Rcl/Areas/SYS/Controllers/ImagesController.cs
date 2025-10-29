using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.SYS;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    /// <summary>
    /// 圖片管理
    /// </summary>
    [Area("SYS")]
    public class ImagesController : Controller
    {
		private readonly Cloudinary _cloudinary;
		private readonly tHerdDBContext _db;
        private readonly ISysAssetFileService _frepo;

        public ImagesController(Cloudinary cloudinary, tHerdDBContext db, ISysAssetFileService frepo)
        {
            _cloudinary = cloudinary;
            _db = db;
            _frepo = frepo;
        }

        // === 頁面 ===
        public IActionResult Index() => View();

        public class FileListWrapper
        {
            public List<SimpleAssetFileDto> Files { get; set; } = new();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RenderFileListPartial()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            // 若前端不小心多包一層字串（例如 "\"{...}\""）
            if (body.StartsWith("\"") && body.EndsWith("\""))
                body = JsonSerializer.Deserialize<string>(body)!;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FlexibleStringConverter() }
            };

            var wrapper = JsonSerializer.Deserialize<FileListWrapper>(body, options);
            var files = wrapper?.Files;

            if (files == null || !files.Any())
                return PartialView("_FileListPartial", new List<SysAssetFileDto>());

            var mapped = files.Select(f => new SysAssetFileDto
            {
                FileId = f.FileId,
                FileUrl = f.FileUrl,
                MimeType = f.MimeType ?? "image/jpeg",
                AltText = f.AltText ?? "",
                Caption = f.Caption ?? "",
                IsActive = f.IsActive,
                CreatedDate = DateTime.Now,
                FileKey = "",
                IsExternal = false,
                FolderPaths = new List<FolderLevelItem>()
            }).ToList();

            return PartialView("_FileListPartial", mapped);
        }

        // ============================================================
        // 資料夾結構與查詢
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetTreeData()
            => Json(await _frepo.GetTreeData());

        [HttpGet]
        public async Task<IActionResult> GetBreadcrumbPath(int folderId)
            => Json(await _frepo.GetBreadcrumbPath(folderId));

        [HttpGet]
        public async Task<IActionResult> GetPagedFolderItems(
            int? parentId = null,
            string? keyword = "",
            int? start = null,
            int? length = null,
            int draw = 1,
            string? orderColumn = "Name",
            string? orderDir = "asc")
            => Json(await _frepo.GetPagedFolderItems(parentId, keyword, start, length, draw, orderColumn, orderDir));

        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromBody] SysFolderDto dto)
            => Json(await _frepo.CreateFolderAsync(dto.FolderName, dto.ParentId));

        [HttpPost]
        public async Task<IActionResult> RenameFolder([FromBody] SysFolderDto dto)
            => Json(await _frepo.RenameFolder(dto));

        [HttpGet]
        public async Task<IActionResult> GetAllFolders()
            => Json(await _frepo.GetAllFolders());

        [HttpPost]
        public async Task<IActionResult> MoveToFolder([FromBody] MoveRequestDto dto)
            => Json(await _frepo.MoveToFolder(dto));

        // ============================================================
        // 🖼️ 圖片檔案操作
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetFileDetail(int fileId)
        {
            var res = await _frepo.GetFileById(fileId);
            return Json(new
            {
                success = res != null,
                data = res
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFile([FromBody] SysAssetFileDto dto, CancellationToken ct)
            => Json(await _frepo.UpdateImageMeta(dto, ct));

        /// <summary>
        /// 照片資訊即時更新
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UpdateFileMetaField([FromBody] FileMetaUpdateDto model)
        {
            try
            {
                await _frepo.UpdateFileMetaField(model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // 攔截錯誤，確保回傳 JSON 而不是 .NET 例外字串
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(int? fileId, CancellationToken ct)
        {
            if (fileId == null)
                return Json(new { success = false, message = "請選擇檔案" });

            var result = await _frepo.DeleteImage((int)fileId, ct);

            var success = (bool)(result.GetType().GetProperty("success")?.GetValue(result) ?? false);
            var message = (string)(result.GetType().GetProperty("message")?.GetValue(result) ?? "未知狀態");

            return Json(new
            {
                success,
                status = success ? "ok" : "error",
                message
            });
        }

        // ============================================================
        // ⚙️ 批次操作
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> BatchSetActive([FromBody] BatchActiveRequest req)
        {
            if (req.Ids == null || req.Ids.Count == 0)
                return Json(new { success = false, message = "未選取任何項目" });

            return Json(await _frepo.BatchSetActive(req));
        }

        [HttpPost]
        public async Task<IActionResult> BatchDelete([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return Json(new { success = false, message = "未選取任何項目" });

            var result = await _frepo.BatchDelete(ids);

            // 確保即使部分刪除也回傳 200，不要拋例外
            return Json(result);
        }

        // ============================================================
        // ☁️ Cloudinary 清理
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> CleanCloudinaryOrphans(CancellationToken ct)
            => Json(await _frepo.CleanOrphanCloudinaryFiles(ct));

        // === 刪除資料夾 ===
        [HttpPost]
        public async Task<IActionResult> DeleteFolder([FromBody] int folderId)
            => Json(await _frepo.DeleteFolder(folderId));
    }
}
