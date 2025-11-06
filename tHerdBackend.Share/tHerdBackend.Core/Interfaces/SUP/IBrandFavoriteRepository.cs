using tHerdBackend.Core.DTOs.SUP.Brand;

namespace tHerdBackend.Core.Interfaces.SUP
{
	/// <summary>
	/// 品牌收藏資料存取
	/// </summary>
	public interface IBrandFavoriteRepository
	{
		/// <summary>確認是否已收藏</summary>
		Task<bool> ExistsAsync(int userNumberId, int brandId, CancellationToken ct = default);
		/// <summary>新增收藏</summary>
		Task AddAsync(BrandFavoriteRequestDto dto, CancellationToken ct = default);
		/// <summary>取消收藏</summary>
		Task RemoveAsync(BrandFavoriteRequestDto dto, CancellationToken ct = default);
		/// <summary>取得我的收藏清單</summary>
		Task<List<BrandFavoriteItemDto>> GetMyListAsync(int userNumberId, CancellationToken ct = default);

	}
}
