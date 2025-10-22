using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SYS;
using tHerdBackend.Infra.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    /// <summary>
    /// 圖片管理
    /// </summary>
    [Area("SYS")]
    public class ImagesController : Controller
    {
        private readonly tHerdDBContext _db;

        public ImagesController(tHerdDBContext db)
        {
            _db = db;
        }

        // === 頁面 ===
        public IActionResult Index()
        {
            return View();
        }

        // === API: 取得子資料夾與檔案 ===
        [HttpGet]
        public async Task<IActionResult> GetFolderItems(int? parentId = null, string? keyword = "")
        {
            if (parentId == 0)
                parentId = null;

            // Step 1️ 取得子資料夾
            var folders = await _db.SysFolders
                .Where(f => f.ParentId == parentId && f.IsActive)
                .Select(f => new FolderItemDto
                {
                    Id = f.FolderId,
                    Name = f.FolderName,
                    IsFolder = true,
                    ModifiedDate = null,
                    MimeType = "資料夾",
                    Size = null
                })
                .ToListAsync();

            // Step 2️ 取得檔案
            var filesQuery = _db.SysAssetFiles.Where(f => f.IsActive);

            // 根目錄（parentId = null）
            if (parentId == null)
            {
                // 只撈出真的沒有 FolderId 的
                filesQuery = filesQuery.Where(f => f.FolderId == null);
            }
            else
            {
                filesQuery = filesQuery.Where(f => f.FolderId == parentId);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filesQuery = filesQuery.Where(f =>
                    f.FileKey.Contains(keyword) ||
                    f.AltText.Contains(keyword) ||
                    f.Caption.Contains(keyword));
            }

            var files = await filesQuery
                .Select(f => new FolderItemDto
                {
                    Id = f.FileId,
                    Name = f.FileKey,
                    IsFolder = false,
                    Url = f.IsExternal ? f.FileUrl :
                          $"/Images/{Path.GetFileName(f.FileKey)}",
                    MimeType = f.MimeType,
                    Size = f.FileSizeBytes,
                    ModifiedDate = f.CreatedDate
                })
                .ToListAsync();

            var items = folders.Concat(files)
                .OrderByDescending(x => x.IsFolder)
                .ThenBy(x => x.Name)
                .ToList();

            // 找出目前所在資料夾
            SysFolder? currentFolder = null;
            if (parentId.HasValue && parentId > 0)
                currentFolder = await _db.SysFolders.FindAsync(parentId);

            // 從上層開始遞迴找（例如 Products → PROD）
            var breadcrumb = await GetBreadcrumbAsync(currentFolder?.ParentId);

            // 把自己這層加進來（例如 Products）
            if (currentFolder != null)
            {
                breadcrumb.Add(new SysFolderDto
                {
                    FolderId = currentFolder.FolderId,
                    FolderName = currentFolder.FolderName,
                    ParentId = currentFolder.ParentId
                });
            }

            return Json(new { items, breadcrumb });
        }

        // === 產生麵包屑（遞迴找上層） ===
        private async Task<List<SysFolderDto>> GetBreadcrumbAsync(int? folderId)
        {
            var breadcrumb = new List<SysFolderDto>();

            if (!folderId.HasValue)
                return breadcrumb;

            var current = await _db.SysFolders.FindAsync(folderId);

            while (current != null)
            {
                breadcrumb.Insert(0, new SysFolderDto
                {
                    FolderId = current.FolderId,
                    FolderName = current.FolderName,
                    ParentId = current.ParentId
                });

                if (current.ParentId == null)
                    break;

                current = await _db.SysFolders.FindAsync(current.ParentId);
            }

            return breadcrumb;
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedFolderItems(
            int? parentId = null,
            string? keyword = "",
            int start = 0,      // 起始筆數（由 DataTables 自動傳）
            int length = 10,    // 每頁筆數
            int draw = 1        // DataTables 驗證用
        )
        {
            // === 1 查資料夾 ===
            var folderQuery = _db.SysFolders
                .Where(f => f.ParentId == parentId && f.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
                folderQuery = folderQuery.Where(f => f.FolderName.Contains(keyword));

            var folders = await folderQuery
            .Select(f => new FolderItemDto
            {
                Id = f.FolderId,
                Name = f.FolderName,
                IsFolder = true,
                MimeType = "資料夾",
                ModifiedDate = null,
                Size = null
            })
            .ToListAsync();

            var q = _db.SysAssetFiles.AsQueryable();

            // === 2️ 查檔案 ===
            if (parentId == null)
            {
                q = q.Where(f => f.FolderId == null);
            }
            else
            {
                q = q.Where(f => f.FolderId == parentId);
            }

            // 關鍵字搜尋
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(f =>
                    f.FileKey.Contains(keyword) ||
                    f.AltText.Contains(keyword) ||
                    f.Caption.Contains(keyword));
            }

            var data = await q
                .Select(f => new FolderItemDto
                {
                    Id = f.FileId,
                    Name = Path.GetFileName(f.FileKey) ?? f.FileKey,
                    Url = f.IsExternal
                        ? f.FileUrl
                        : $"/Uploads/{f.FolderId}/{Path.GetFileName(f.FileKey)}",
                    MimeType = f.MimeType,
                    IsFolder = false,
                    ModifiedDate = f.CreatedDate,
                    Size = f.FileSizeBytes
                })
                .ToListAsync();

            // === 3️ 合併 + 排序 + 分頁 ===
            var combined = folders.Concat(data)
            .OrderByDescending(x => x.IsFolder) // 資料夾永遠在上方
            .ThenBy(x => x.Name)
            .ToList();

            var totalCount = combined.Count;
            var paged = combined.Skip(start).Take(length).ToList();

            // === 4️ 生成麵包屑（可沿用你的遞迴法） ===
            var breadcrumb = await GetBreadcrumbAsync(parentId);

            // === 5️ 回傳 DataTables 標準格式 ===
            return Json(new
            {
                draw,
                recordsTotal = totalCount,
                recordsFiltered = totalCount,
                data = paged,
                breadcrumb
            });
        }

        [HttpPost]
        public async Task<IActionResult> BatchSetActive([FromBody] BatchActiveRequest req)
        {
            if (req.Ids == null || req.Ids.Count == 0)
                return BadRequest();

            var files = await _db.SysAssetFiles
                .Where(f => req.Ids.Contains(f.FileId))
                .ToListAsync();

            foreach (var f in files)
                f.IsActive = req.IsActive;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, count = files.Count });
        }

        public class BatchActiveRequest
        {
            public List<int> Ids { get; set; } = new();
            public bool IsActive { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> BatchDelete([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest();

            var files = await _db.SysAssetFiles
                .Where(f => ids.Contains(f.FileId))
                .ToListAsync();

            _db.SysAssetFiles.RemoveRange(files);
            await _db.SaveChangesAsync();
            return Ok(new { success = true, deleted = files.Count });
        }

        [HttpPost]
        public async Task<IActionResult> MoveToFolder([FromBody] MoveFolderRequest req)
        {
            if (req.Ids == null || req.Ids.Count == 0)
                return BadRequest();

            var files = await _db.SysAssetFiles
                .Where(f => req.Ids.Contains(f.FileId))
                .ToListAsync();

            foreach (var f in files)
                f.FolderId = req.FolderId;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, moved = files.Count });
        }

        public class MoveFolderRequest
        {
            public List<int> Ids { get; set; } = new();
            public int FolderId { get; set; }
        }

        [HttpGet]
		public IActionResult GetFileDetail(int id)
		{
			var file = _db.SysAssetFiles
				.Where(f => f.FileId == id)
				.Select(f => new
				{
					FileId = f.FileId,
					Name = Path.GetFileName(f.FileKey),
					FileUrl = f.FileUrl,
					AltText = f.AltText,
					Caption = f.Caption,
					IsActive = f.IsActive
				})
				.FirstOrDefault();

			return Json(file);
		}

		[HttpPost]
        public async Task<IActionResult> UpdateFile([FromBody] UpdateFileDto dto)
        {
            var file = await _db.SysAssetFiles.FindAsync(dto.Id);
            if (file == null)
                return NotFound();

            file.AltText = dto.AltText;
            file.Caption = dto.Caption;
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        public class UpdateFileDto
        {
            public int Id { get; set; }
            public string? AltText { get; set; }
            public string? Caption { get; set; }
        }
    }
}
