using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.SUP
{
	public class BrandBannerRepository : IGenericContentRepository<BannerDto>
	{
		private readonly tHerdDBContext _context;

		public BrandBannerRepository(tHerdDBContext context)
		{
			_context = context;
		}

		public async Task<BannerDto?> GetByIdAsync(int fileId)
		{
			return await _context.SysAssetFiles
				.Where(f => f.FileId == fileId)
				.Select(f => new BannerDto
				{
					FileId = f.FileId,
					FileKey = f.FileKey,
					IsExternal = f.IsExternal,
					FileUrl = f.FileUrl,
					AltText = f.AltText,
					Caption = f.Caption,
					IsActive = f.IsActive
				})
				.FirstOrDefaultAsync();
		}

		public async Task<int> CreateAsync(BannerDto dto)
		{
			var newEntity = new SysAssetFile
			{
				// FileKey 應該由上傳服務生成，這裡假設 DTO 傳入
				FileKey = dto.FileKey ?? Guid.NewGuid().ToString(),
				IsExternal = dto.IsExternal,
				FileUrl = dto.FileUrl,
				AltText = dto.AltText,
				Caption = dto.Caption,
				IsActive = true, // 新增時預設為啟用
				CreatedDate = DateTime.Now
			};

			_context.SysAssetFiles.Add(newEntity);
			await _context.SaveChangesAsync();
			return newEntity.FileId;
		}

		public async Task UpdateAsync(BannerDto dto)
		{
			var entity = await _context.SysAssetFiles.FindAsync(dto.FileId);
			if (entity != null)
			{
				entity.AltText = dto.AltText;
				entity.Caption = dto.Caption;
				entity.FileUrl = dto.FileUrl; // 允許更新 URL
				entity.IsActive = dto.IsActive;

				await _context.SaveChangesAsync();
			}
		}
	}
}
