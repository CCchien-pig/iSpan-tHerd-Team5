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
				SeoId = b.SeoId,
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
				SeoId = b.SeoId,
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

	public async Task<List<BrandDto>> GetActiveAsync()
	{
		return await _context.SupBrands.AsNoTracking()
			.Where(b => b.IsActive)
			.Select(b => new BrandDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				BrandCode = b.BrandCode,
				SupplierId = b.SupplierId,
				SeoId = b.SeoId,
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

	public async Task<List<BrandDto>> GetActiveDiscountAsync()
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		return await _context.SupBrands.AsNoTracking()
			.Where(b => b.IsActive &&
						b.IsDiscountActive &&
						(b.DiscountRate > 0) &&
						(b.StartDate == null || b.StartDate <= today) &&
						(b.EndDate == null || b.EndDate >= today))
			.Select(b => new BrandDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				BrandCode = b.BrandCode,
				SupplierId = b.SupplierId,
				SeoId = b.SeoId,
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

	public async Task<List<BrandDto>> GetActiveFeaturedAsync()
	{
		return await _context.SupBrands.AsNoTracking()
			.Where(b => b.IsActive && b.IsFeatured)
			.Select(b => new BrandDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				BrandCode = b.BrandCode,
				SupplierId = b.SupplierId,
				SeoId = b.SeoId,
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
