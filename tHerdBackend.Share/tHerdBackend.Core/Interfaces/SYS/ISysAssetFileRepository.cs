using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.SYS;

namespace tHerdBackend.Core.Interfaces.SYS
{
    public interface ISysAssetFileRepository
    {
        Task<AssetFileUploadDto> AddImages(AssetFileUploadDto uploadDto, CancellationToken ct = default);
        Task<List<SysAssetFileDto>> GetFilesByProg(string moduleId, string progId, CancellationToken ct = default);
        Task<PagedResult<SysAssetFileDto>> GetPagedFilesAsync(ImageFilterQueryDto query, CancellationToken ct = default);
        Task<bool> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default);
        Task<bool> DeleteImage(int fileId, CancellationToken ct = default);

        Task<List<SysFolderDto>> GetSubFoldersAsync(int? parentId);
        Task<SysFolderDto> CreateFolderAsync(string folderName, int? parentId);
    }
}
