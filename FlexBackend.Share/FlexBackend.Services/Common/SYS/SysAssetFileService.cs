using FlexBackend.Core.DTOs;
using FlexBackend.Core.Interfaces.SYS;

namespace FlexBackend.Services.Common.SYS
{
    public class SysAssetFileService : ISysAssetFileService
    {
        private readonly ISysAssetFileRepository _frepo;

        public SysAssetFileService(ISysAssetFileRepository frepo)
        {
            _frepo = frepo;
        }

        public Task<List<SysAssetFileDto>> GetFiles(string moduleId, string progId, CancellationToken ct = default)
        {
            return _frepo.GetFiles(moduleId, progId, ct);
        }

        /// <summary>
        /// 新增相片至 Cloudinary 並存入資料庫
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public async Task<AssetFileUploadDto> AddImages(AssetFileUploadDto uploadDto)
        {
            return await _frepo.AddImages(uploadDto);
        }
    }
}
