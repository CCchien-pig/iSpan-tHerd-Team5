using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.SUP
{
	public class BrandLayoutRepository : IBrandLayoutRepository
	{
		private readonly tHerdDBContext _context;
		public BrandLayoutRepository(tHerdDBContext context)
		{
			_context = context;
		}

		// 品牌版面

		#region 品牌版面 - 查詢 (Read Operations)

		public async Task<IEnumerable<BrandLayoutDto>> GetLayoutsByBrandIdAsync(int brandId)
		{
			return await _context.SupBrandLayoutConfigs
				.Where(x => x.BrandId == brandId)
				.OrderByDescending(x => x.CreatedDate)
				.Select(x => new BrandLayoutDto
				{
					LayoutId = x.LayoutId,
					BrandId = x.BrandId,
					LayoutJson = x.LayoutJson,
					LayoutVersion = x.LayoutVersion,
					IsActive = x.IsActive,
					Creator = x.Creator,
					CreatedDate = x.CreatedDate,
					Reviser = x.Reviser,
					RevisedDate = x.RevisedDate
				}).ToListAsync();
		}

		public async Task<BrandLayoutDto?> GetActiveLayoutAsync(int brandId)
		{
			return await _context.SupBrandLayoutConfigs
				.Where(x => x.BrandId == brandId && x.IsActive)
				.Select(x => new BrandLayoutDto
				{
					LayoutId = x.LayoutId,
					BrandId = x.BrandId,
					LayoutJson = x.LayoutJson,
					LayoutVersion = x.LayoutVersion,
					IsActive = x.IsActive,
					Creator = x.Creator,
					CreatedDate = x.CreatedDate,
					Reviser = x.Reviser,
					RevisedDate = x.RevisedDate
				}).FirstOrDefaultAsync();
		}

		// **新增：實作 GetActivationInfoByIdAsync (用於 Service 啟用判斷)**
		public async Task<BrandLayoutDto?> GetActivationInfoByIdAsync(int layoutId)
		{
			// 只查詢 Service 層執行 Activate 邏輯所需的最低限度資訊
			return await _context.SupBrandLayoutConfigs
				.Where(x => x.LayoutId == layoutId)
				.Select(x => new BrandLayoutDto
				{
					LayoutId = x.LayoutId,
					BrandId = x.BrandId,
					IsActive = x.IsActive,
					// 其他欄位給予預設值，避免 EF 查詢不必要的資料
					LayoutJson = string.Empty,
					Creator = 0,
					CreatedDate = DateTime.MinValue,
				})
				.FirstOrDefaultAsync();
		}

		#endregion

		#region 品牌版面 - 新增與更新 (Write Operations for Content)

		public async Task<int> AddLayoutAsync(int brandId, BrandLayoutCreateDto dto)
		{
			var entity = new SupBrandLayoutConfig
			{
				BrandId = brandId,
				LayoutJson = dto.LayoutJson,
				LayoutVersion = dto.LayoutVersion,
				Creator = dto.Creator,
				CreatedDate = DateTime.Now, // 註：DateTime.Now 應移到 Service 層
				IsActive = false
			};
			_context.SupBrandLayoutConfigs.Add(entity);
			await _context.SaveChangesAsync();
			return entity.LayoutId;
		}

		// **修正：實作 UpdateLayoutAsync**
		public async Task<bool> UpdateLayoutAsync(int layoutId, BrandLayoutUpdateDto dto)
		{
			var layout = await _context.SupBrandLayoutConfigs.FindAsync(layoutId);
			if (layout == null) return false;

			// 負責 DTO -> Entity 映射
			layout.LayoutJson = dto.LayoutJson;
			layout.LayoutVersion = dto.LayoutVersion;
			layout.Reviser = dto.Reviser;
			layout.RevisedDate = DateTime.Now; // 註：DateTime.Now 應移到 Service 層

			await _context.SaveChangesAsync();
			return true;
		}

		#endregion

		#region 品牌版面 - 狀態管理 (Activation/Deactivation - 底層高效操作)

		// **修正：SoftDeleteLayoutAsync (使用 ExecuteUpdateAsync 實現高效軟刪除)**
		public async Task<bool> SoftDeleteLayoutAsync(int layoutId, int reviserId, DateTime revisedDate)
		{
			var updatedRows = await _context.SupBrandLayoutConfigs
				.Where(l => l.LayoutId == layoutId)
				.ExecuteUpdateAsync(sets => sets
					.SetProperty(l => l.IsActive, false)
					.SetProperty(l => l.Reviser, reviserId)
					.SetProperty(l => l.RevisedDate, revisedDate));

			return updatedRows > 0;
		}

		// **修正：實作 BulkDeactivateAllActiveByBrandIdAsync (用於 Service 啟用邏輯)**
		public async Task<int> BulkDeactivateAllActiveByBrandIdAsync(int brandId, int reviserId, DateTime revisedDate)
		{
			return await _context.SupBrandLayoutConfigs
				.Where(l => l.BrandId == brandId && l.IsActive == true)
				.ExecuteUpdateAsync(sets => sets
					.SetProperty(l => l.IsActive, false)
					.SetProperty(l => l.Reviser, reviserId)
					.SetProperty(l => l.RevisedDate, revisedDate));
		}

		// **修正：實作 BulkActivateLayoutAsync (用於 Service 啟用邏輯)**
		public async Task<int> BulkActivateLayoutAsync(int layoutId, int reviserId, DateTime revisedDate)
		{
			return await _context.SupBrandLayoutConfigs
				.Where(l => l.LayoutId == layoutId)
				.ExecuteUpdateAsync(sets => sets
					.SetProperty(l => l.IsActive, true)
					.SetProperty(l => l.Reviser, reviserId)
					.SetProperty(l => l.RevisedDate, revisedDate));
		}

		#endregion

	}
}
