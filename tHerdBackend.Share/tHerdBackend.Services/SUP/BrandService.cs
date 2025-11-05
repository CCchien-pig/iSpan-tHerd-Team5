using Microsoft.Extensions.Logging;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models.Sup;

public class BrandService : IBrandService
{
	private readonly IBrandRepository _repo;
	private readonly ILogger<IBrandService> _logger;
	private readonly ICurrentUser _me;

	public BrandService(IBrandRepository repo, ILogger<IBrandService> logger, ICurrentUser me)
	{
		_repo = repo;
		_logger = logger;
		_me = me;
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
		// raw: List<(string contentKey, BrandAccordionItemDto item)>
		// item 內已含 Title/Body/Order

		// raw: List<(string contentKey /*ContentId字串*/, BrandAccordionItemDto item /*含 Title/Body/Order*/)>
		var grouped = raw
			.GroupBy(x => x.item.Title ?? string.Empty)
			.Select(g => new
			{
				Title = g.Key,
				// 找出該組中對應的最小 ContentId 作為排序鍵（從 contentKey 來）
				MinContentId = g.Select(x =>
				{
					// contentKey 目前是 ContentId.ToString()
					return int.TryParse(x.contentKey, out var cid) ? cid : int.MaxValue;
				}).DefaultIfEmpty(int.MaxValue).Min(),
				Items = g.Select(x => x.item).OrderBy(x => x.Order).ToList()
			})
			.OrderBy(x => x.MinContentId) // 依 ContentId 先後
			.Select(x => new BrandAccordionGroupDto
			{
				ContentKey = x.Title,
				Items = x.Items
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

