

using tHerdBackend.Core.DTOs.PROD;

namespace tHerdBackend.Core.Interfaces.PROD
{
	public interface IProductQueryService
	{
        Task<IEnumerable<ProdProductQueryDto>> GetAllProductQueryListAsync(int productId);
    }
}
