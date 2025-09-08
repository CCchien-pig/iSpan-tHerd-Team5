using FlexBackend.Core.DTOs.PROD;

namespace FlexBackend.Core.Interfaces.PROD
{
    public interface IProductService
    {
        Task<IEnumerable<ProdProductDto>> GetAllAsync();
        Task<ProdProductDto?> GetByIdAsync(int productId);
        Task<int> CreateAsync(ProdProductDto dto);
        Task UpdateAsync(ProdProductDto dto);
        Task DeleteAsync(int productId);
    }
}
