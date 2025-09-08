using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Core.Interfaces.Products;

namespace FlexBackend.Services.PROD
{
    public class ProductService : IProductService
    {
        private readonly IProdProductRepository _repo;

        public ProductService(IProdProductRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<ProdProductDto>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<ProdProductDto?> GetByIdAsync(int productId)
        {
            if (productId <= 0)
                throw new ArgumentException("ProductId must be greater than zero.");

            return await _repo.GetByIdAsync(productId);
        }

        public async Task<int> CreateAsync(ProdProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName))
                throw new ArgumentException("Product name is required.");

            return await _repo.AddAsync(dto);
        }

        public async Task UpdateAsync(ProdProductDto dto)
        {
            if (dto.ProductId <= 0)
                throw new ArgumentException("Invalid ProductId.");

            await _repo.UpdateAsync(dto);
        }

        public async Task DeleteAsync(int productId)
        {
            if (productId <= 0)
                throw new ArgumentException("Invalid ProductId.");

            await _repo.DeleteAsync(productId);
        }
    }
}
