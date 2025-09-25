using FlexBackend.Core.DTOs;

namespace FlexBackend.Core.Interfaces.SYS
{
    public interface ISysAssetFileService
    {
        Task<AssetFileUploadDto> AddImages(AssetFileUploadDto uploadDto);
        Task<List<SysAssetFileDto>> GetFiles(string moduleId, string progId, CancellationToken ct = default);
    }
}
