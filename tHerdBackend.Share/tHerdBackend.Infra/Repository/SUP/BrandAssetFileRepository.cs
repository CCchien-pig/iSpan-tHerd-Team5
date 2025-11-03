using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

public sealed class BrandAssetFileRepository : IBrandAssetFileRepository
{
	private readonly tHerdDBContext _db; 

	public BrandAssetFileRepository(tHerdDBContext db)
	{
		_db = db;
	}

	public async Task<IReadOnlyList<BrandLogoAssetDto>> GetActiveBrandLogosAsync(int folderId = 56)
	{
		// EF Core 版本（假設已將 SYS_AssetFile 映射到 DbContext）
		return await _db.SysAssetFiles
			.Where(f => f.FolderId == folderId && f.IsActive)
			.Select(f => new BrandLogoAssetDto
			{
				FileId = f.FileId,
				AltText = f.AltText,
				FileUrl = f.FileUrl,
				CreatedDate = f.CreatedDate
			})
			.ToListAsync();
	}
}
