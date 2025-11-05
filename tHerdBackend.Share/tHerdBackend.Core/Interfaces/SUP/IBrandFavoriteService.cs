using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.Core.Interfaces.SUP
{
	/// <summary>品牌收藏服務</summary>
	public interface IBrandFavoriteService
	{
		Task<ApiResponse<bool>> AddAsync(int userNumberId, int brandId, CancellationToken ct = default);
		Task<ApiResponse<bool>> RemoveAsync(int userNumberId, int brandId, CancellationToken ct = default);
		Task<ApiResponse<bool>> ExistsAsync(int userNumberId, int brandId, CancellationToken ct = default);
		Task<ApiResponse<List<BrandFavoriteItemDto>>> GetMyListAsync(int userNumberId, CancellationToken ct = default);
	}
}
