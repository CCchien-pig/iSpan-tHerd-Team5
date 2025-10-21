using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SYS;
using tHerdBackend.Infra.Models;

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

            // Step 1️⃣ 取得子資料夾
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

            // Step 2️⃣ 取得檔案
            var filesQuery = _db.SysAssetFiles
                .Where(f => f.FolderId == parentId && f.IsActive);

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

            // ✅ 找出目前所在資料夾
            SysFolder? currentFolder = null;
            if (parentId.HasValue && parentId > 0)
                currentFolder = await _db.SysFolders.FindAsync(parentId);

            // ✅ 從上層開始遞迴找（例如 Products → PROD）
            var breadcrumb = await GetBreadcrumbAsync(currentFolder?.ParentId);

            // ✅ 把自己這層加進來（例如 Products）
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
        public async Task<IActionResult> GetPagedFolderItems(int? parentId, string? keyword = "", int pageIndex = 1, int pageSize = 10)
        {
            var q = _db.SysAssetFiles.AsQueryable();

            // 過濾資料夾層級
            if (parentId.HasValue)
                q = q.Where(f => f.FolderId == parentId);

            // 關鍵字搜尋
            if (!string.IsNullOrEmpty(keyword))
                q = q.Where(f => f.AltText.Contains(keyword) || f.Caption.Contains(keyword));

            var totalCount = await q.CountAsync();

            var items = await q
                .OrderByDescending(f => f.CreatedDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
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

            var breadcrumb = new List<string> { "根目錄" }; // 可根據 parentId 動態生成

            return Json(new
            {
                totalCount,
                items,
                breadcrumb
            });
        }
    }
}
