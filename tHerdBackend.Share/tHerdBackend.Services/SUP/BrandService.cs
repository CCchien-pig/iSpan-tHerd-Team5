using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;

public class BrandService : IBrandService
{
	private readonly IBrandRepository _repo;

	public BrandService(IBrandRepository repo)
	{
		_repo = repo;
	}

	//public async Task<List<BrandDto>> GetAllAsync() => await _repo.GetAllAsync();

	public async Task<BrandDto?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

	public async Task<List<BrandDto>> GetFilteredAsync(
		bool? isActive = null,
		bool? isDiscountActive = null,
		bool? isFeatured = null)
	{
		return await _repo.GetFilteredAsync(isActive, isDiscountActive, isFeatured);
	}



	public async Task<int?> GetLikeCountAsync(int brandId) => await _repo.GetLikeCountAsync(brandId);
}

