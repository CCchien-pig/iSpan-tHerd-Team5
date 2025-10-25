using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.DTOs.SUP.BrandLayoutBlocks;

namespace tHerdBackend.Core.Services.SUP
{
	public interface IBrandLayoutService
	{
		#region  品牌版面 - 查詢與CRUD

		/// <summary>
		/// 依 Brand ID 取得指定品牌的所有 Layout 設定（歷史版本）
		/// </summary>
		/// <returns>版面設定清單 DTO 集合</returns>
		Task<IEnumerable<BrandLayoutDto>> GetLayoutsByBrandIdAsync(int brandId);

		/// <summary>
		/// 依 Layout ID 取得單一 Layout 紀錄（包含完整的 LayoutJson）
		/// </summary>
		Task<BrandLayoutDto?> GetLayoutByLayoutIdAsync(int layoutId);

		/// <summary>
		/// 取得目前啟用中的 Brand Layout ID。
		/// </summary>
		Task<int?> GetActiveLayoutIdAsync(int brandId);

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

		#endregion

		#region 品牌版面 - 啟用、軟刪除

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

		#region 品牌版面 - 驗證 (Validation)

		/// <summary>
		/// 檢查指定品牌的版面版本號是否已存在。
		/// </summary>
		/// <param name="brandId">品牌 ID。</param>
		/// <param name="version">要檢查的版本號。</param>
		/// <param name="currentLayoutId">當前正在編輯的 Layout ID (用於更新時排除自身)。</param>
		/// <returns>如果版本號已存在，則為 true；否則為 false。</returns>
		Task<bool> VersionExistsAsync(int brandId, string version, int? currentLayoutId);

		#endregion

		#region  品牌版面 - JSON 處理

		List<BaseLayoutBlockDto> DeserializeLayout(string layoutJson);
		string SerializeLayout(List<BaseLayoutBlockDto> blocks);

		#endregion

	}
}