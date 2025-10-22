using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly tHerdDBContext _db;
        private readonly ISysAssetFileService _frepo;

        public ImagesController(tHerdDBContext db, ISysAssetFileService frepo)
        {
            _db = db;
            _frepo = frepo;
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
            // === 防呆處理 ===
            if (length <= 0) length = 10;
            if (start < 0) start = 0;

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
                    Size = null,
                    Url = string.Empty
                })
                .ToListAsync();

            // === 2️ 查檔案 ===
            var fileQuery = _db.SysAssetFiles.AsQueryable();

            if (parentId == null)
                fileQuery = fileQuery.Where(f => f.FolderId == null);
            else
                fileQuery = fileQuery.Where(f => f.FolderId == parentId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                fileQuery = fileQuery.Where(f =>
                    f.FileKey.Contains(keyword) ||
                    f.AltText.Contains(keyword) ||
                    f.Caption.Contains(keyword));
            }

            var data = await fileQuery
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

            // === 3 計算數量 ===
            var totalFolderCount = await _db.SysFolders
                .Where(f => f.ParentId == parentId && f.IsActive)
                .CountAsync();

            var totalFileCount = await _db.SysAssetFiles
                .Where(f => f.FolderId == parentId && f.IsActive)
                .CountAsync();

            var totalCount = totalFolderCount + totalFileCount;

            var files = await fileQuery
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
                    Size = f.FileSizeBytes,
                    IsActive = f.IsActive
                })
                .ToListAsync();

            // === 4 合併 + 排序 + 分頁 ===
            var combined = folders.Concat(files)
                .OrderByDescending(x => x.IsFolder) // 資料夾在上
                .ThenBy(x => x.Name)
                .ToList();

            // 5 若有搜尋條件，FilteredCount 要以搜尋結果為準
            var filteredCount = combined.Count;

            // 分頁
            var paged = combined.Skip(start).Take(length).ToList();

            // === 6️ 麵包屑（遞迴取得）===
            var breadcrumb = await GetBreadcrumbAsync(parentId);

            // === 7️ 回傳 DataTables 標準格式 ===
            return Json(new
            {
                draw,                           // 驗證用
                recordsTotal = totalCount,       // 總筆數（未篩選）
                recordsFiltered = filteredCount, // 篩選後筆數
                data = paged,                    // 當前頁資料
                breadcrumb                       // 額外資訊（前端自用）
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

        /// <summary>
        /// 刪除多個檔案
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
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

        public class MoveFolderRequest
        {
            public List<int> Ids { get; set; } = new();
            public int FolderId { get; set; }
        }

        /// <summary>
        /// 依檔案編號，取得檔案
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
		public async Task<IActionResult> GetFileDetail(int id)
		{
            var file = await _frepo.GetFilesById(id);
            if (file == null)
                return Json(new { success = false, message = "找不到檔案" });

            return Json(new { success = true, data = file });
        }

        /// <summary>
        /// 新增資料夾
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromBody] SysFolderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FolderName))
                return Json(new { success = false, message = "請輸入資料夾名稱" });

            try
            {
                // 防止指向自己或循環
                if (dto.ParentId == dto.FolderId)
                    return Json(new { success = false, message = "資料夾不能指向自己" });

                // 如果指定的父層不存在
                if (dto.ParentId.HasValue && !await _db.SysFolders.AnyAsync(f => f.FolderId == dto.ParentId))
                    return Json(new { success = false, message = "找不到父層資料夾" });

                // 建立新資料夾
                var newFolder = new SysFolder
                {
                    FolderName = dto.FolderName.Trim(),
                    ParentId = dto.ParentId,
                    IsActive = true,
                };

                _db.SysFolders.Add(newFolder);
                await _db.SaveChangesAsync();

                // 組出完整路徑
                string fullPath = await BuildFullPathAsync(newFolder.FolderId);

                return Json(new
                {
                    success = true,
                    message = "建立成功",
                    data = new
                    {
                        newFolder.FolderId,
                        newFolder.FolderName,
                        newFolder.ParentId,
                        fullPath
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 遞迴組出完整路徑，例如 /根目錄/產品圖片/2025
        /// </summary>
        private async Task<string> BuildFullPathAsync(int folderId)
        {
            var folderList = await _db.SysFolders.AsNoTracking().ToListAsync();
            var folder = folderList.FirstOrDefault(f => f.FolderId == folderId);

            if (folder == null)
                return string.Empty;

            var path = new Stack<string>();
            while (folder != null)
            {
                path.Push(folder.FolderName);
                folder = folder.ParentId.HasValue
                    ? folderList.FirstOrDefault(f => f.FolderId == folder.ParentId.Value)
                    : null;
            }

            return "/" + string.Join("/", path);
        }

        [HttpPost]
        public async Task<IActionResult> RenameFolder([FromBody] SysFolderDto dto)
        {
            if (dto == null || dto.FolderId <= 0)
                return Json(new { success = false, message = "資料夾編號無效" });

            if (string.IsNullOrWhiteSpace(dto.FolderName))
                return Json(new { success = false, message = "請輸入資料夾名稱" });

            var folder = await _db.SysFolders.FindAsync(dto.FolderId);
            if (folder == null)
                return Json(new { success = false, message = "找不到指定資料夾" });

            folder.FolderName = dto.FolderName.Trim();
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "資料夾名稱已更新" });
        }

        /// <summary>
        /// 取得所有資料夾
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAllFolders()
        {
            try
            {
                var folders = await _db.SysFolders
                    .Select(f => new
                    {
                        f.FolderId,
                        f.FolderName,
                        f.ParentId,
                        f.IsActive
                    })
                    .ToListAsync();

                // 用 Dictionary 快取
                var folderDict = folders.ToDictionary(f => f.FolderId);

                // 建立結果
                var result = new List<object>();

                foreach (var f in folders)
                {
                    var names = new List<string> { f.FolderName };
                    var parentId = f.ParentId;

                    // 防止循環
                    var visited = new HashSet<int> { f.FolderId };

                    while (parentId != null &&
                           folderDict.TryGetValue(parentId.Value, out var parent) &&
                           !visited.Contains(parent.FolderId))
                    {
                        names.Insert(0, parent.FolderName);
                        visited.Add(parent.FolderId);
                        parentId = parent.ParentId;
                    }

                    result.Add(new
                    {
                        f.FolderId,
                        f.FolderName,
                        f.ParentId,
                        f.IsActive,
                        FullPath = "/" + string.Join("/", names)
                    });
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MoveToFolder([FromBody] MoveRequestDto dto)
        {
            try
            {
                if (dto == null || dto.Ids == null || dto.Ids.Count == 0)
                    return Json(new { success = false, message = "沒有選取項目" });

                if (dto.FolderId == 0)
                    dto.FolderId = null;

                // 1️ 如果 FolderId 是 null → 根目錄，允許通過
                SysFolder? targetFolder = null;
                if (dto.FolderId.HasValue)
                {
                    targetFolder = await _db.SysFolders.FindAsync(dto.FolderId);
                    if (targetFolder == null)
                        return Json(new { success = false, message = "找不到目標資料夾" });
                }

                // 2️ 找出檔案
                var files = await _db.SysAssetFiles
                    .Where(f => dto.Ids.Contains(f.FileId))
                    .ToListAsync();

                // 3️ 找出資料夾
                var folders = await _db.SysFolders
                    .Where(f => dto.Ids.Contains(f.FolderId))
                    .ToListAsync();

                // 防呆：不允許資料夾移到自己或子層底下
                if (dto.FolderId != null)
                {
                    var movingIds = folders.Select(f => f.FolderId).ToHashSet();
                    if (movingIds.Contains(dto.FolderId.Value))
                        return Json(new { success = false, message = "無法將資料夾移動到自己" });
                }

                // 4️ 更新檔案的 FolderId
                foreach (var file in files)
                    file.FolderId = dto.FolderId;

                // 5️ 更新資料夾的 ParentId（防止自我循環）
                foreach (var folder in folders)
                    folder.ParentId = dto.FolderId;

                // 防呆：不允許資料夾移到自己或子層底下
                if (dto.FolderId != null)
                {
                    var movingIds = folders.Select(f => f.FolderId).ToHashSet();

                    // 1️ 檢查是否移到自己
                    if (movingIds.Contains(dto.FolderId.Value))
                        return Json(new { success = false, message = "無法將資料夾移動到自己" });

                    // 2️ 檢查是否移到自己的子層底下
                    var allFolders = await _db.SysFolders.ToListAsync();
                    foreach (var movingFolder in folders)
                    {
                        if (IsDescendant(dto.FolderId.Value, movingFolder.FolderId, allFolders))
                        {
                            return Json(new { success = false, message = $"無法將「{movingFolder.FolderName}」移到自己的子層" });
                        }
                    }
                }

                await _db.SaveChangesAsync();

                var targetName = targetFolder?.FolderName ?? "根目錄";

                return Json(new
                {
                    success = true,
                    message = $"成功移動 {files.Count + folders.Count} 項至「{targetName}」"
                });
            }
            catch (Exception ex)
            {
                // ⚠️ 回傳 JSON 而不是整頁錯誤
                return Json(new { success = false, message = $"伺服器錯誤：{ex.Message}" });
            }
        }

        /// <summary>
        /// 判斷 targetId 是否是 sourceId 的子孫
        /// </summary>
        private bool IsDescendant(int targetId, int sourceId, List<SysFolder> all)
        {
            var current = all.FirstOrDefault(f => f.FolderId == targetId);
            while (current != null)
            {
                if (current.ParentId == sourceId)
                    return true;
                current = current.ParentId.HasValue
                    ? all.FirstOrDefault(f => f.FolderId == current.ParentId.Value)
                    : null;
            }
            return false;
        }

        /// <summary>
        /// 更新圖片資訊
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
		[HttpPost]
        public async Task<IActionResult> UpdateFile([FromBody] SysAssetFileDto dto, CancellationToken ct)
        {
            if (dto == null)
                return Json(new { success = false, message = "空的輸入資料" });

            var file = await _db.SysAssetFiles.FindAsync(dto.FileId);

            if (file == null)
                return Json(new { success = false, message = "找不到檔案" });

            var result = await _frepo.UpdateImageMeta(dto);

            if (result)
                return Json(new { success = true, message = "更新成功" });
            else
              
                return Json(new { success = false, message = "更新失敗" });
        }

        /// <summary>
        /// 刪除單一照片
        /// </summary>
        /// <param name="fileId">檔案編號</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DeleteFile(int? fileId, CancellationToken ct)
        {
            if (fileId == null)
                return Json(new { success = false, message = "請選擇檔案" });

            bool success = await _frepo.DeleteImage((int)fileId, ct);

            if (success)
                return Json(new { success = true, message = "刪除成功" });
            else
                return Json(new { success = false, message = "刪除失敗" });
        }
    }
}
