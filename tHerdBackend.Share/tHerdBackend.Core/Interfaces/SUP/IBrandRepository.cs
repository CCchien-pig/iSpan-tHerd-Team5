using tHerdBackend.Core.DTOs.SUP.Brand;

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


	}

}
