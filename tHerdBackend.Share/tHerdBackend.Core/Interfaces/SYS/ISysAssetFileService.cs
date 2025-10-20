using tHerdBackend.Core.DTOs;

namespace tHerdBackend.Core.Interfaces.SYS
{
    public interface ISysAssetFileService
    {
        Task<AssetFileUploadDto> AddImages(AssetFileUploadDto uploadDto);
        Task<List<SysAssetFileDto>> GetFiles(string moduleId, string progId, CancellationToken ct = default);
        Task<bool> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default);
        Task<bool> DeleteImage(int fileId, CancellationToken ct = default);
    }
}
