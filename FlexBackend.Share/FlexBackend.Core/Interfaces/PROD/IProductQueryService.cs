

using FlexBackend.Core.DTOs.PROD;

namespace FlexBackend.Core.Interfaces.PROD
{
	public interface IProductQueryService
	{
        Task<IEnumerable<ProdProductQueryDto>> GetAllProductQueryListAsync(int productId);
    }
}
