using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.Interfaces.SYS;

namespace tHerdBackend.Services.Common.SYS
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

        public async Task<bool> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default)
        {
            return await _frepo.UpdateImageMeta(dto);
        }

        public async Task<bool> DeleteImage(int fileId, CancellationToken ct = default)
        {
            return await _frepo.DeleteImage(fileId);
        }
    }
}
