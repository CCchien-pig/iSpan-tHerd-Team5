using tHerdBackend.Core.DTOs.PROD.ord;

namespace tHerdBackend.Core.Interfaces.PROD
{
	public interface IShoppingCartRepository
    {
        Task<int> AddShoppingCartAsync(AddToCartDto dto, CancellationToken ct = default);
    }
}
