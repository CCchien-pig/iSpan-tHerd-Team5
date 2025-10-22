using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IBrandRepository
	{
		#region 品牌

		Task<List<BrandDto>> GetAllAsync();
		Task<BrandDto?> GetByIdAsync(int id);

		/// <summary>
		/// 依條件篩選品牌
		/// </summary>
		Task<List<BrandDto>> GetFilteredAsync(bool? active = null, bool? discountOnly = null, bool? featuredOnly = null);

		#endregion

		Task<int?> GetLikeCountAsync(int id);

		#region 品牌折扣

		Task<bool> CheckBrandExistsAsync(int brandId);
		Task<List<BrandDiscountDto>> GetAllDiscountsAsync();
		Task<BrandDiscountDto?> GetDiscountByBrandIdAsync(int brandId);

		#endregion


		#region 品牌版面

		Task<IEnumerable<BrandLayoutDto>> GetLayoutsByBrandIdAsync(int brandId);
		Task<BrandLayoutDto?> GetActiveLayoutAsync(int brandId);
		Task<int> CreateLayoutAsync(int brandId, BrandLayoutCreateDto dto);
		Task<bool> UpdateLayoutAsync(int layoutId, BrandLayoutUpdateDto dto);
		Task<bool> ActivateLayoutAsync(int layoutId, int reviserId);
		Task<bool> SoftDeleteLayoutAsync(int layoutId, int reviserId);

		#endregion
	}

}
