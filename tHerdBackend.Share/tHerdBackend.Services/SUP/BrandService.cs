using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;

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

	//取得指定品牌的按讚數
	public async Task<int?> GetLikeCountAsync(int brandId) => await _repo.GetLikeCountAsync(brandId);

	#region 品牌折扣

	public async Task<List<BrandDiscountDto>> GetAllDiscountsAsync()
	=> await _repo.GetAllDiscountsAsync();

	public async Task<BrandDiscountDto?> GetDiscountByBrandIdAsync(int brandId)
		=> await _repo.GetDiscountByBrandIdAsync(brandId);

	#endregion


}

