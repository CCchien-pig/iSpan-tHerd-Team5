using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.SYS;
using tHerdBackend.Core.Exceptions;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.SYS
{
    public class SysAssetFileRepository : ISysAssetFileRepository
    {
        private readonly Cloudinary _cloudinary;
        private readonly ISqlConnectionFactory _factory;     // 給「純查詢」或「無交易時」使用
        private readonly tHerdDBContext _db;                 // 寫入與交易來源

        public SysAssetFileRepository(Cloudinary cloudinary, ISqlConnectionFactory factory,
            tHerdDBContext db)
        {
            _cloudinary = cloudinary;
            _factory = factory;
            _db = db;
        }

        /// <summary>
        /// 取得子資料夾列表
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public async Task<List<SysFolderDto>> GetSubFoldersAsync(int? parentId)
        {
            return await _db.SysFolders
                .Where(f => f.ParentId == parentId && f.IsActive)
                .OrderBy(f => f.FolderName)
                .Select(f => new SysFolderDto
                {
                    FolderId = f.FolderId,
                    FolderName = f.FolderName,
                    ParentId = f.ParentId,
                })
                .ToListAsync();
        }

        /// <summary>
        /// 建立資料夾
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public async Task<SysFolderDto> CreateFolderAsync(string folderName, int? parentId)
        {
            var parent = await _db.SysFolders.FindAsync(parentId);

            var folder = new SysFolder
            {
                FolderName = folderName,
                ParentId = parentId,
                IsActive = true,
            };

            _db.SysFolders.Add(folder);
            await _db.SaveChangesAsync();

            return new SysFolderDto
            {
                FolderId = folder.FolderId,
                FolderName = folder.FolderName,
                IsActive = folder.IsActive,
            };
        }

        /// <summary>
        /// 圖片管理工具，取得檔案資訊
        /// </summary>
        /// <param name="moduleId"></param>
        /// <param name="progId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<PagedResult<SysAssetFileDto>> GetPagedFilesAsync(ImageFilterQueryDto query, CancellationToken ct = default)
        {
            var q = _db.SysAssetFiles.AsQueryable();

            if (!string.IsNullOrEmpty(query.Keyword))
                q = q.Where(f => f.AltText.Contains(query.Keyword) ||
                                 f.Caption.Contains(query.Keyword));

            var totalCount = await q.CountAsync();

            var items = await q
                .OrderByDescending(f => f.CreatedDate)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToAssetFileDtoExpression)
                .ToListAsync(ct);

            return new PagedResult<SysAssetFileDto>
            {
                TotalCount = totalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                Items = items
            };
        }

        /// <summary>
        /// 依模組及程式代號取得檔案資訊
        /// </summary>
        /// <param name="moduleId">模組代號</param>
        /// <param name="progId">程式代號</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<SysAssetFileDto>> GetFilesByProg(string moduleId, string progId, CancellationToken ct = default)
        => await _db.SysAssetFiles
            .Where(f => f.FileKey.StartsWith($"{moduleId}/{progId}"))
            .OrderByDescending(f => f.CreatedDate)
            .Select(ToAssetFileDtoExpression)
            .ToListAsync(ct);

        /// <summary>
        /// 依檔案代號取得檔案資訊
        /// </summary>
        /// <param name="moduleId">模組代號</param>
        /// <param name="progId">程式代號</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<SysAssetFileDto?> GetFilesById(int id, CancellationToken ct = default)
        => await _db.SysAssetFiles
            .Where(f => f.FileId == id)
            .Select(ToAssetFileDtoExpression).FirstOrDefaultAsync(ct);

        /// <summary>
        /// 共用投影邏輯：從 Entity → DTO
        /// </summary>
        private static readonly Expression<Func<SysAssetFile, SysAssetFileDto>> ToAssetFileDtoExpression = f => new SysAssetFileDto
        {
            FileId = f.FileId,
            FileKey = f.FileKey,
            IsExternal = f.IsExternal,
            FileUrl = f.FileUrl,
            FileExt = f.FileExt,
            MimeType = f.MimeType,
            Width = f.Width,
            Height = f.Height,
            FileSizeBytes = f.FileSizeBytes,
            AltText = f.AltText,
            Caption = f.Caption,
            CreatedDate = f.CreatedDate,
            IsActive = f.IsActive,
            FolderId = f.FolderId
        };

        /// <summary>
        /// 新增相片至 Cloudinary 並存入資料庫
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public async Task<AssetFileUploadDto> AddImages(AssetFileUploadDto uploadDto, CancellationToken ct = default)
        {
            try
            {
                var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

                if (uploadDto.Meta == null || uploadDto.Meta.Count == 0) return uploadDto;

                var now = DateTime.Now;
                var entities = new List<SysAssetFile>();

                foreach (var fileDto in uploadDto.Meta)
                {
                    var file = fileDto.File;
                    if (file == null || file.Length <= 0) continue;

                    // 改成 MemoryStream，避免 OpenReadStream 提早被 Dispose
                    await using var ms = new MemoryStream();
                    await file.CopyToAsync(ms, ct);
                    ms.Position = 0;

                    var extension = Path.GetExtension(file.FileName); // 例如 ".png"

                    // 產生唯一檔名：時間戳 + GUID
                    var uniqueName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";

                    var folderPath = $"{uploadDto.ModuleId}/{uploadDto.ProgId}/";

                    // PublicId → 存在 Cloudinary（不含副檔名，Cloudinary 會自動加回去）
                    var publicId = $"{Path.GetFileNameWithoutExtension(uniqueName)}";

                    // 1. 上傳到 Cloudinary
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, ms),
                        PublicId = publicId,    // 自訂，不用原始檔名
                        Folder = folderPath, // 資料夾 (模組 + 程式名稱)
                        UseFilename = false,    // 不要用原檔名
                        UniqueFilename = false, // 關掉 Cloudinary 自動亂改
                        Overwrite = false       // 保證不會覆蓋
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams, ct);

                    if (uploadResult == null || uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception($"Cloudinary 上傳失敗: {file.FileName}");
                    }

                    // 2. 收集 Entity（不要馬上 Add 到 _db）
                    entities.Add(new SysAssetFile
                    {
                        FileKey = uploadResult.PublicId,
                        IsExternal = true,  // Cloudinary 是外部空間
                        FileUrl = uploadResult.SecureUrl.ToString(), // HTTPS URL
                        FileExt = extension?.TrimStart('.'),
                        MimeType = file.ContentType,
                        Width = uploadResult.Width,
                        Height = uploadResult.Height,
                        FileSizeBytes = file.Length,
                        AltText = string.IsNullOrWhiteSpace(fileDto.AltText)
                            ? Path.GetFileNameWithoutExtension(uniqueName)
                            : fileDto.AltText,
                        Caption = string.IsNullOrWhiteSpace(fileDto.Caption)
                            ? $"上傳於 {now:yyyy-MM-dd HH:mm}"
                            : fileDto.Caption,
                        CreatedDate = now,
                        IsActive = fileDto.IsActive,
                    });

                    fileDto.FileUrl = uploadResult.SecureUrl.ToString();
                }

                // === 最後一次性寫入 DB ===
                if (entities.Count > 0)
                {
                    await _db.SysAssetFiles.AddRangeAsync(entities, ct);
                    var cc = await _db.SaveChangesAsync(ct);
                }

                return uploadDto;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        /// <summary>
        /// 修改圖片資訊
        /// </summary>
        public async Task<bool> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default)
        {
            try
            {
                var file = await _db.SysAssetFiles
                    .FirstOrDefaultAsync(f => f.FileId == dto.FileId, ct);

                if (file == null)
                    throw new Exception($"找不到 FileId={dto.FileId} 的圖片");

                file.AltText = dto.AltText;
                file.Caption = dto.Caption;
                file.IsActive = dto.IsActive;
                file.Width = dto.Width;
                file.Height = dto.Height;


                await _db.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                return false;
            }
        }

        /// <summary>
        /// 刪除圖片（同步刪除 Cloudinary 與 DB）
        /// </summary>
        public async Task<bool> DeleteImage(int fileId, CancellationToken ct = default)
        {
            try
            {
                var file = await _db.SysAssetFiles
                    .FirstOrDefaultAsync(f => f.FileId == fileId, ct);

                if (file == null)
                    throw new Exception($"找不到 FileId={fileId} 的圖片");

                // === 1️ 刪除 Cloudinary 檔案 ===
                if (!string.IsNullOrWhiteSpace(file.FileKey))
                {
                    try
                    {
                        var delParams = new DeletionParams(file.FileKey)
                        {
                            ResourceType = ResourceType.Image
                        };
                        var delResult = await _cloudinary.DestroyAsync(delParams);

                        if (delResult.Result != "ok" && delResult.Result != "not found")
                        {
                            Console.WriteLine($"⚠️ Cloudinary 刪除失敗：{file.FileKey}, Result={delResult.Result}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Cloudinary 連線異常：{ex.Message}");
                    }
                }

                // === 2️ 刪除資料庫記錄 ===
                _db.SysAssetFiles.Remove(file);
                await _db.SaveChangesAsync(ct);

                return true;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                return false;
            }
        }

        /// <summary>
        /// 批次刪除多筆圖片
        /// </summary>
        public async Task<int> DeleteImages(List<int> fileIds, CancellationToken ct = default)
        {
            int successCount = 0;
            foreach (var id in fileIds)
            {
                if (await DeleteImage(id, ct))
                    successCount++;
            }
            return successCount;
        }

		/// <summary>
		/// 新增資料夾
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		//[HttpPost]
		//public async Task<IActionResult> CreateFolder([FromBody] SysFolderDto dto)
		//{
		//	if (string.IsNullOrWhiteSpace(dto.FolderName))
		//		return Json(new { success = false, message = "請輸入資料夾名稱" });

		//	try
		//	{
		//		bool exists = await _db.SysFolders.AnyAsync(f => f.ParentId == dto.ParentId && f.FolderName == dto.FolderName);

		//		if (exists)
		//			return Json(new { success = false, message = "同一層已存在相同名稱的資料夾" });

		//		// 防止指向自己或循環
		//		if (dto.ParentId == dto.FolderId)
		//			return Json(new { success = false, message = "資料夾不能指向自己" });

		//		// 如果指定的父層不存在
		//		if (dto.ParentId.HasValue && !await _db.SysFolders.AnyAsync(f => f.FolderId == dto.ParentId))
		//			return Json(new { success = false, message = "找不到父層資料夾" });

		//		// 建立新資料夾
		//		var newFolder = new SysFolder
		//		{
		//			FolderName = dto.FolderName.Trim(),
		//			ParentId = dto.ParentId,
		//			IsActive = true,
		//		};

		//		_db.SysFolders.Add(newFolder);
		//		await _db.SaveChangesAsync();

		//		// 組出完整路徑
		//		string fullPath = await BuildFullPathAsync(newFolder.FolderId);

		//		return Json(new
		//		{
		//			success = true,
		//			message = "建立成功",
		//			data = new
		//			{
		//				newFolder.FolderId,
		//				newFolder.FolderName,
		//				newFolder.ParentId,
		//				fullPath
		//			}
		//		});
		//	}
		//	catch (Exception ex)
		//	{
		//		return new { success = false, message = ex.Message };
		//	}
		//}

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
		public async Task<dynamic> RenameFolder(SysFolderDto dto)
		{
			var folder = await _db.SysFolders.FindAsync(dto.FolderId);
			if (folder == null)
				return new { success = false, message = "找不到指定資料夾" };

			// 檢查同層重名
			bool duplicate = await _db.SysFolders.AnyAsync(f =>
				f.ParentId == folder.ParentId &&
				f.FolderId != folder.FolderId &&
				f.FolderName.ToLower() == dto.FolderName.ToLower());

			if (duplicate) return new { success = false, message = "同層已有相同名稱的資料夾" };

			folder.FolderName = dto.FolderName.Trim();
			await _db.SaveChangesAsync();

			return new { success = true, message = "資料夾名稱已更新" };
		}

		/// <summary>
		/// 取得所有資料夾
		/// </summary>
		/// <returns></returns>
		public async Task<dynamic> GetAllFolders()
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

				return new { success = true, data = result };
			}
			catch (Exception ex)
			{
				return new { success = false, message = ex.Message };
			}
		}

		/// <summary>
		/// 移動檔案或資料夾到指定資料夾
		/// </summary>
		/// <param name="dto"></param>
		/// <returns></returns>
		public async Task<string> MoveToFolder(MoveRequestDto dto)
		{
			try
			{
				// 1️ 如果 FolderId 是 null → 根目錄，允許通過
				SysFolder? targetFolder = null;
				if (dto.FolderId.HasValue)
				{
					targetFolder = await _db.SysFolders.FindAsync(dto.FolderId);
					if (targetFolder == null) return "找不到目標資料夾";
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
					if (movingIds.Contains(dto.FolderId.Value)) return "無法將資料夾移動到自己";
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
					if (movingIds.Contains(dto.FolderId.Value))	return "無法將資料夾移動到自己";

					// 2️ 檢查是否移到自己的子層底下
					var allFolders = await _db.SysFolders.ToListAsync();
					foreach (var movingFolder in folders)
					{
						if (IsDescendant(dto.FolderId.Value, movingFolder.FolderId, allFolders))
						{
							return $"無法將「{movingFolder.FolderName}」移到自己的子層";
						}
					}
				}

				await _db.SaveChangesAsync();

				var targetName = targetFolder?.FolderName ?? "根目錄";

				return string.Empty;
			}
			catch (Exception ex)
			{
				return $"伺服器錯誤：{ex.Message}";
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
		/// 清除 Cloudinary 上「資料庫未使用」的孤立檔案
		/// </summary>
		public async Task<(int totalChecked, int deletedCount, List<string> deletedKeys)> CleanOrphanCloudinaryFiles(CancellationToken ct = default)
		{
			var deletedKeys = new List<string>();
			int totalChecked = 0;
			int deletedCount = 0;

			try
			{
				// === 1. 取出目前資料庫中存在的 FileKey 清單 ===
				var dbKeys = await _db.SysAssetFiles
					.Select(f => f.FileKey)
					.ToListAsync(ct);

				var dbKeySet = new HashSet<string>(dbKeys);

				// === 2. 從 Cloudinary 分頁抓取所有圖片 ===
				string? nextCursor = null;

				do
				{
					var listParams = new ListResourcesParams
					{
						Type = "upload",
						ResourceType = ResourceType.Image,
						MaxResults = 500, // 每次最多500筆
						NextCursor = nextCursor
					};

					var listResult = await _cloudinary.ListResourcesAsync(listParams, ct);
					totalChecked += (listResult.Resources == null ? 0 : listResult.Resources.Count());

					foreach (var res in listResult.Resources)
					{
						var publicId = res.PublicId;

						// Cloudinary 上有，但 DB 沒有 → 要刪除
						if (!dbKeySet.Contains(publicId))
						{
							try
							{
								var delParams = new DeletionParams(publicId)
								{
									ResourceType = ResourceType.Image
								};
								var delResult = await _cloudinary.DestroyAsync(delParams);

								if (delResult.Result == "ok" || delResult.Result == "not found")
								{
									deletedKeys.Add(publicId);
									deletedCount++;
									Console.WriteLine($"已刪除未使用檔案: {publicId}");
								}
								else
								{
									Console.WriteLine($"刪除失敗: {publicId}, result={delResult.Result}");
								}
							}
							catch (Exception ex)
							{
								Console.WriteLine($"刪除 {publicId} 失敗: {ex.Message}");
							}
						}
					}

					nextCursor = listResult.NextCursor;
				} while (!string.IsNullOrEmpty(nextCursor));

				Console.WriteLine($"清理完成，共檢查 {totalChecked} 筆，刪除 {deletedCount} 筆孤立檔案。");

				return (totalChecked, deletedCount, deletedKeys);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"清理失敗: {ex.Message}");
				throw;
			}
		}
	}
}
