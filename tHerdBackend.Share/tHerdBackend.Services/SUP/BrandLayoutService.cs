using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.Services.SUP;

namespace tHerdBackend.Services.SUP
{
	public class BrandLayoutService : IBrandLayoutService
	{
		private readonly IBrandLayoutRepository _repo;

		public BrandLayoutService(IBrandLayoutRepository repo)
		{
			_repo = repo;
		}

		// 品牌版面

		#region 品牌版面 - 查詢與CRUD (呼叫 Repository)

		// 查詢方法直接轉發給 Repository
		public Task<IEnumerable<BrandLayoutDto>> GetLayoutsByBrandIdAsync(int brandId)
		  => _repo.GetLayoutsByBrandIdAsync(brandId);

		public Task<BrandLayoutDto?> GetActiveLayoutAsync(int brandId)
			=> _repo.GetActiveLayoutAsync(brandId);

		// 新增方法直接轉發給 Repository
		public Task<int> CreateLayoutAsync(int brandId, BrandLayoutCreateDto dto)
			=> _repo.AddLayoutAsync(brandId, dto); // 呼叫 Repository 的 AddLayoutAsync

		// 更新方法直接轉發給 Repository
		public Task<bool> UpdateLayoutAsync(int layoutId, BrandLayoutUpdateDto dto)
			=> _repo.UpdateLayoutAsync(layoutId, dto);

		#endregion


		#region 品牌版面 - 狀態管理 (業務邏輯實作)

		// **業務邏輯：啟用指定 Layout**
		public async Task<bool> ActivateLayoutAsync(int layoutId, int reviserId)
		{
			// 1. 業務判斷：檢查目標 Layout 資訊 (使用 Repository 提供的 DTO 查詢)
			var targetInfo = await _repo.GetActivationInfoByIdAsync(layoutId);

			if (targetInfo == null)
			{
				return false; // 找不到指定 Layout
			}

			var brandId = targetInfo.BrandId;
			var now = DateTime.Now;

			// 2. 業務規則：先停用同一品牌下所有舊版
			if (targetInfo.IsActive == false)
			{
				// 使用 Repository 底層的高效批量停用操作
				await _repo.BulkDeactivateAllActiveByBrandIdAsync(brandId, reviserId, now);
			}

			// 3. 業務規則：啟用指定的 Layout
			// 使用 Repository 底層的高效啟用操作
			var updatedRows = await _repo.BulkActivateLayoutAsync(layoutId, reviserId, now);

			return updatedRows > 0;
		}

		// **業務邏輯：軟刪除 Layout**
		public Task<bool> SoftDeleteLayoutAsync(int layoutId, int reviserId)
		{
			// 直接呼叫 Repository 的高效軟刪除操作
			var now = DateTime.Now;
			return _repo.SoftDeleteLayoutAsync(layoutId, reviserId, now);
		}


		#endregion

	}
}
