using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.PROD.ord;

namespace tHerdBackend.Core.Interfaces.PROD
{
    public interface IProductsForApiService
	{
		Task<PagedResult<ProdProductSearchDto>> GetFrontProductListAsync(ProductFilterQueryDto query, CancellationToken ct = default);
        Task<ProdProductDetailDto?> GetFrontProductListAsync(int productId, CancellationToken ct = default);

        Task<IEnumerable<ProductTypeTreeDto>> GetProductTypeTreeAsync(int? id, CancellationToken ct = default);
        Task<int> AddShoppingCartAsync(AddToCartDto dto, CancellationToken ct = default);
    }
}
