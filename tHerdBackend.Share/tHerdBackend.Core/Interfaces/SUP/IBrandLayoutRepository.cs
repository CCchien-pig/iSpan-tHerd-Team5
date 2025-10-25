using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IBrandLayoutRepository
	{

		// 品牌版面

		#region 品牌版面 - 查詢 (Read Operations)

		/// <summary>
		/// 依 Brand ID 取得指定品牌的所有 Layout 設定（歷史版本）
		/// </summary>
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
		Task<BrandLayoutDto?> GetActiveLayoutAsync(int brandId);

		/// <summary>
		/// 依 LayoutId 取得啟用所需的必要資訊 (LayoutId, BrandId, IsActive)。
		/// (取代原 GetEntityByIdAsync，用於 Service 層的啟用邏輯判斷)
		/// </summary>
		Task<BrandLayoutDto?> GetActivationInfoByIdAsync(int layoutId);

		#endregion

		#region 品牌版面 - 新增與更新 (Write Operations for Content)

		/// <summary>
		/// 建立新的品牌 Layout 設定（內容覆寫）
		/// </summary>
		//Task<int> CreateLayoutAsync(int brandId, BrandLayoutCreateDto dto);
		Task<int> AddLayoutAsync(int brandId, BrandLayoutCreateDto dto);

		/// <summary>
		/// 更新現有 Layout 設定內容（內容覆寫）
		/// </summary>
		Task<bool> UpdateLayoutAsync(int layoutId, BrandLayoutUpdateDto dto);

		#endregion

		#region 品牌版面 - 啟用與停用 (Activation/Deactivation)

		/// <summary>
		/// 啟用指定 Layout（同品牌僅允許一個啟用中）
		/// </summary>
		//Task<bool> ActivateLayoutAsync(int layoutId, int reviserId);

		/// <summary>
		/// 停用指定 Layout（軟刪除）
		/// </summary>
		//Task<bool> SoftDeleteLayoutAsync(int layoutId, int reviserId);

		/// <summary>
		/// 底層：批量停用指定品牌下所有啟用中的 Layout (使用 ExecuteUpdateAsync)
		/// </summary>
		Task<int> BulkDeactivateAllActiveByBrandIdAsync(int brandId, int reviserId, DateTime revisedDate);

		/// <summary>
		/// 底層：啟用指定的 Layout (使用 ExecuteUpdateAsync)
		/// </summary>
		Task<int> BulkActivateLayoutAsync(int layoutId, int reviserId, DateTime revisedDate);

		/// <summary>
		/// 停用指定 Layout（軟刪除，將 IsActive 設為 0）
		/// </summary>
		Task<bool> SoftDeleteLayoutAsync(int layoutId, int reviserId, DateTime revisedDate);

		#endregion

		#region 品牌版面 - 驗證 (Validation)

		/// <summary>
		/// 檢查版本號是否存在，currentLayoutId 用於排除自身
		/// </summary>
		Task<bool> VersionExistsAsync(int brandId, string version, int? currentLayoutId);

		#endregion

	}
}
