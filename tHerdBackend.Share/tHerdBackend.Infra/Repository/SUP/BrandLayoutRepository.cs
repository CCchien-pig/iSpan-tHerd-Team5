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

		// (多筆)取得所有歷史版本 Layouts
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

		// (單筆)依 Layout ID 取得單一 Layout 紀錄（包含完整的 LayoutJson）
		public async Task<BrandLayoutDto?> GetLayoutByLayoutIdAsync(int layoutId)
		{
			return await _context.SupBrandLayoutConfigs.AsNoTracking()
				.Where(x => x.LayoutId == layoutId)
				.Select(x => new BrandLayoutDto
				{
					// ... (BrandLayoutDto 的所有屬性，包括 LayoutJson)
					LayoutId = x.LayoutId,
					BrandId = x.BrandId,
					LayoutJson = x.LayoutJson, // 必須包含完整的 JSON
					LayoutVersion = x.LayoutVersion,
					IsActive = x.IsActive,
					Creator = x.Creator,
					CreatedDate = x.CreatedDate,
					Reviser = x.Reviser,
					RevisedDate = x.RevisedDate
				})
				.FirstOrDefaultAsync(); // 確保只傳回一筆或 null
		}

		// 僅取目前啟用中的 Layout ID
		public async Task<int?> GetActiveLayoutIdAsync(int brandId)
		{
			// 查詢 IsActive = true 的 Layout，並只選擇 LayoutId
			return await _context.SupBrandLayoutConfigs.AsNoTracking()
				.Where(x => x.BrandId == brandId && x.IsActive == true)
				.Select(x => (int?)x.LayoutId) // 確保傳回 int? 類型
				.FirstOrDefaultAsync();
		}

		// 取得完整的 Layout 數據
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

		// 先取出資料，再在記憶體中指定預設值 (用於 Service 啟用判斷)
		public async Task<BrandLayoutDto?> GetActivationInfoByIdAsync(int layoutId)
		{
			var entity = await _context.SupBrandLayoutConfigs
				.AsNoTracking()
				.Where(x => x.LayoutId == layoutId)
				.FirstOrDefaultAsync();

			if (entity == null)
				return null;

			return new BrandLayoutDto
			{
				LayoutId = entity.LayoutId,
				BrandId = entity.BrandId,
				IsActive = entity.IsActive,
				CreatedDate = entity.CreatedDate,
				RevisedDate = entity.RevisedDate,
				LayoutJson = string.Empty,
				Creator = 0,
				Reviser = 0
			};
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

		#region 品牌版面 - 驗證 (Validation)

		// 檢查版本號是否存在，currentLayoutId 用於排除自身
		public async Task<bool> VersionExistsAsync(int brandId, string version, int? currentLayoutId)
		{
			var query = _context.SupBrandLayoutConfigs
				.Where(x => x.BrandId == brandId && x.LayoutVersion == version);

			if (currentLayoutId.HasValue)
			{
				// 如果是更新，則排除當前正在編輯的 Layout
				query = query.Where(x => x.LayoutId != currentLayoutId.Value);
			}

			return await query.AnyAsync();
		}

		#endregion

	}
}
