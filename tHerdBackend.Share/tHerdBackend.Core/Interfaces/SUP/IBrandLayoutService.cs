using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Services.SUP
{
	public interface IBrandLayoutService
	{
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
}