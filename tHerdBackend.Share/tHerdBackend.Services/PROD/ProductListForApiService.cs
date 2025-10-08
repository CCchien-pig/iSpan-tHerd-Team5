using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.Interfaces.Products;

namespace tHerdBackend.Services.PROD
{
    public class ProductListForApiService : IProductListForApiService
	{
        private readonly IProdProductRepository _repo;

        public ProductListForApiService(IProdProductRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// 前台: 依傳入條件，取得產品清單
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<PagedResult<ProdProductDto>> GetFrontProductListAsync(ProductFilterQueryDto query, CancellationToken ct = default)
        {
			// 查詢商品基本資料
			var list = await _repo.GetAllAsync(ct);

			// 判斷是否有資料
			if (list == null || !list.Any())
				return new PagedResult<ProdProductDto>
				{
					TotalCount = 0,
					PageIndex = query.PageIndex,
					PageSize = query.PageSize,
					Items = new List<ProdProductDto>()
				};

			// === 1. 條件篩選 ===
			var filtered = list.AsQueryable();

			if (string.IsNullOrWhiteSpace(query.Keyword) == false)
				filtered = filtered.Where(x => x.ProductName.Contains(query.Keyword));

			if (string.IsNullOrWhiteSpace(query.ProductTypeCode) == false)
				filtered = filtered.Where(x => x.ProductTypeCode == query.ProductTypeCode);

			if (query.BrandId.HasValue)
				filtered = filtered.Where(x => x.BrandId == query.BrandId);

			if (query.MinPrice.HasValue)
				filtered = filtered.Where(x => x.UnitPrice >= query.MinPrice);

			if (query.MaxPrice.HasValue)
				filtered = filtered.Where(x => x.UnitPrice <= query.MaxPrice);

			// === 2. 排序 ===
			filtered = query.SortBy switch
			{
				"price" when query.SortDesc => filtered.OrderByDescending(x => x.UnitPrice),
				"price" => filtered.OrderBy(x => x.UnitPrice),
				"name" when query.SortDesc => filtered.OrderByDescending(x => x.ProductName),
				"name" => filtered.OrderBy(x => x.ProductName),
				_ => filtered.OrderBy(x => x.ProductId)
			};

			// === 3. 分頁 ===
			int total = filtered.Count();
			var items = filtered
				.Skip((query.PageIndex - 1) * query.PageSize)
				.Take(query.PageSize)
				.Select(x => new ProdProductDto
				{
					ProductId = x.ProductId,
					ProductName = x.ProductName,
					ImageUrl = x.ImageUrl,
					Badge = x.Badge,
					ListPrice = x.ListPrice,
					UnitPrice = x.UnitPrice,
					SalePrice = x.SalePrice
				})
				.ToList();

			// === 4. 組裝結果 ===
			return new PagedResult<ProdProductDto>
			{
				TotalCount = total,
				PageIndex = query.PageIndex,
				PageSize = query.PageSize,
				Items = items
			};
		}
    }
}
