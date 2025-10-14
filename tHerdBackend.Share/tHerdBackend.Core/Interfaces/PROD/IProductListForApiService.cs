using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;

namespace tHerdBackend.Core.Interfaces.PROD
{
    public interface IProductListForApiService
	{
		Task<PagedResult<ProdProductDto>> GetFrontProductListAsync(ProductFilterQueryDto query, CancellationToken ct = default);
	}
}
