using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.PROD.ord;
using tHerdBackend.Core.DTOs.PROD.sup;

namespace tHerdBackend.Core.Interfaces.PROD
{
    public interface IProductsForApiService
	{
		Task<PagedResult<ProdProductSearchDto>> GetFrontProductListAsync(ProductFrontFilterQueryDto query, CancellationToken ct = default);
        Task<ProdProductDetailDto?> GetFrontProductListAsync(int productId, int? skuId, CancellationToken ct = default);

        Task<IEnumerable<ProductTypeTreeDto>> GetProductTypeTreeAsync(int? id, CancellationToken ct = default);
        Task<int> AddShoppingCartAsync(AddToCartDto dto, CancellationToken ct = default);
        Task<dynamic?> GetCartSummaryAsync(int? userNumberId, string? sessionId, CancellationToken ct = default);

        Task<List<SupBrandsDto>> GetBrandsAll();
        Task<List<SupBrandsDto>> SearchBrands(string keyword);

        Task<List<AttributeWithOptionsDto>> GetFilterAttributesAsync(CancellationToken ct = default);

        Task<(bool IsLiked, string Message)> ToggleLikeAsync(int userNumberId, int productId, CancellationToken ct = default);

        Task<bool> HasUserLikedProductAsync(int userNumberId, int productId, CancellationToken ct = default);

		Task<object> GetProductStats(int productId);
	}
}
