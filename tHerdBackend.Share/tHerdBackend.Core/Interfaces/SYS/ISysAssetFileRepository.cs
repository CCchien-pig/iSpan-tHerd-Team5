using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.SYS;

namespace tHerdBackend.Core.Interfaces.SYS
{
    public interface ISysAssetFileRepository
    {
        // ============================================================
        //  Cloudinary 上傳 / 刪除 / 清理
        // ============================================================
        Task AddLocalFileAsync(AssetFileUploadDto uploadDto, AssetFileDetailsDto meta, string fileUrl, CancellationToken ct = default);
        Task<object> AddFilesAsync(AssetFileUploadDto uploadDto, CancellationToken ct = default);
        Task<object> DeleteImage(int fileId, CancellationToken ct = default);
        Task<object> DeleteImages(List<int> ids, CancellationToken ct = default);
        Task<object> CleanOrphanCloudinaryFiles(CancellationToken ct = default);

        // ============================================================
        //  檔案查詢 / 更新
        // ============================================================
        Task<PagedResult<SysAssetFileDto>> GetPagedFilesAsync(ImageFilterQueryDto query, CancellationToken ct = default);
        Task<List<SysAssetFileDto>> GetFilesByProg(string moduleId, string progId, CancellationToken ct = default);
        Task<SysAssetFileDto?> GetFileById(int id, CancellationToken ct = default);
        Task<object> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default);
        Task<bool> UpdateFileMetaField(FileMetaUpdateDto model);
        Task<object> BatchSetActive(BatchActiveRequest req);

        // ============================================================
        //  資料夾 CRUD / 結構操作
        // ============================================================
        Task<object> CreateFolderAsync(string folderName, int? parentId);
        Task<object> RenameFolder(SysFolderDto dto);
        Task<object> DeleteFolder(int folderId);
        Task<List<SysFolderDto>> GetSubFoldersAsync(int? parentId);
        Task<object> GetTreeData();
        Task<object> GetAllFolders();
        Task<object> MoveToFolder(MoveRequestDto dto);

        // ============================================================
        //  結構查詢（資料夾 + 檔案、麵包屑）
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

        // ============================================================
        //  批次刪除
        // ============================================================
        Task<object> BatchDelete(List<int> ids, CancellationToken ct = default);
    }
}
