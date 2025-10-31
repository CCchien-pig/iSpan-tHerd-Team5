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
using tHerdBackend.Infra.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace tHerdBackend.Infra.Repository.SYS
{
    //圖片模組完整邏輯
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

        // 共用 Entity → DTO 投影
        private static readonly Expression<Func<SysAssetFile, SysAssetFileDto>> ToDto = f => new SysAssetFileDto
        {
            FileId = f.FileId,
            FileKey = f.FileKey,
            FileUrl = f.FileUrl,
            FileExt = f.FileExt,
            MimeType = f.MimeType,
            Width = f.Width,
            Height = f.Height,
            FileSizeBytes = f.FileSizeBytes,
            AltText = f.AltText,
            Caption = f.Caption,
            CreatedDate = f.CreatedDate,
            IsExternal = f.IsExternal,
            IsActive = f.IsActive,
            FolderId = f.FolderId
        };

        #region === 本地上傳（自動建立 Folder 結構） ===
        public async Task AddLocalFileAsync(
            AssetFileUploadDto uploadDto,
            AssetFileDetailsDto meta,
            string fileUrl,
            CancellationToken ct = default)
        {
            var now = DateTime.Now;
            var fileName = Path.GetFileName(fileUrl);
            var fileExt = Path.GetExtension(fileName)?.TrimStart('.');

            // === 1️ 自動建立資料夾結構 ===
            var folderId = await EnsureFolderHierarchy(uploadDto.ModuleId, uploadDto.ProgId, ct);
            uploadDto.FolderId = folderId;

            // === 2️ 建立實體路徑 ===
            var folderPath = Path.Combine("wwwroot", "Uploads", uploadDto.ModuleId, uploadDto.ProgId);
            Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), folderPath, fileName);

            // === 3️ 自動偵測圖片尺寸（僅限 image/*） ===
            int width = 0, height = 0;
            try
            {
                if (meta.File != null && meta.File.ContentType.StartsWith("image/") && File.Exists(fullPath))
                {
                    using var image = await Image.LoadAsync<Rgba32>(fullPath, ct);
                    width = image.Width;
                    height = image.Height;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 無法讀取圖片尺寸：{fileUrl}，原因：{ex.Message}");
            }

            // === 4️ 寫入資料庫 ===
            var entity = new SysAssetFile
            {
                FileKey = $"{uploadDto.ModuleId}/{uploadDto.ProgId}/{fileName}",
                IsExternal = false,
                FileUrl = $"/uploads/{uploadDto.ModuleId}/{uploadDto.ProgId}/{fileName}",
                FileExt = fileExt,
                MimeType = meta.File?.ContentType ?? "image/*",
                Width = width,
                Height = height,
                FileSizeBytes = meta.File?.Length ?? 0,
                AltText = string.IsNullOrWhiteSpace(meta.AltText)
                    ? Path.GetFileNameWithoutExtension(fileName)
                    : meta.AltText,
                Caption = string.IsNullOrWhiteSpace(meta.Caption)
                    ? $"上傳於 {now:yyyy-MM-dd HH:mm}"
                    : meta.Caption,
                CreatedDate = now,
                IsActive = meta.IsActive,
                FolderId = uploadDto.FolderId,
            };

            await _db.SysAssetFiles.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// 自動檢查並建立 Folder 階層（例如 PROD / ProductEdit）
        /// </summary>
        private async Task<int?> EnsureFolderHierarchy(string moduleId, string progId, CancellationToken ct)
        {
            // === 找或建第一層 (ModuleId) ===
            var moduleFolder = await _db.SysFolders
                .FirstOrDefaultAsync(f => f.ParentId == null && f.FolderName == moduleId, ct);

            if (moduleFolder == null)
            {
                moduleFolder = new SysFolder
                {
                    FolderName = moduleId,
                    ParentId = null,
                    IsActive = true
                };
                _db.SysFolders.Add(moduleFolder);
                await _db.SaveChangesAsync(ct);
            }

            // === 找或建第二層 (ProgId) ===
            var progFolder = await _db.SysFolders
                .FirstOrDefaultAsync(f => f.ParentId == moduleFolder.FolderId && f.FolderName == progId, ct);

            if (progFolder == null)
            {
                progFolder = new SysFolder
                {
                    FolderName = progId,
                    ParentId = moduleFolder.FolderId,
                    IsActive = true
                };
                _db.SysFolders.Add(progFolder);
                await _db.SaveChangesAsync(ct);
            }

            return progFolder.FolderId;
        }
        #endregion

        #region === Cloudinary 上傳 ===
        /// <summary>
        /// 上傳檔案到 Cloudinary 並寫入資料庫
        /// </summary>
        public async Task<object> AddFilesAsync(AssetFileUploadDto uploadDto, CancellationToken ct = default)
        {
            if (uploadDto.Meta == null || uploadDto.Meta.Count == 0)
                return new { success = false, message = "沒有上傳內容" };

            // === 常數設定 ===
            const long CHUNK_THRESHOLD = 30 * 1024 * 1024; // 超過 100MB 啟用分段
            const int CHUNK_SIZE = 20 * 1024 * 1024;        // 每段 20MB

            // === 建立資料夾階層（例如 PROD/ProductEdit） ===
            var folderId = await EnsureFolderHierarchy(uploadDto.ModuleId, uploadDto.ProgId, ct);
            uploadDto.FolderId = folderId;

            var now = DateTime.Now;
            var entities = new List<SysAssetFile>();

            foreach (var meta in uploadDto.Meta)
            {
                var file = meta.File;
                if (file == null || file.Length == 0)
                    continue;

                Console.WriteLine($"➡️ 開始上傳: {file.FileName} ({file.Length / 1024 / 1024:F2} MB)");

                await using var ms = new MemoryStream();
                await file.CopyToAsync(ms, ct);
                ms.Position = 0;

                var ext = Path.GetExtension(file.FileName);
                var unique = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
                var publicId = Path.GetFileNameWithoutExtension(unique);
                var folderPath = $"{uploadDto.ModuleId}/{uploadDto.ProgId}/";

                var resourceType = GetCloudinaryResourceType(file.ContentType);
                string? resultUrl = null;
                int width = 0, height = 0;

                try
                {
                    UploadResult? result = null;

                    switch (resourceType)
                    {
                        // 🖼️ 圖片上傳
                        case ResourceType.Image:
                            if (file.Length > CHUNK_THRESHOLD)
                            {
                                Console.WriteLine($"[Chunked] 圖片分段上傳: {file.FileName}");
                                var imgParams = new ImageUploadParams
                                {
                                    File = new FileDescription(file.FileName, ms),
                                    Folder = folderPath,
                                    PublicId = publicId
                                };
                                result = await _cloudinary.UploadLargeAsync(imgParams, CHUNK_SIZE, null);
                            }
                            else
                            {
                                var imgParams = new ImageUploadParams
                                {
                                    File = new FileDescription(file.FileName, ms),
                                    Folder = folderPath,
                                    PublicId = publicId
                                };
                                result = await _cloudinary.UploadAsync(imgParams, null);
                            }

                            if (result is ImageUploadResult imgResult)
                            {
                                resultUrl = imgResult.SecureUrl?.ToString();
                                width = imgResult.Width;
                                height = imgResult.Height;
                            }
                            break;

                        // 🎬 影片上傳
                        case ResourceType.Video:
                            if (file.Length > CHUNK_THRESHOLD)
                            {
                                Console.WriteLine($"[Chunked] 影片分段上傳: {file.FileName}");
                                var videoParams = new VideoUploadParams
                                {
                                    File = new FileDescription(file.FileName, ms),
                                    Folder = folderPath,
                                    PublicId = publicId
                                };
                                result = await _cloudinary.UploadLargeAsync(videoParams, CHUNK_SIZE, null);
                            }
                            else
                            {
                                var videoParams = new VideoUploadParams
                                {
                                    File = new FileDescription(file.FileName, ms),
                                    Folder = folderPath,
                                    PublicId = publicId
                                };
                                result = await _cloudinary.UploadAsync(videoParams, null);
                            }

                            if (result is VideoUploadResult videoResult)
                            {
                                resultUrl = videoResult.SecureUrl?.ToString();
                                width = videoResult.Width;
                                height = videoResult.Height;
                            }
                            break;

                        // 📦 其他 (raw / zip / pdf / log)
                        default:
                            if (file.Length > CHUNK_THRESHOLD)
                            {
                                Console.WriteLine($"[Chunked] RAW 分段上傳: {file.FileName}");
                                ms.Position = 0;

                                // 注意：BasicRawUploadParams 沒有 Folder，用 PublicId 模擬階層
                                var rawParams = new BasicRawUploadParams
                                {
                                    File = new FileDescription(file.FileName, ms),
                                    PublicId = $"{folderPath}{publicId}" // ← 把 folderPath 直接放進來
                                };

                                var rawLarge = await _cloudinary.UploadLargeRawAsync(rawParams, CHUNK_SIZE, null);
                                resultUrl = rawLarge.SecureUrl?.ToString();
                            }
                            else
                            {
                                var rawParams = new RawUploadParams
                                {
                                    File = new FileDescription(file.FileName, ms),
                                    Folder = folderPath,
                                    PublicId = publicId
                                };
                                var rawResult = await _cloudinary.UploadAsync(rawParams, type: "raw", cancellationToken: null);
                                resultUrl = rawResult.SecureUrl?.ToString();
                            }
                            width = 0;
                            height = 0;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Cloudinary 上傳失敗: {file.FileName}, {ex.Message}");
                    continue; // 跳過這筆檔案，不寫入 DB
                }

                // === 寫入 DB 實體 ===
                var entity = new SysAssetFile
                {
                    FileKey = publicId,
                    IsExternal = true,
                    FileUrl = resultUrl ?? "",
                    FileExt = ext.TrimStart('.'),
                    MimeType = file.ContentType,
                    Width = width,
                    Height = height,
                    FileSizeBytes = file.Length,
                    AltText = string.IsNullOrWhiteSpace(meta.AltText)
                        ? Path.GetFileNameWithoutExtension(unique)
                        : meta.AltText,
                    Caption = string.IsNullOrWhiteSpace(meta.Caption)
                        ? $"上傳於 {now:yyyy-MM-dd HH:mm}"
                        : meta.Caption,
                    CreatedDate = now,
                    IsActive = meta.IsActive,
                    FolderId = uploadDto.FolderId
                };

                entities.Add(entity);
                Console.WriteLine($"✅ 上傳完成: {file.FileName}");
            }

            if (entities.Any())
            {
                await _db.SysAssetFiles.AddRangeAsync(entities, ct);
                await _db.SaveChangesAsync(ct);
            }

            return new
            {
                success = true,
                message = $"成功上傳 {entities.Count} 筆檔案",
                data = entities.Select(e => new
                {
                    e.FileId,
                    e.FileUrl,
                    e.MimeType,
                    e.FileKey,
                    e.AltText,
                    e.Caption,
                    e.Width,
                    e.Height,
                    e.FileSizeBytes,
                    e.FolderId
                })
            };
        }


        /// <summary>
        /// 根據 MIME 類型回傳 Cloudinary ResourceType
        /// </summary>
        private static ResourceType GetCloudinaryResourceType(string mimeType)
        {
            if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Image;
            if (mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Video;
            return ResourceType.Raw; // 其他都歸類為一般檔案（包含 PDF）
        }
        #endregion

        #region === 刪除（含雲端同步） ===
        /// <summary>
        /// 真正呼叫 Cloudinary API 刪除檔案的小工具
        /// </summary>
        private async Task<bool> DeleteCloudinaryAsync(string fileKey, ResourceType type = ResourceType.Image)
        {
            if (string.IsNullOrWhiteSpace(fileKey))
                return false;

            try
            {
                var del = await _cloudinary.DestroyAsync(new DeletionParams(fileKey)
                {
                    ResourceType = type
                });

                return del.Result == "ok" || del.Result == "not found";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cloudinary 刪除失敗：{fileKey}，原因：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 刪除圖片（同步刪除 Cloudinary 與 DB）
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<object> DeleteImage(int fileId, CancellationToken ct = default)
        {
            var file = await _db.SysAssetFiles
                .FirstOrDefaultAsync(f => f.FileId == fileId, ct);

            if (file == null)
                return new { success = false, message = "找不到指定檔案。" };

            file.IsDeleted = true;
            file.IsActive = false;

            _db.SysAssetFiles.Update(file);
            await _db.SaveChangesAsync(ct);

            return new { success = true, message = "已軟刪除該圖片。" };
        }

        /// <summary>
        /// 刪除多個圖片（同步刪除 Cloudinary 與 DB）
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<object> DeleteImages(List<int> ids, CancellationToken ct = default)
        {
            var files = await _db.SysAssetFiles
                .Where(f => ids.Contains(f.FileId) && !f.IsDeleted)
                .ToListAsync(ct);

            int cloudDeleted = 0;

            foreach (var file in files)
            {
                file.IsDeleted = true;
                file.IsActive = false;
                _db.SysAssetFiles.Update(file);
            }

            await _db.SaveChangesAsync(ct);

            return new
            {
                success = true,
                message = "批次刪除完成",
                data = new
                {
                    total = files.Count,
                    cloudDeleted
                }
            };
        }
        #endregion

        #region === Cloudinary 清理孤檔 ===
        public async Task<object> CleanOrphanCloudinaryFiles(CancellationToken ct = default)
        {
            var dbKeys = new HashSet<string>(
                await _db.SysAssetFiles
                    .Select(f => f.FileKey)
                    .ToListAsync(ct)
            );

            var deletedKeys = new List<string>();
            int totalChecked = 0;
            int deletedCount = 0;
            string? cursor = null;

            do
            {
                var listResult = await _cloudinary.ListResourcesAsync(new ListResourcesParams
                {
                    Type = "upload",
                    ResourceType = ResourceType.Image,
                    MaxResults = 500,
                    NextCursor = cursor
                }, ct);

                var resources = listResult.Resources ?? Enumerable.Empty<Resource>();
                totalChecked += resources.Count();

                foreach (var res in resources)
                {
                    var publicId = res.PublicId;
                    if (!dbKeys.Contains(publicId))
                    {
                        // DB 沒有，但 Cloudinary 有 -> 刪掉
                        var delParams = new DeletionParams(publicId)
                        {
                            ResourceType = ResourceType.Image
                        };
                        var delResult = await _cloudinary.DestroyAsync(delParams);

                        if (delResult.Result == "ok" || delResult.Result == "not found")
                        {
                            deletedKeys.Add(publicId);
                            deletedCount++;
                        }
                    }
                }

                cursor = listResult.NextCursor;
            }
            while (!string.IsNullOrEmpty(cursor));

            return new
            {
                success = true,
                message = "孤立檔案清理完成",
                data = new
                {
                    totalChecked,
                    deletedCount,
                    deletedKeys
                }
            };
        }
        #endregion

        #region === 資料夾 CRUD ===
        /// <summary>
        /// 建立資料夾
        /// </summary>
        /// <summary>
        /// 建立資料夾
        /// </summary>
        public async Task<object> CreateFolderAsync(string folderName, int? parentId)
        {
            // 驗證同層重名
            bool duplicate = await _db.SysFolders
                .AnyAsync(f => f.ParentId == parentId && f.FolderName == folderName);

            if (duplicate)
            {
                return new
                {
                    success = false,
                    message = "同一層已存在相同名稱的資料夾"
                };
            }

            // 驗證 parent 是否存在（如果有傳 parentId）
            if (parentId.HasValue)
            {
                bool parentExists = await _db.SysFolders.AnyAsync(f => f.FolderId == parentId.Value);
                if (!parentExists)
                {
                    return new
                    {
                        success = false,
                        message = "找不到父層資料夾"
                    };
                }
            }

            var folder = new SysFolder
            {
                FolderName = folderName.Trim(),
                ParentId = parentId,
                IsActive = true
            };

            _db.SysFolders.Add(folder);
            await _db.SaveChangesAsync();

            // 組完整路徑
            string fullPath = await BuildFullPathAsync(folder.FolderId);

            return new
            {
                success = true,
                message = "建立成功",
                data = new
                {
                    folder.FolderId,
                    folder.FolderName,
                    folder.ParentId,
                    FullPath = fullPath
                }
            };
        }

        /// <summary>
        /// 取得子資料夾列表
        /// </summary>
        public async Task<List<SysFolderDto>> GetSubFoldersAsync(int? parentId)
        {
            return await _db.SysFolders
                .Where(f => f.IsActive && f.ParentId == parentId)
                .OrderBy(f => f.FolderName)
                .Select(f => new SysFolderDto
                {
                    FolderId = f.FolderId,
                    FolderName = f.FolderName,
                    ParentId = f.ParentId
                })
                .ToListAsync();
        }

        /// <summary>
        /// 重新命名資料夾
        /// </summary>
        public async Task<object> RenameFolder(SysFolderDto dto)
        {
            var folder = await _db.SysFolders.FindAsync(dto.FolderId);
            if (folder == null)
            {
                return new { success = false, message = "找不到指定資料夾" };
            }

            bool duplicate = await _db.SysFolders.AnyAsync(f =>
                f.ParentId == folder.ParentId &&
                f.FolderId != folder.FolderId &&
                f.FolderName.ToLower() == dto.FolderName.ToLower());

            if (duplicate)
            {
                return new
                {
                    success = false,
                    message = "同層已有相同名稱的資料夾"
                };
            }

            folder.FolderName = dto.FolderName.Trim();
            await _db.SaveChangesAsync();

            return new { success = true, message = "資料夾名稱已更新" };
        }

        /// <summary>
        /// 移動資料夾 / 檔案
        /// </summary>
        public async Task<object> MoveToFolder(MoveRequestDto dto)
        {
            // 1. 確認目標資料夾（null = 根目錄）
            SysFolder? targetFolder = null;
            if (dto.FolderId.HasValue)
            {
                targetFolder = await _db.SysFolders.FindAsync(dto.FolderId);
                if (targetFolder == null)
                {
                    return new
                    {
                        success = false,
                        message = "找不到目標資料夾"
                    };
                }
            }

            // 2. 找出待移動的檔案與資料夾
            var files = await _db.SysAssetFiles
                .Where(f => dto.Ids.Contains(f.FileId))
                .ToListAsync();

            var folders = await _db.SysFolders
                .Where(f => dto.Ids.Contains(f.FolderId))
                .ToListAsync();

            // 3. 防呆：不能把資料夾移到自己裡面
            if (dto.FolderId != null)
            {
                var movingIds = folders.Select(f => f.FolderId).ToHashSet();
                if (movingIds.Contains(dto.FolderId.Value))
                {
                    return new
                    {
                        success = false,
                        message = "無法將資料夾移動到自己"
                    };
                }
            }

            // 4. 防呆：不能把資料夾移到自己的子層
            if (dto.FolderId != null && folders.Any())
            {
                var allFolders = await _db.SysFolders.ToListAsync();

                foreach (var movingFolder in folders)
                {
                    if (IsDescendant(dto.FolderId.Value, movingFolder.FolderId, allFolders))
                    {
                        return new
                        {
                            success = false,
                            message = $"無法將「{movingFolder.FolderName}」移到自己的子層"
                        };
                    }
                }
            }

            // 5. 實際更新
            foreach (var f in files)
            {
                f.FolderId = dto.FolderId; // null 代表移到根目錄
            }

            foreach (var folder in folders)
            {
                folder.ParentId = dto.FolderId;
            }

            await _db.SaveChangesAsync();

            return new
            {
                success = true,
                message = "移動完成",
                data = new
                {
                    targetFolderId = dto.FolderId,
                    targetFolderName = targetFolder?.FolderName ?? "根目錄"
                }
            };
        }

        /// <summary>
        /// 判斷 targetId 是否是 sourceId 的子孫層級（用來防止把資料夾塞到自己的子層）
        /// </summary>
        private bool IsDescendant(int targetId, int sourceId, List<SysFolder> all)
        {
            var current = all.FirstOrDefault(f => f.FolderId == targetId);
            while (current != null)
            {
                if (current.ParentId == sourceId) return true;
                current = current.ParentId.HasValue
                    ? all.FirstOrDefault(f => f.FolderId == current.ParentId.Value)
                    : null;
            }
            return false;
        }

        /// <summary>
        /// 刪除資料夾（僅允許刪除空資料夾）
        /// 條件：
        /// 1. 不能有任何子資料夾
        /// 2. 不能有任何未刪除的檔案（IsDeleted = false）
        /// </summary>
        public async Task<object> DeleteFolder(int folderId)
        {
            // 1. 找資料夾本身
            var folder = await _db.SysFolders.FindAsync(folderId);
            if (folder == null)
            {
                return new { success = false, message = "找不到資料夾" };
            }

            // 2. 檢查是否有子資料夾
            bool hasChildFolders = await _db.SysFolders
                .AnyAsync(f => f.ParentId == folderId);

            if (hasChildFolders)
            {
                return new
                {
                    success = false,
                    message = $"「{folder.FolderName}」內仍有子資料夾，無法刪除。請先刪除子資料夾。"
                };
            }

            // 3. 檢查是否有檔案（還沒軟刪除的）
            bool hasFiles = await _db.SysAssetFiles
                .AnyAsync(f => f.FolderId == folderId && !f.IsDeleted);

            if (hasFiles)
            {
                return new
                {
                    success = false,
                    message = $"「{folder.FolderName}」內仍有檔案，無法刪除。請先刪除或移動檔案。"
                };
            }

            // 4. 通過檢查，允許刪除這個資料夾
            _db.SysFolders.Remove(folder);
            await _db.SaveChangesAsync();

            return new
            {
                success = true,
                message = $"已刪除資料夾「{folder.FolderName}」。"
            };
        }

        /// <summary>
        /// 真正刪除已標記刪除的檔案（含 Cloudinary 同步）
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<object> PurgeDeletedFiles(CancellationToken ct = default)
        {
            var deletedFiles = await _db.SysAssetFiles
                .Where(f => f.IsDeleted)
                .ToListAsync(ct);

            int cloudDeleted = 0;
            foreach (var f in deletedFiles)
            {
                if (f.IsExternal && !string.IsNullOrEmpty(f.FileKey))
                {
                    if (await DeleteCloudinaryAsync(f.FileKey))
                        cloudDeleted++;
                }
                else
                {
                    // ✅ 同步刪除本地檔案
                    var localPath = Path.Combine("wwwroot", f.FileKey.Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (File.Exists(localPath))
                    {
                        try { File.Delete(localPath); }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ 無法刪除本地檔案：{localPath}，原因：{ex.Message}");
                        }
                    }
                }
            }

            _db.SysAssetFiles.RemoveRange(deletedFiles);
            await _db.SaveChangesAsync(ct);

            return new
            {
                success = true,
                message = $"已永久刪除 {deletedFiles.Count} 筆（Cloudinary 清除 {cloudDeleted} 筆）"
            };
        }
        #endregion

        #region === 圖片管理後臺查詢 ===
        /// <summary>
        /// 後台圖片管理：用於分頁顯示圖片列表
        /// </summary>
        public async Task<PagedResult<SysAssetFileDto>> GetPagedFilesAsync(ImageFilterQueryDto query, CancellationToken ct = default)
        {
            var q = _db.SysAssetFiles.Where(f => !f.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                q = q.Where(f =>
                    f.AltText.Contains(query.Keyword) ||
                    f.Caption.Contains(query.Keyword));
            }

            var totalCount = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(f => f.CreatedDate)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToDto)
                .ToListAsync(ct);

            return new PagedResult<SysAssetFileDto>
            {
                TotalCount = totalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                Items = items
            };
        }
        #endregion

        #region === 其他查詢功能 ===
        /// <summary>
        /// 依模組與程式代號取得該資料夾底下的所有檔案
        /// 例如：moduleId = "PROD", progId = "ProductEdit"
        /// </summary>
        public async Task<List<SysAssetFileDto>> GetFilesByProg(string moduleId, string progId, CancellationToken ct = default)
        {
            // === 1️ 找到對應的模組資料夾 ===
            var moduleFolder = await _db.SysFolders
                .FirstOrDefaultAsync(f => f.ParentId == null && f.FolderName == moduleId, ct);

            if (moduleFolder == null)
            {
                // 模組不存在，直接回傳空集合
                return new List<SysAssetFileDto>();
            }

            // === 2️ 找到對應的程式資料夾（模組底下） ===
            var progFolder = await _db.SysFolders
                .FirstOrDefaultAsync(f => f.ParentId == moduleFolder.FolderId && f.FolderName == progId, ct);

            if (progFolder == null)
            {
                // 程式資料夾不存在
                return new List<SysAssetFileDto>();
            }

            // === 3️ 取得該資料夾底下的檔案 ===
            var files = await _db.SysAssetFiles
                .Where(f => f.FolderId == progFolder.FolderId && !f.IsDeleted)
                .OrderByDescending(f => f.CreatedDate)
                .Select(ToDto)
                .ToListAsync(ct);

            return files;
        }

        /// <summary>
        /// 依 FileId 取得單一檔案資訊
        /// </summary>
        public async Task<SysAssetFileDto?> GetFileById(int id, CancellationToken ct = default)
        {
            return await _db.SysAssetFiles
                .Where(f => !f.IsDeleted)
                .Where(f => f.FileId == id)
                .Select(ToDto)
                .FirstOrDefaultAsync(ct);
        }

        /// <summary>
        /// 回傳 jsTree 用的樹狀資料
        /// </summary>
        public async Task<object> GetTreeData()
        {
            var nodes = await _db.SysFolders
                .Where(f => f.IsActive)
                .Select(f => new
                {
                    id = f.FolderId.ToString(),
                    parent = f.ParentId == null ? "#" : f.ParentId.ToString(),
                    text = f.FolderName
                })
                .ToListAsync();

            return nodes;
        }

        /// <summary>
        /// 取得資料夾麵包屑路徑
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public async Task<dynamic> GetBreadcrumbPath(int folderId)
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

            return list;
        }

        /// <summary>
        /// 取得所有資料夾
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// 回傳所有資料夾清單（含 FullPath），可以拿來做「移動到...」下拉選單
        /// </summary>
        public async Task<object> GetAllFolders()
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

                var folderDict = folders.ToDictionary(f => f.FolderId);
                var result = new List<object>();

                foreach (var f in folders)
                {
                    var names = new List<string> { f.FolderName };
                    var parentId = f.ParentId;
                    var visited = new HashSet<int> { f.FolderId };

                    // 逐層往上組 FullPath，防止循環
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

                return new
                {
                    success = true,
                    message = "OK",
                    data = result
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        /// <summary>
        /// 取得指定資料夾底下的「子資料夾 + 檔案」清單，支援搜尋、排序、分頁
        /// 同時支援 DataTables (start/length/draw) 和一般模式
        /// </summary>
        public async Task<object> GetPagedFolderItems(
            int? parentId = null,
            string? keyword = "",
            int? start = null,
            int? length = null,
            int draw = 1,
            string? orderColumn = "Name",
            string? orderDir = "asc"
        )
        {
            // 1. 先找當層的資料夾
            var folderQuery = _db.SysFolders.AsQueryable();

            // 停用的資料夾一律顯示在根層
            if (parentId == null)
            {
                folderQuery = folderQuery.Where(f =>
                    !f.IsActive || f.ParentId == null);
            }
            else
            {
                folderQuery = folderQuery.Where(f =>
                    f.IsActive && f.ParentId == parentId);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                folderQuery = folderQuery.Where(f => f.FolderName.Contains(keyword));
            }

            var folders = await folderQuery
                .Select(f => new FolderItemDto
                {
                    Id = f.FolderId,
                    Name = f.FolderName,
                    IsFolder = true,
                    MimeType = "資料夾",
                    AltText = null,
                    Caption = null,
                    Url = string.Empty,
                    IsActive = true
                })
                .ToListAsync();

            // 2. 再找當層的檔案
            var fileQuery = _db.SysAssetFiles.Where(f => !f.IsDeleted);

            fileQuery = parentId == null
                ? fileQuery.Where(f => f.FolderId == null)
                : fileQuery.Where(f => f.FolderId == parentId);

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
                        : Path.Combine(
                            "/uploads",
                            // === 動態取得對應模組 / 程式資料夾 ===
                            _db.SysFolders
                                .Where(p2 => p2.FolderId == f.FolderId)
                                .Select(p2 => _db.SysFolders
                                    .Where(p1 => p1.FolderId == p2.ParentId)
                                    .Select(p1 => p1.FolderName + "/" + p2.FolderName)
                                    .FirstOrDefault())
                                .FirstOrDefault() ?? "",
                            Path.GetFileName(f.FileKey)
                          ).Replace("\\", "/"),
                    MimeType = f.MimeType,
                    IsFolder = false,
                    AltText = f.AltText,
                    Caption = f.Caption,
                    IsActive = f.IsActive
                })
                .ToListAsync();

            // 3. 合併
            var combined = folders.Concat(files).ToList();
            var totalCount = combined.Count;

            // 4. 排序
            bool desc = string.Equals(orderDir, "desc", StringComparison.OrdinalIgnoreCase);

            combined = orderColumn?.ToLower() switch
            {
                "modifieddate" => desc
                    ? combined.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.AltText).ToList()
                    : combined.OrderByDescending(x => x.IsFolder).ThenBy(x => x.AltText).ToList(),

                "size" => desc
                    ? combined.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.Caption).ToList()
                    : combined.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Caption).ToList(),

                "mimetype" => desc
                    ? combined.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.MimeType).ToList()
                    : combined.OrderByDescending(x => x.IsFolder).ThenBy(x => x.MimeType).ToList(),

                _ => desc
                    ? combined.OrderByDescending(x => x.IsFolder).ThenByDescending(x => x.Name).ToList()
                    : combined.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Name).ToList(),
            };

            // 5. 分頁
            List<FolderItemDto> paged;
            if (start.HasValue && length.HasValue && length > 0)
            {
                paged = combined.Skip(start.Value).Take(length.Value).ToList();
            }
            else
            {
                paged = combined; // 不分頁模式
            }

            // 6. 麵包屑
            var breadcrumb = await GetBreadcrumbAsync(parentId);

            // 7. 回傳格式
            if (start.HasValue && length.HasValue)
            {
                // DataTables 模式：不要多包 data = {}
                return new
                {
                    draw,
                    recordsTotal = totalCount,
                    recordsFiltered = totalCount,
                    data = paged,
                    breadcrumb
                };
            }
            else
            {
                // 一般模式
                return new
                {
                    success = true,
                    message = "OK",
                    data = new { items = paged, breadcrumb }
                };
            }
        }

        /// <summary>
        /// 產生麵包屑（由當前往上推到根）
        /// </summary>
        private async Task<List<SysFolderDto>> GetBreadcrumbAsync(int? folderId)
        {
            var breadcrumb = new List<SysFolderDto>();
            if (!folderId.HasValue) return breadcrumb;

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
        #endregion

        #region 修改 / 刪除圖片屬性
        /// <summary>
        /// 更新圖片的 AltText / Caption / 寬高 / 啟用狀態
        /// 對應「圖片資訊」Modal 的儲存
        /// </summary>
        public async Task<object> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default)
        {
            try
            {
                var file = await _db.SysAssetFiles
                    .FirstOrDefaultAsync(f => f.FileId == dto.FileId, ct);

                if (file == null)
                {
                    return new
                    {
                        success = false,
                        message = $"找不到 FileId={dto.FileId} 的圖片"
                    };
                }

                file.AltText = dto.AltText;
                file.Caption = dto.Caption;
                file.IsActive = dto.IsActive;
                file.Width = dto.Width;
                file.Height = dto.Height;

                await _db.SaveChangesAsync(ct);

                return new
                {
                    success = true,
                    message = "更新成功"
                };
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        /// <summary>
        /// 照片資訊即時更新
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> UpdateFileMetaField(FileMetaUpdateDto model)
        {
            var file = await _db.SysAssetFiles.FindAsync(model.FileId);
            if (file == null)
                throw new Exception($"找不到檔案 ID {model.FileId}");

            switch (model.Field?.ToLower())
            {
                case "alttext":
                    file.AltText = model.Value;
                    break;
                case "caption":
                    file.Caption = model.Value;
                    break;
                default:
                    throw new Exception($"不允許的欄位 {model.FileId}");
            }

            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 刪除多個檔案
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<object> BatchDelete(List<int> ids, CancellationToken ct = default)
        {
            try
            {
                // === 1. 找出要刪的檔案 ===
                var files = await _db.SysAssetFiles
                    .Where(f => ids.Contains(f.FileId) && !f.IsDeleted)
                    .ToListAsync(ct);

                if (files.Count == 0)
                {
                    return new
                    {
                        success = false,
                        message = "未找到可刪除的檔案。"
                    };
                }

                // === 2. 一律軟刪除 ===
                foreach (var file in files)
                {
                    file.IsDeleted = true;
                    file.IsActive = false;
                    _db.SysAssetFiles.Update(file);
                }

                await _db.SaveChangesAsync(ct);

                return new
                {
                    success = true,
                    message = $"已軟刪除 {files.Count} 筆圖片。",
                    data = new
                    {
                        softDeleted = files.Select(f => f.FileKey).ToList()
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ BatchDelete 發生例外: {ex}");
                return new
                {
                    success = false,
                    message = $"刪除時發生錯誤：{ex.Message}"
                };
            }
        }


        /// <summary>
        /// 批次啟用 / 停用 多張圖片
        /// </summary>
        public async Task<object> BatchSetActive(BatchActiveRequest req)
        {
            var files = await _db.SysAssetFiles
                .Where(f => req.Ids.Contains(f.FileId))
                .ToListAsync();

            foreach (var f in files)
            {
                f.IsActive = req.IsActive;
            }

            await _db.SaveChangesAsync();

            return new
            {
                success = true,
                message = "狀態已更新",
                data = new { count = files.Count }
            };
        }
        #endregion

        /// <summary>
        /// 產生完整路徑（/根/子層/...）
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
    }
}
