using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.SUP
{
	public class BrandAssetsRepository : IBrandAssetsRepository
	{
		private readonly tHerdDBContext _db;
		private readonly ISqlConnectionFactory _factory;

		public BrandAssetsRepository(tHerdDBContext db, ISqlConnectionFactory factory)
		{
			_db = db; _factory = factory;
		}

		// 右欄「內容圖片」：依 FolderId 與 AltText 查 SYS_AssetFile，簡單回傳 URL 清單（CreatedDate 排序）
		public async Task<List<string>> GetContentImagesAsync(int brandId, int folderId, string? altText, CancellationToken ct)
		{
			// 現階段：依 FolderId 與 AltText（品牌名）回傳所有啟用圖片 URL，CreatedDate 排序
			// 若之後需要以 ContentTitle 個別對應，再改這裡的條件
			var q = _db.Set<SysAssetFile>()
				.AsNoTracking()
				.Where(f => f.FolderId == folderId && f.IsActive == true);

			if (!string.IsNullOrWhiteSpace(altText))
				q = q.Where(f => f.AltText == altText);

			// 排序規則現在用 CreatedDate（可視你需求調整）
			var urls = await q.OrderBy(f => f.CreatedDate)
							  .Select(f => f.FileUrl)
							  .ToListAsync(ct);

			return urls;
		}
	}
}
