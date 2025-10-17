using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IBrandRepository
	{
		Task<List<BrandDto>> GetAllAsync();
		Task<BrandDto?> GetByIdAsync(int id);

		/// <summary>
		/// 依條件篩選品牌
		/// </summary>
		Task<List<BrandDto>> GetFilteredAsync(bool? active = null, bool? discountOnly = null, bool? featuredOnly = null);

		Task<int?> GetLikeCountAsync(int id);
	}

}
