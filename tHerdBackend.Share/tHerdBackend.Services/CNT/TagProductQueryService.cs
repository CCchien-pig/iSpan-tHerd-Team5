using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.Interfaces.CNT;


namespace tHerdBackend.Services.CNT
{
	public class TagProductQueryService : ITagProductQueryService
	{
		private readonly ITagProductReadRepository _repo;

		public TagProductQueryService(ITagProductReadRepository repo)
		{
			_repo = repo;
		}

		public async Task<PagedResult<ProdProductDto>> GetProductsByTagAsync(
			int tagId,
			int page,
			int pageSize)
		{
			var (total, rows) = await _repo.GetTagProductsPageAsync(tagId, page, pageSize);

			var productIds = rows
				.Select(r => r.ProductId)
				.Distinct()
				.ToList();

			var skuIds = rows
				.Select(r => r.MainSkuIdRaw)
				.OfType<int>()
				.Distinct()
				.ToList();

			var priceMap = await _repo.GetSkuPriceMapAsync(skuIds);
			var imageMap = await _repo.GetMainImageMapAsync(productIds);
			var reviewMap = await _repo.GetReviewMapAsync(productIds);

			var dtoItems = rows.Select(r =>
			{
				// 價格
				(decimal? SalePrice, decimal? ListPrice, decimal? UnitPrice) skuInfo =
					(null, null, null);

				if (r.MainSkuIdRaw.HasValue &&
					priceMap.TryGetValue(r.MainSkuIdRaw.Value, out var foundSku))
				{
					skuInfo = foundSku;
				}

				// 評價
				(decimal? Avg, int? Count) rv = (null, null);
				if (reviewMap.TryGetValue(r.ProductId, out var foundRv))
				{
					rv = foundRv;
				}

				// 主圖
				imageMap.TryGetValue(r.ProductId, out var mainImgUrl);

				return new ProdProductDto
				{
					ProductId = r.ProductId,
					ProductName = r.ProductName ?? string.Empty,
					Badge = r.Badge ?? string.Empty,
					IsPublished = r.IsPublished,

					BrandName = r.BrandName ?? string.Empty,
					ProductTypeName = r.ProductTypeName,

					MainSkuId = r.MainSkuIdRaw ?? 0,
					ProductCode = r.ProductCode ?? string.Empty,

					Creator = r.Creator,
					CreatedDate = r.CreatedDate,
					Reviser = r.Reviser,

					SalePrice = skuInfo.SalePrice,
					ListPrice = skuInfo.ListPrice,
					UnitPrice = skuInfo.UnitPrice,

					AvgRating = rv.Avg,
					ReviewCount = rv.Count,

					ImageUrl = mainImgUrl ?? string.Empty,

					SupplierName = string.Empty,
					CreatorNm = string.Empty,
					ReviserNm = string.Empty,
					ProductTypeDesc = new List<string>()
				};
			}).ToList();

			return new PagedResult<ProdProductDto>
			{
				Total = total,
				Items = dtoItems
			};
		}
	}
}
