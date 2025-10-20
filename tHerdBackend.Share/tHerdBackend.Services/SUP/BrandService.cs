using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;

public class BrandService : IBrandService
{
	private readonly IBrandRepository _repo;
	public BrandService(IBrandRepository repo)
	{
		_repo = repo;
	}

	public async Task<List<BrandDto>> GetAllAsync() => await _repo.GetAllAsync();
	public async Task<BrandDto?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);
	public async Task<List<BrandDto>> GetActiveAsync() => await _repo.GetActiveAsync();
	public async Task<List<BrandDto>> GetActiveDiscountAsync() => await _repo.GetActiveDiscountAsync();
	public async Task<List<BrandDto>> GetActiveFeaturedAsync() => await _repo.GetActiveFeaturedAsync();
	public async Task<int?> GetLikeCountAsync(int brandId) => await _repo.GetLikeCountAsync(brandId);
}
