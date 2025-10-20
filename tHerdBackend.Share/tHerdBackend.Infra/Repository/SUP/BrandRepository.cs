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


	public async Task<int?> GetLikeCountAsync(int id)
	{
		return await _context.SupBrands
			.Where(b => b.BrandId == id)
			.Select(b => (int?)b.LikeCount)
			.FirstOrDefaultAsync();
	}
}
