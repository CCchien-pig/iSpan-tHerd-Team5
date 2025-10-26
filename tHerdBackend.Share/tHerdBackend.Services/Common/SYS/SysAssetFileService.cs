using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.SYS;
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

        // ============================================================
        //  檔案 / 圖片：查詢、上傳、刪除、更新
        // ============================================================

        public Task<PagedResult<SysAssetFileDto>> GetPagedFilesAsync(ImageFilterQueryDto query, CancellationToken ct = default)
            => _frepo.GetPagedFilesAsync(query, ct);

        public Task<List<SysAssetFileDto>> GetFilesByProg(string moduleId, string progId, CancellationToken ct = default)
            => _frepo.GetFilesByProg(moduleId, progId, ct);

        public Task<SysAssetFileDto?> GetFilesById(int id, CancellationToken ct = default)
            => _frepo.GetFilesById(id, ct);

        public Task<object> AddImages(AssetFileUploadDto uploadDto, CancellationToken ct = default)
            => _frepo.AddImages(uploadDto, ct);

        public Task<object> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default)
        {
            if (dto == null)
                return Task.FromResult<object>(new { success = false, message = "空的輸入資料" });
            return _frepo.UpdateImageMeta(dto, ct);
        }

        public async Task<bool> UpdateFileMetaField(FileMetaUpdateDto model)
            => await _frepo.UpdateFileMetaField(model);

        public Task<object> DeleteImage(int fileId, CancellationToken ct = default)
            => _frepo.DeleteImage(fileId, ct);

        public Task<object> DeleteImages(List<int> ids, CancellationToken ct = default)
        {
            if (ids == null || ids.Count == 0)
                return Task.FromResult<object>(new { success = false, message = "未選取任何項目" });
            return _frepo.DeleteImages(ids, ct);
        }

        public Task<object> BatchDelete(List<int> ids, CancellationToken ct = default)
        {
            if (ids == null || ids.Count == 0)
                return Task.FromResult<object>(new { success = false, message = "未選取任何項目" });
            return _frepo.BatchDelete(ids, ct);
        }

        public Task<object> BatchSetActive(BatchActiveRequest req)
            => _frepo.BatchSetActive(req);

        public Task<object> CleanOrphanCloudinaryFiles(CancellationToken ct = default)
            => _frepo.CleanOrphanCloudinaryFiles(ct);

        // ============================================================
        //  資料夾：CRUD、移動、結構
        // ============================================================

        public Task<object> CreateFolderAsync(string folderName, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return Task.FromResult<object>(new { success = false, message = "請輸入資料夾名稱" });
            return _frepo.CreateFolderAsync(folderName, parentId);
        }

        public Task<object> RenameFolder(SysFolderDto dto)
        {
            if (dto == null || dto.FolderId <= 0)
                return Task.FromResult<object>(new { success = false, message = "資料夾編號無效" });
            if (string.IsNullOrWhiteSpace(dto.FolderName))
                return Task.FromResult<object>(new { success = false, message = "請輸入資料夾名稱" });

            return _frepo.RenameFolder(dto);
        }

        public Task<object> DeleteFolder(int folderId)
            => _frepo.DeleteFolder(folderId);

        public Task<List<SysFolderDto>> GetSubFoldersAsync(int? parentId)
            => _frepo.GetSubFoldersAsync(parentId);

        public Task<object> GetTreeData()
            => _frepo.GetTreeData();

        public Task<object> GetAllFolders()
            => _frepo.GetAllFolders();

        public Task<object> MoveToFolder(MoveRequestDto dto)
        {
            if (dto == null || dto.Ids == null || dto.Ids.Count == 0)
                return Task.FromResult<object>(new { success = false, message = "沒有選取項目" });

            if (dto.FolderId == 0)
                dto.FolderId = null;

            return _frepo.MoveToFolder(dto);
        }

        // ============================================================
        //  檔案總管用查詢 (資料夾 + 檔案 + 麵包屑)
        // ============================================================

        public Task<object> GetPagedFolderItems(
            int? parentId = null,
            string? keyword = "",
            int? start = null,
            int? length = null,
            int draw = 1,
            string? orderColumn = "Name",
            string? orderDir = "asc")
        {
            if (parentId == 0) parentId = null;
            return _frepo.GetPagedFolderItems(parentId, keyword, start, length, draw, orderColumn, orderDir);
        }

        public Task<object> GetBreadcrumbPath(int folderId)
            => _frepo.GetBreadcrumbPath(folderId);
    }
}
