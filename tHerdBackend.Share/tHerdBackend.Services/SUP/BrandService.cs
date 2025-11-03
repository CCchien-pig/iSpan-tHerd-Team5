using Microsoft.Extensions.Logging;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models.Sup;

public class BrandService : IBrandService
{
	private readonly IBrandRepository _repo;
	private readonly ILogger<IBrandService> _logger;

	public BrandService(IBrandRepository repo, ILogger<IBrandService> logger)
	{
		_repo = repo;
		_logger = logger;
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



	#region 前台查詳情

	public async Task<BrandDetailDto?> GetBrandDetailAsync(int brandId, CancellationToken ct)
	{
		// 基礎品牌
		var (id, name, imgId) = await _repo.GetBrandAsync(brandId, ct);
		if (id == 0)
		{
			_logger.LogWarning("Brand not found or inactive. brandId={BrandId}", brandId);
			return null;
		}

		// Banner
		string? bannerUrl = null;
		if (imgId.HasValue)
			bannerUrl = await _repo.GetAssetFileUrlByFileIdAsync(imgId.Value, ct);

		// Buttons
		var buttons = await _repo.GetBrandButtonsAsync(brandId, ct);

		// Accordions：扁平 → 分組
		var raw = await _repo.GetBrandAccordionRawAsync(brandId, ct);
		var grouped = raw
			.GroupBy(x => x.contentKey)          // contentKey = ContentId
			.Select(g => new BrandAccordionGroupDto
			{
				ContentKey = g.Key.ToString(),   // 若希望字串，可 ToString()
				Items = g.Select(x => x.item)
						 .OrderBy(x => x.Order)
						 .ToList()
			})
			.ToList();


		return new BrandDetailDto
		{
			BrandId = id,
			BrandName = name,
			BannerUrl = bannerUrl,
			Buttons = buttons.OrderBy(x => x.Order).ToList(),
			Accordions = grouped,
			// OrderedBlocks = new() { "Banner", "Buttons", "Accordion" } // 未實作 LayoutConfig 前的預設
		};
	}

	#endregion

}

