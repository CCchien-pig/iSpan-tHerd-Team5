using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Infra.Models.Sup;

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

		#region 前台查詳情

		Task<(int brandId, string brandName, int? imgId)> GetBrandAsync(int brandId, CancellationToken ct);
		Task<string?> GetAssetFileUrlByFileIdAsync(int fileId, CancellationToken ct);
		Task<List<BrandButtonDto>> GetBrandButtonsAsync(int brandId, CancellationToken ct);
		Task<List<(string contentKey, BrandAccordionItemDto item)>> GetBrandAccordionRawAsync(int brandId, CancellationToken ct);

		#endregion

	}

}
