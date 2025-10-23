using Microsoft.AspNetCore.Mvc;
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

        Task<SysAssetFileDto?> GetFilesById(int id, CancellationToken ct = default);

        Task<dynamic> RenameFolder([FromBody] SysFolderDto dto);

		Task<dynamic> GetAllFolders();

		Task<string> MoveToFolder(MoveRequestDto dto);

		Task<(int totalChecked, int deletedCount, List<string> deletedKeys)> CleanOrphanCloudinaryFiles(CancellationToken ct = default);
	}
}
