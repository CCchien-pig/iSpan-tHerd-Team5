using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.Exceptions;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;

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
        /// 取得圖片
        /// </summary>
        /// <param name="moduleId"></param>
        /// <param name="progId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<SysAssetFileDto>> GetFiles(string moduleId, string progId, CancellationToken ct = default)
        {
            return await _db.SysAssetFiles
                .Where(f => f.FileKey.StartsWith($"{moduleId}/{progId}"))
                .OrderByDescending(f => f.CreatedDate)
                .Select(f => new SysAssetFileDto
                {
                    FileId = f.FileId,
                    FileKey = f.FileKey,
                    FileUrl = f.FileUrl,
                    AltText = f.AltText,
                    Caption = f.Caption,
                    IsActive = f.IsActive,
                    CreatedDate = f.CreatedDate
                })
                .ToListAsync(ct);
        }

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
                    var publicId = $"{folderPath}{Path.GetFileNameWithoutExtension(uniqueName)}";

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
                        IsActive = fileDto.IsActive
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
        /// 修改圖片中繼資料 (AltText, Caption, IsActive)
        /// </summary>
        public async Task<bool> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default)
        {
            try
            {
                var file = await _db.SysAssetFiles
                    .FirstOrDefaultAsync(f => f.FileId == dto.FileId, ct);

                if (file == null)
                    throw new Exception($"找不到 FileId={dto.FileId} 的圖片");

                // 修改中繼資料
                file.AltText = dto.AltText ?? file.AltText;
                file.Caption = dto.Caption ?? file.Caption;
                file.IsActive = dto.IsActive;

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
