using FlexBackend.Core.Interfaces.SYS;
using Microsoft.AspNetCore.Http;

namespace FlexBackend.Services.PROD
{
    public class SysAssetFileService : ISysAssetFileService
    {
        private readonly ISysAssetFileRepository _frepo;

        public SysAssetFileService(ISysAssetFileRepository frepo)
        {
            _frepo = frepo;
        }

        /// <summary>
        /// 新增相片至 Cloudinary 並存入資料庫
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public Task<bool> AddImages(List<IFormFile> files)
        {
            return _frepo.AddImages(files);
        }
    }
}
