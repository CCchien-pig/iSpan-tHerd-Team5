using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;

namespace tHerdBackend.Core.Interfaces.PROD
{
    public interface IProductsForApiService
	{
		Task<PagedResult<ProdProductDto>> GetFrontProductListAsync(ProductFilterQueryDto query, CancellationToken ct = default);
        Task<ProdProductDetailDto?> GetFrontProductListAsync(int productId, CancellationToken ct = default);
    }
}
