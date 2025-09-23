using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FlexBackend.Core.Exceptions;
using FlexBackend.Core.Interfaces.SYS;
using FlexBackend.Infra.DBSetting;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Http;

namespace FlexBackend.Infra.Repository.SYS
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
        /// 新增相片至 Cloudinary 並存入資料庫
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public async Task<bool> AddImages(List<IFormFile> files)
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return false;
                }

                foreach (var file in files)
                {
                    if (file.Length <= 0) continue;

                    // 改成 MemoryStream，避免 OpenReadStream 提早被 Dispose
                    await using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    ms.Position = 0;

                    // 1. 上傳到 Cloudinary
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, ms),
                        Folder = "prod_demo",
                        UseFilename = true,
                        UniqueFilename = false,
                        Overwrite = true
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult == null || uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception($"Cloudinary 上傳失敗: {file.FileName}");
                    }

                    // 2. 存入資料庫
                    var entity = new SysAssetFile
                    {
                        FileKey = uploadResult.PublicId, // ✅ 用 PublicId 較穩定
                        IsExternal = true,
                        FileUrl = uploadResult.SecureUrl.ToString(),
                        FileExt = Path.GetExtension(file.FileName)?.TrimStart('.'),
                        MimeType = file.ContentType,
                        Width = uploadResult.Width,
                        Height = uploadResult.Height,
                        FileSizeBytes = file.Length,
                        AltText = Path.GetFileNameWithoutExtension(file.FileName),
                        Caption = "上傳於 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };

                    _db.SysAssetFiles.Add(entity);
                }

                // 統一 SaveChanges
                var cc = await _db.SaveChangesAsync();
                return cc > 0;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }
    }
}
