using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.SYS;

namespace tHerdBackend.Core.Interfaces.SYS
{
    /// <summary>
    /// 系統資產（圖片與資料夾）服務層介面
    /// </summary>
    public interface ISysAssetFileService
    {
        // ============================================================
        //  檔案 / 圖片操作
        // ============================================================

        Task<object> AddImages(AssetFileUploadDto uploadDto, CancellationToken ct = default);
        Task<PagedResult<SysAssetFileDto>> GetPagedFilesAsync(ImageFilterQueryDto query, CancellationToken ct = default);
        Task<List<SysAssetFileDto>> GetFilesByProg(string moduleId, string progId, CancellationToken ct = default);
        Task<SysAssetFileDto?> GetFilesById(int id, CancellationToken ct = default);
        Task<object> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default);
        Task<bool> UpdateFileMetaField(FileMetaUpdateDto model);
        Task<object> DeleteImage(int fileId, CancellationToken ct = default);
        Task<object> DeleteImages(List<int> ids, CancellationToken ct = default);
        Task<object> BatchDelete(List<int> ids, CancellationToken ct = default);
        Task<object> BatchSetActive(BatchActiveRequest req);
        Task<object> CleanOrphanCloudinaryFiles(CancellationToken ct = default);

        // ============================================================
        //  資料夾操作
        // ============================================================

        Task<object> CreateFolderAsync(string folderName, int? parentId);
        Task<object> RenameFolder(SysFolderDto dto);
        Task<object> DeleteFolder(int folderId);
        Task<List<SysFolderDto>> GetSubFoldersAsync(int? parentId);
        Task<object> GetTreeData();
        Task<object> GetAllFolders();
        Task<object> MoveToFolder(MoveRequestDto dto);

        // ============================================================
        //  檔案總管整合查詢
        // ============================================================

        Task<object> GetPagedFolderItems(
            int? parentId = null,
            string? keyword = "",
            int? start = null,
            int? length = null,
            int draw = 1,
            string? orderColumn = "Name",
            string? orderDir = "asc"
        );

        Task<object> GetBreadcrumbPath(int folderId);
    }
}