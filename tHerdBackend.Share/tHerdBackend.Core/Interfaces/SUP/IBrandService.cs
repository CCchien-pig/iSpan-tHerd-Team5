using tHerdBackend.Core.DTOs.SUP;

public interface IBrandService
{
	/// <summary>
	/// 取得所有品牌
	/// </summary>
	//Task<List<BrandDto>> GetAllAsync();

	/// <summary>
	/// 依 ID 取得單一品牌
	/// </summary>
	Task<BrandDto?> GetByIdAsync(int id);

	/// <summary>
	/// 依條件取得品牌清單，可篩選啟用、折扣、精選
	/// </summary>
	Task<List<BrandDto>> GetFilteredAsync(bool? active = null, bool? discountOnly = null, bool? featuredOnly = null);

	/// <summary>
	/// 取得指定品牌的按讚數
	/// </summary>
	Task<int?> GetLikeCountAsync(int brandId);
}

