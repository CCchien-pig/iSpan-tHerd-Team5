using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

public class BrandService : IBrandService
{
	private readonly IBrandRepository _repo;

	public BrandService(IBrandRepository repo)
	{
		_repo = repo;
	}

	#region 品牌
	//public async Task<List<BrandDto>> GetAllAsync() => await _repo.GetAllAsync();
	
	public async Task<BrandDto?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

	public async Task<List<BrandDto>> GetFilteredAsync(
		bool? isActive = null,
		bool? isDiscountActive = null,
		bool? isFeatured = null)
	{
		return await _repo.GetFilteredAsync(isActive, isDiscountActive, isFeatured);
	}

	public async Task<bool> CheckBrandExistsAsync(int brandId)
	{
		return await _repo.CheckBrandExistsAsync(brandId);
	}

	#endregion

	public async Task<int?> GetLikeCountAsync(int brandId) => await _repo.GetLikeCountAsync(brandId);

	#region 品牌折扣

	public async Task<List<BrandDiscountDto>> GetAllDiscountsAsync()
	=> await _repo.GetAllDiscountsAsync();

	public async Task<BrandDiscountDto?> GetDiscountByBrandIdAsync(int brandId)
		=> await _repo.GetDiscountByBrandIdAsync(brandId);

	#endregion

	#region 品牌版面

	public Task<IEnumerable<BrandLayoutDto>> GetLayoutsByBrandIdAsync(int brandId)
	   => _repo.GetLayoutsByBrandIdAsync(brandId);

	public Task<BrandLayoutDto?> GetActiveLayoutAsync(int brandId)
		=> _repo.GetActiveLayoutAsync(brandId);

	public Task<int> CreateLayoutAsync(int brandId, BrandLayoutCreateDto dto)
		=> _repo.CreateLayoutAsync(brandId, dto);

	public Task<bool> UpdateLayoutAsync(int layoutId, BrandLayoutUpdateDto dto)
		=> _repo.UpdateLayoutAsync(layoutId, dto);

	public Task<bool> ActivateLayoutAsync(int layoutId, int reviserId)
		=> _repo.ActivateLayoutAsync(layoutId, reviserId);

	public Task<bool> SoftDeleteLayoutAsync(int layoutId, int reviserId)
		=> _repo.SoftDeleteLayoutAsync(layoutId, reviserId);


	#endregion
}

