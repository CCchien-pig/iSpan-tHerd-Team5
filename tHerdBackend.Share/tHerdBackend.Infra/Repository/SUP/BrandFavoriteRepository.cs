using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.SUP
{
	/// <summary>
	/// 品牌收藏資料存取實作（內部使用 EF 實體，介面只暴露 DTO）
	/// </summary>
	public class BrandFavoriteRepository : IBrandFavoriteRepository
	{
		private readonly tHerdDBContext _db;

		public BrandFavoriteRepository(tHerdDBContext db)
		{
			_db = db;
		}

		/// <summary>確認是否已收藏</summary>
		public async Task<bool> ExistsAsync(int userNumberId, int brandId, CancellationToken ct = default)
		{
			return await _db.Set<SupBrandFavorite>()
				.AnyAsync(x => x.UserNumberId == userNumberId && x.BrandId == brandId, ct);
		}

		/// <summary>新增收藏（由 DTO 轉成實體）</summary>
		public async Task AddAsync(BrandFavoriteRequestDto dto, CancellationToken ct = default)
		{
			// 併發下請同時確保資料庫唯一鍵：UQ(UserNumberId, BrandId)
			var entity = new SupBrandFavorite
			{
				UserNumberId = dto.UserNumberId,
				BrandId = dto.BrandId,
				CreatedDate = DateTime.UtcNow
			};
			await _db.Set<SupBrandFavorite>().AddAsync(entity, ct);
			await _db.SaveChangesAsync(ct);
		}

		/// <summary>取消收藏</summary>
		public async Task RemoveAsync(BrandFavoriteRequestDto dto, CancellationToken ct = default)
		{
			var entity = await _db.Set<SupBrandFavorite>()
				.FirstOrDefaultAsync(x => x.UserNumberId == dto.UserNumberId && x.BrandId == dto.BrandId, ct);
			if (entity != null)
			{
				_db.Set<SupBrandFavorite>().Remove(entity);
				await _db.SaveChangesAsync(ct);
			}
		}

		/// <summary>取得我的收藏清單</summary>
		public async Task<List<BrandFavoriteItemDto>> GetMyListAsync(int userNumberId, CancellationToken ct = default)
		{
			return await _db.Set<SupBrandFavorite>()
				.Where(x => x.UserNumberId == userNumberId)
				.Include(x => x.Brand)
				.OrderByDescending(x => x.CreatedDate)
				.Select(x => new BrandFavoriteItemDto
				{
					BrandId = x.BrandId,
					BrandName = x.Brand.BrandName,
					CreatedDate = x.CreatedDate
				})
				.ToListAsync(ct);
		}
	}
}
