using tHerdBackend.Core.DTOs.SUP;

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
	Task<List<BrandDto>> GetFilteredAsync(bool? active = null, bool? discountOnly = null, bool? featuredOnly = null);

	#endregion

	/// <summary>
	/// 取得指定品牌的按讚數
	/// </summary>
	Task<int?> GetLikeCountAsync(int brandId);

	# region 品牌折扣

	Task<bool> CheckBrandExistsAsync(int brandId);
	Task<List<BrandDiscountDto>> GetAllDiscountsAsync();
	Task<BrandDiscountDto?> GetDiscountByBrandIdAsync(int brandId);

	#endregion

	#region  品牌版面

	/// <summary>
	/// 取得指定品牌的所有 Layout 設定（歷史版本）
	/// </summary>
	/// <returns>版面設定清單 DTO 集合</returns>
	Task<IEnumerable<BrandLayoutDto>> GetLayoutsByBrandIdAsync(int brandId);

	/// <summary>
	/// 取得目前啟用的品牌 Layout（IsActive = 1）
	/// </summary>
	/// <returns>啟用中的版面設定 DTO</returns>
	Task<BrandLayoutDto?> GetActiveLayoutAsync(int brandId);

	/// <summary>
	/// 建立新的品牌 Layout 設定
	/// </summary>
	/// <returns>新增 Layout 的 ID</returns>
	Task<int> CreateLayoutAsync(int brandId, BrandLayoutCreateDto dto);

	/// <summary>
	/// 更新現有 Layout 設定內容
	/// </summary>
	/// <returns>布林值，表示是否更新成功</returns>
	Task<bool> UpdateLayoutAsync(int layoutId, BrandLayoutUpdateDto dto);

	/// <summary>
	/// 啟用指定 Layout（同品牌僅允許一個啟用中）
	/// </summary>
	/// <returns>布林值，表示是否啟用成功</returns>
	Task<bool> ActivateLayoutAsync(int layoutId, int reviserId);

	/// <summary>
	/// 停用指定 Layout（軟刪除）
	/// </summary>
	/// <returns>布林值，表示是否刪除成功</returns>
	Task<bool> SoftDeleteLayoutAsync(int layoutId, int reviserId);

	#endregion
}

