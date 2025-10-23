using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得資料夾結構
        /// </summary>
        /// <returns></returns>
		[HttpGet]
		public IActionResult GetTreeData()
		{
			var folders = _db.SysFolders
				.Where(f => f.IsActive)
				.Select(f => new
				{
					id = f.FolderId.ToString(),
					parent = f.ParentId == null ? "#" : f.ParentId.ToString(),
					text = f.FolderName
				})
				.ToList();

			return Json(folders);
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
		public async Task<IActionResult> GetBreadcrumbPath(int folderId)
		{
			var list = new List<object>();
			var folder = await _db.SysFolders.FindAsync(folderId);

			while (folder != null)
			{
				list.Insert(0, new { folder.FolderId, folder.FolderName });
				folder = folder.ParentId.HasValue
					? await _db.SysFolders.FindAsync(folder.ParentId.Value)
					: null;
			}

			return Json(list);
		}

		[HttpGet]
		public async Task<IActionResult> GetPagedFolderItems(
			int? parentId = null,
			string? keyword = "",
			int? start = null,      // 可選：起始筆數（DataTables 模式用）
			int? length = null,     // 可選：每頁筆數（DataTables 模式用）
			int draw = 1,           // DataTables 驗證用
			string? orderColumn = "Name",
			string? orderDir = "asc"
		)
		{
			// === 0️ 防呆 ===
			if (parentId == 0) parentId = null;

			// === 1 查資料夾 ===
			var folderQuery = _db.SysFolders
				.Where(f => f.IsActive && f.ParentId == parentId);

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
			var fileQuery = _db.SysAssetFiles.AsQueryable().Where(f => f.IsActive);

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

			// === 3️ 合併 ===
			var combined = folders.Concat(files).ToList();
			var totalCount = combined.Count; // 總筆數

			// === 4️ 排序 ===
			bool desc = string.Equals(orderDir, "desc", StringComparison.OrdinalIgnoreCase);
			combined = orderColumn?.ToLower() switch
			{
				"modifieddate" => desc
					? combined.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.ModifiedDate).ToList()
					: combined.OrderByDescending(x => x.IsFolder).ThenBy(x => x.ModifiedDate).ToList(),

				"size" => desc
					? combined.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.Size).ToList()
					: combined.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Size).ToList(),

				"mimetype" => desc
					? combined.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.MimeType).ToList()
					: combined.OrderByDescending(x => x.IsFolder).ThenBy(x => x.MimeType).ToList(),

				_ => desc
					? combined.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.Name).ToList()
					: combined.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Name).ToList(),
			};

			// === 5️ 分頁（如果前端有傳 start/length） ===
			List<FolderItemDto> paged;
			if (start.HasValue && length.HasValue && length > 0)
				paged = combined.Skip(start.Value).Take(length.Value).ToList();
			else
				paged = combined; // 不分頁 → 原 GetFolderItems 模式

			// === 6️ 麵包屑 ===
			var breadcrumb = await GetBreadcrumbAsync(parentId);

			// === 7️ 判斷回傳模式（DataTables 或一般） ===
			if (start.HasValue && length.HasValue)
			{
				// DataTables 模式
				return Json(new
				{
					draw,
					recordsTotal = totalCount,
					recordsFiltered = totalCount,
					data = paged,
					breadcrumb
				});
			}
			else
			{
				// 一般模式（取代原 GetFolderItems）
				return Json(new
				{
					items = paged,
					breadcrumb
				});
			}
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
			if (ids == null || !ids.Any())
				return Json(new { success = false, message = "未選取任何項目" });

			// === 查出這些 ID 是哪些資料夾 or 檔案 ===
			var folders = await _db.SysFolders
				.Where(f => ids.Contains(f.FolderId))
				.ToListAsync();

			var files = await _db.SysAssetFiles
				.Where(f => ids.Contains(f.FileId))
				.ToListAsync();

			// === 驗證資料夾是否可以刪除 ===
			foreach (var folder in folders)
			{
				bool hasSubFolders = await _db.SysFolders.AnyAsync(f => f.ParentId == folder.FolderId);
				bool hasFiles = await _db.SysAssetFiles.AnyAsync(f => f.FolderId == folder.FolderId);

				if (hasSubFolders || hasFiles)
				{
					return Json(new
					{
						success = false,
						message = $"資料夾「{folder.FolderName}」下仍有內容，無法刪除。"
					});
				}
			}

			// === 執行刪除 ===
			_db.SysFolders.RemoveRange(folders);
			_db.SysAssetFiles.RemoveRange(files);
			await _db.SaveChangesAsync();

			return Json(new
			{
				success = true,
				message = $"成功刪除 {folders.Count + files.Count} 筆項目"
			});
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
				bool exists = await _db.SysFolders.AnyAsync(f => f.ParentId == dto.ParentId && f.FolderName == dto.FolderName);

				if (exists)
					return Json(new { success = false, message = "同一層已存在相同名稱的資料夾" });

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
			var names = new List<string>();
			var folder = await _db.SysFolders.FindAsync(folderId);

			while (folder != null)
			{
				names.Insert(0, folder.FolderName);
				folder = folder.ParentId.HasValue
					? await _db.SysFolders.FindAsync(folder.ParentId.Value)
					: null;
			}

			return "/" + string.Join("/", names);
		}

		/// <summary>
		/// 重新命名資料夾
		/// </summary>
		/// <param name="dto"></param>
		/// <returns></returns>
		[HttpPost]
		public async Task<IActionResult> RenameFolder([FromBody] SysFolderDto dto)
			=> Json(_frepo.RenameFolder(dto));

		/// <summary>
		/// 取得所有資料夾
		/// </summary>
		/// <returns></returns>
		[HttpGet]
        public async Task<IActionResult> GetAllFolders()
			=> Json(await _frepo.GetAllFolders());

		/// <summary>
		/// 移動檔案到指定資料夾
		/// </summary>
		/// <param name="dto"></param>
		/// <returns></returns>
		[HttpPost]
        public async Task<IActionResult> MoveToFolder([FromBody] MoveRequestDto dto)
        {
			var res = await _frepo.MoveToFolder(dto);

			return Json(new
			{
				success = string.IsNullOrEmpty(res),
				message = res
			});
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

        /// <summary>
        /// 清除雲端未建資料庫的資料
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
		[HttpPost]
		public async Task<IActionResult> CleanCloudinaryOrphans(CancellationToken ct)
		{
			var (totalChecked, deletedCount, deletedKeys) = await _frepo.CleanOrphanCloudinaryFiles(ct);

			return Json(new
			{
				success = true,
				totalChecked,
				deletedCount,
				deletedKeys
			});
		}
	}
}
