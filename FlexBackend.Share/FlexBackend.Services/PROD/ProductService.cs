using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.Exceptions;
using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Core.Interfaces.Products;

namespace FlexBackend.Services.PROD
{
    public class ProductService : IProductService
    {
        private readonly IProdProductRepository _repo;
        private readonly IProdProductQueryRepository _qrepo;

        public ProductService(IProdProductRepository repo, IProdProductQueryRepository qrepo)
        {
            _repo = repo;
            _qrepo = qrepo;
        }

        /// <summary>
        /// 商品完整資料清單
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ProdProductQueryDto>> GetAllProductQueryListAsync(int ProductId)
        {
            try
            {
                return await _qrepo.GetAllProductQueryListAsync(ProductId);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        /// <summary>
        /// 商品基本資料清單
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ProdProductDto>> GetAllAsync()
        {
            try
            {
                return await _repo.GetAllAsync();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        public async Task<ProdProductDto?> GetByIdAsync(int productId)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("ProductId must be greater than zero.");

                return await _repo.GetByIdAsync(productId);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        public async Task<int> CreateAsync(ProdProductDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ProductName))
                    throw new ArgumentException("Product name is required.");

                return await _repo.AddAsync(dto);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        public async Task UpdateAsync(ProdProductDto dto)
        {
            try
            {
                if (dto.ProductId <= 0)
                    throw new ArgumentException("Invalid ProductId.");

                await _repo.UpdateAsync(dto);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        public async Task DeleteAsync(int productId)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Invalid ProductId.");

                await _repo.DeleteAsync(productId);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }
    }
}
