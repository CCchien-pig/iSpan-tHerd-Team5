using Microsoft.AspNetCore.Mvc;
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

        /// <summary>
        /// 圖片管理用: 取得取得所有分頁圖片檔案清單
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<PagedResult<SysAssetFileDto>> GetPagedFilesAsync(ImageFilterQueryDto query, CancellationToken ct = default)
        {
            return _frepo.GetPagedFilesAsync(query, ct);
        }

        /// <summary>
        /// 依模組與程式取得圖片檔案
        /// </summary>
        /// <param name="moduleId">模組代號</param>
        /// <param name="progId">程式代號</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<List<SysAssetFileDto>> GetFilesByProg(string moduleId, string progId, CancellationToken ct = default)
        {
            return _frepo.GetFilesByProg(moduleId, progId, ct);
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

        /// <summary>
        /// 更新圖片檔案的描述資訊
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> UpdateImageMeta(SysAssetFileDto dto, CancellationToken ct = default)
        {
            if (dto == null) throw new Exception($"空的輸入資料");

            return await _frepo.UpdateImageMeta(dto);
        }

        /// <summary>
        /// 刪除圖片檔案
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> DeleteImage(int fileId, CancellationToken ct = default)
        {
            return await _frepo.DeleteImage(fileId);
        }

        /// <summary>
        /// 取得圖片詳細資料
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<SysAssetFileDto?> GetFilesById(int id, CancellationToken ct = default)
        {
            return await _frepo.GetFilesById(id);
        }

        /// <summary>
        /// 建立資料夾
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public async Task<SysFolderDto> CreateFolderAsync(string folderName, int? parentId)
        {
            return await _frepo.CreateFolderAsync(folderName, parentId);
        }

		/// <summary>
		/// 重新命名資料夾
		/// </summary>
		/// <param name="dto"></param>
		/// <returns></returns>
		public async Task<dynamic> RenameFolder(SysFolderDto dto)
		{
			if (dto == null || dto.FolderId <= 0)
				return new { success = false, message = "資料夾編號無效" };

			if (string.IsNullOrWhiteSpace(dto.FolderName))
				return new { success = false, message = "請輸入資料夾名稱" };

			return await _frepo.RenameFolder(dto);
		}

		/// <summary>
		/// 取得所有資料夾結構
		/// </summary>
		/// <returns></returns>
		public async Task<dynamic> GetAllFolders()
		{
			return await _frepo.GetAllFolders();
		}

		/// <summary>
		/// 移動檔案到指定資料夾
		/// </summary>
		/// <param name="dto"></param>
		/// <returns></returns>
		public async Task<string> MoveToFolder(MoveRequestDto dto)
		{
			if (dto == null || dto.Ids == null || dto.Ids.Count == 0) return "沒有選取項目";

			if (dto.FolderId == 0) dto.FolderId = null;

			return await _frepo.MoveToFolder(dto);
		}

		/// <summary>
		/// 清除雲端孤兒檔案
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<(int totalChecked, int deletedCount, List<string> deletedKeys)> CleanOrphanCloudinaryFiles(CancellationToken ct = default)
		{
			return await _frepo.CleanOrphanCloudinaryFiles(ct);
		}
	}
}
