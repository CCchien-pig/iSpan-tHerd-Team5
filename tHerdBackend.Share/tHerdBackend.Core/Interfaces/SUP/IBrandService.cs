using tHerdBackend.Core.DTOs.SUP;

public interface IBrandService
{
	Task<List<BrandDto>> GetAllAsync();
	Task<BrandDto?> GetByIdAsync(int id);
	Task<List<BrandDto>> GetActiveAsync();
	Task<List<BrandDto>> GetActiveDiscountAsync();
	Task<List<BrandDto>> GetActiveFeaturedAsync();
	Task<int?> GetLikeCountAsync(int brandId);
}
