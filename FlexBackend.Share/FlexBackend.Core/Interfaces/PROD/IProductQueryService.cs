

using FlexBackend.Core.DTOs.PROD;

namespace FlexBackend.Core.Interfaces.PROD
{
	public interface IProductQueryService
	{
		//Task<ProdProductDto?> GetProductDetailAsync(int productId);
		Task<IEnumerable<ProdProductDto>> GetAllProductsAsync();
	}
}
