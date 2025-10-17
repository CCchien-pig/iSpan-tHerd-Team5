using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.Helpers
{
	public class GetAssetFiles
	{
		/// <summary>
		/// 根據 FileId 取出 [SYS_AssetFile] 資訊
		/// </summary>
		/// <param name="fileIds"></param>
		/// <param name="db"></param>
		/// <returns></returns>
		public async Task<List<SysAssetFileDto>> GetAssetFilesAsync(IEnumerable<int> fileIds, DbContext db)
		{
			return await db.Set<SysAssetFile>()
				.Where(f => fileIds.Contains(f.FileId) && f.IsActive)
				.Select(f => new SysAssetFileDto
				{
					FileId = f.FileId,
					FileKey = f.FileKey,
					IsExternal = f.IsExternal,
					FileUrl = f.FileUrl,
					FileExt = f.FileExt,
					MimeType = f.MimeType,
					Width = f.Width,
					Height = f.Height,
					FileSizeBytes = f.FileSizeBytes,
					AltText = f.AltText,
					Caption = f.Caption,
					CreatedDate = f.CreatedDate,
					IsActive = f.IsActive
				})
				.ToListAsync();
		}

	}
}
