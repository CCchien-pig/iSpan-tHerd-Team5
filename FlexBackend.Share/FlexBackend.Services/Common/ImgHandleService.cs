using FlexBackend.Core.Interfaces.Abstractions;
using Microsoft.AspNetCore.Http;

namespace FlexBackend.Services.Common
{
    /// <summary>
    /// 照片處理服務
    /// </summary>
    public class ImgHandleService
    {
        private readonly IImageStorage _imageStorage;

        public ImgHandleService(IImageStorage imageStorage)
        {
            _imageStorage = imageStorage;
        }

        public async Task<string> SaveProductImageAsync(IFormFile file)
        {
            // 可以先做驗證、壓縮、命名處理
            return await _imageStorage.UploadImageAsync(file, "products");
        }
    }
}
