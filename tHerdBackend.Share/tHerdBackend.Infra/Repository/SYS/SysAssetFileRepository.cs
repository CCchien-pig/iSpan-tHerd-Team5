using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
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
    }
}
