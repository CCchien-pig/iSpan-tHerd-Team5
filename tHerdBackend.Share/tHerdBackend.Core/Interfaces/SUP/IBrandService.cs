using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Infra.Models.Sup;

public interface IBrandService
{
	#region 品牌
	/// <summary>
	/// 取得所有品牌
	/// </summary>
	//Task<List<BrandDto>> GetAllAsync();

	/// <summary>
	/// 依 ID 取得單一品牌
	/// </summary>
	Task<BrandDto?> GetByIdAsync(int id);

	/// <summary>
	/// 依條件取得品牌清單，可篩選啟用、折扣、精選
	/// </summary>
	Task<List<BrandDto>> GetFilteredAsync(bool? isActive = null, bool? isDiscountActive = null, bool? isFeatured = null);

	#endregion

	/// <summary>
	/// 取得指定品牌的按讚數
	/// </summary>
	Task<int?> GetLikeCountAsync(int brandId);


	/// <summary>
	/// 取得品牌綜合資訊（商品數、被收藏數、建立時間、供應商名稱）
	/// </summary>
	/// <param name="brandId">品牌 Id</param>
	/// <returns>BrandOverviewDto</returns>
	Task<BrandOverviewDto> GetBrandOverviewAsync(int brandId);


	#region 品牌折扣

	Task<bool> CheckBrandExistsAsync(int brandId);
	Task<List<BrandDiscountDto>> GetAllDiscountsAsync();
	Task<BrandDiscountDto?> GetDiscountByBrandIdAsync(int brandId);

	#endregion

	#region 前台查詳情

	Task<BrandDetailDto?> GetBrandDetailAsync(int brandId, CancellationToken ct);

	/// <summary>
	/// 取得品牌銷量Top N 排行
	/// </summary>
	Task<List<BrandSalesRankingDto>> GetTopBrandsBySalesAsync(int topN = 10);


	Task<BrandAccordionContentDto?> GetAccordionAsync(int brandId, int contentId, CancellationToken ct);
	Task<BrandArticleDto?> GetArticleAsync(int brandId, int contentId, CancellationToken ct);
	Task<BannerDto?> GetBannerAsync(int brandId, string? linkUrl, CancellationToken ct);

	#endregion

}

