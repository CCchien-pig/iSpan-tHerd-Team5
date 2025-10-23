using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

public class BrandRepository : IBrandRepository
{
	private readonly tHerdDBContext _context;
	public BrandRepository(tHerdDBContext context)
	{
		_context = context;
	}


	#region 品牌

	public async Task<List<BrandDto>> GetAllAsync()
	{
		return await _context.SupBrands.AsNoTracking()
			.Select(b => new BrandDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				BrandCode = b.BrandCode,
				SupplierId = b.SupplierId,
				//SeoId = b.SeoId.HasValue ? b.SeoId.Value.ToString() : null,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate,
				IsFeatured = b.IsFeatured,
				LikeCount = b.LikeCount,
				IsActive = b.IsActive,
				Creator = b.Creator,
				CreatedDate = b.CreatedDate,
				Reviser = b.Reviser,
				RevisedDate = b.RevisedDate
			})
			.ToListAsync();
	}

	public async Task<BrandDto?> GetByIdAsync(int id)
	{
		return await _context.SupBrands.AsNoTracking()
			.Where(b => b.BrandId == id)
			.Select(b => new BrandDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				BrandCode = b.BrandCode,
				SupplierId = b.SupplierId,
				//SeoId = b.SeoId.HasValue ? b.SeoId.Value.ToString() : null,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate,
				IsFeatured = b.IsFeatured,
				LikeCount = b.LikeCount,
				IsActive = b.IsActive,
				Creator = b.Creator,
				CreatedDate = b.CreatedDate,
				Reviser = b.Reviser,
				RevisedDate = b.RevisedDate
			})
			.FirstOrDefaultAsync();
	}

	public async Task<List<BrandDto>> GetFilteredAsync(
		bool? isActive = null,
		bool? isDiscountActive = null,
		bool? isFeatured = null)
	{
		var query = _context.SupBrands.AsNoTracking().AsQueryable();

		// 篩選品牌啟用狀態
		if (isActive.HasValue)
			query = query.Where(b => b.IsActive == isActive.Value);

		// 篩選折扣活動，只依 IsDiscountActive
		if (isDiscountActive.HasValue)
			query = query.Where(b => b.IsDiscountActive == isDiscountActive.Value);

		// 篩選精選品牌
		if (isFeatured.HasValue)
			query = query.Where(b => b.IsFeatured == isFeatured.Value);

		// 投影成 DTO
		return await query
			.Select(b => new BrandDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				BrandCode = b.BrandCode,
				SupplierId = b.SupplierId,
				//SeoId = b.SeoId.HasValue ? b.SeoId.Value.ToString() : null,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate,
				IsFeatured = b.IsFeatured,
				LikeCount = b.LikeCount,
				IsActive = b.IsActive,
				Creator = b.Creator,
				CreatedDate = b.CreatedDate,
				Reviser = b.Reviser,
				RevisedDate = b.RevisedDate
			})
			.ToListAsync();
	}
	public async Task<bool> CheckBrandExistsAsync(int brandId)
	{
		return await _context.Set<SupBrand>()
			.AsNoTracking()
			.AnyAsync(b => b.BrandId == brandId);
	}

	#endregion

	public async Task<int?> GetLikeCountAsync(int id)
	{
		return await _context.SupBrands
			.Where(b => b.BrandId == id)
			.Select(b => (int?)b.LikeCount)
			.FirstOrDefaultAsync();
	}


	#region 品牌折扣

	public async Task<List<BrandDiscountDto>> GetAllDiscountsAsync()
	{
		return await _context.Set<SupBrand>().AsNoTracking()
			.Select(b => new BrandDiscountDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate
			})
			.ToListAsync();
	}

	public async Task<BrandDiscountDto?> GetDiscountByBrandIdAsync(int brandId)
	{
		return await _context.SupBrands.AsNoTracking()
			.Where(b => b.BrandId == brandId)
			.Select(b => new BrandDiscountDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate
			})
			.FirstOrDefaultAsync();
	}

	#endregion


	#region 品牌版面

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

	public async Task<int> CreateLayoutAsync(int brandId, BrandLayoutCreateDto dto)
	{
		var entity = new SupBrandLayoutConfig
		{
			BrandId = brandId,
			LayoutJson = dto.LayoutJson,
			LayoutVersion = dto.LayoutVersion,
			Creator = dto.Creator,
			CreatedDate = DateTime.Now,
			IsActive = false
		};
		_context.SupBrandLayoutConfigs.Add(entity);
		await _context.SaveChangesAsync();
		return entity.LayoutId;
	}

	public async Task<bool> UpdateLayoutAsync(int layoutId, BrandLayoutUpdateDto dto)
	{
		var layout = await _context.SupBrandLayoutConfigs.FindAsync(layoutId);
		if (layout == null) return false;
		layout.LayoutJson = dto.LayoutJson;
		layout.LayoutVersion = dto.LayoutVersion;
		layout.Reviser = dto.Reviser;
		layout.RevisedDate = DateTime.Now;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> ActivateLayoutAsync(int layoutId, int reviserId)
	{
		// 1. 檢查目標 Layout 是否存在並取得 BrandId
		var targetLayout = await _context.SupBrandLayoutConfigs
			.Where(x => x.LayoutId == layoutId)
			.Select(x => new { x.BrandId, x.IsActive })
			.FirstOrDefaultAsync();

		if (targetLayout == null)
		{
			// 找不到指定 Layout
			return false;
		}

		var brandId = targetLayout.BrandId;
		var now = DateTime.Now; // 使用本地時間

		// 2.【關鍵】停用同一品牌下所有「目前啟用中」的版型 (IsActive = 1)
		//    使用 ExecuteUpdateAsync 直接在 DB 執行，解決唯一索引衝突問題。
		if (targetLayout.IsActive == false)
		{
			// 只有在目標版型未啟用時才需要停用舊版，避免不必要的 DB 操作
			await _context.SupBrandLayoutConfigs
				.Where(l => l.BrandId == brandId && l.IsActive == true)
				.ExecuteUpdateAsync(sets => sets
					.SetProperty(l => l.IsActive, false)
					.SetProperty(l => l.Reviser, reviserId)
					.SetProperty(l => l.RevisedDate, now));
		}

		// 3. 啟用指定的 Layout
		var updatedRows = await _context.SupBrandLayoutConfigs
			.Where(l => l.LayoutId == layoutId)
			.ExecuteUpdateAsync(sets => sets
				.SetProperty(l => l.IsActive, true)
				.SetProperty(l => l.Reviser, reviserId)
				.SetProperty(l => l.RevisedDate, now));

		// 如果沒有任何行被更新 (updatedRows = 0)，表示 layoutId 無效
		return updatedRows > 0;
	}

	public async Task<bool> SoftDeleteLayoutAsync(int layoutId, int reviserId)
	{
		var layout = await _context.SupBrandLayoutConfigs.FindAsync(layoutId);
		if (layout == null) return false;
		layout.IsActive = false;
		layout.Reviser = reviserId;
		layout.RevisedDate = DateTime.Now;
		await _context.SaveChangesAsync();
		return true;
	}


	#endregion
}
