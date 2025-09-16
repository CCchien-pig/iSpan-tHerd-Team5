using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.DTOs.SYS;
using FlexBackend.Core.Exceptions;
using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Core.Interfaces.SYS;

namespace FlexBackend.Services.PROD
{
    public class ProductService : IProductService
    {
        private readonly IProdProductRepository _repo;
        private readonly IProdProductQueryRepository _qrepo;
        private readonly ISysCodeRepository _srepo;

        public ProductService(IProdProductRepository repo, IProdProductQueryRepository qrepo, ISysCodeRepository srepo)
        {
            _repo = repo;
            _qrepo = qrepo;
            _srepo = srepo;
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

        public async Task<ProdProductDto?> GetByIdAsync(int Id)
        {
            try
            {
                if (Id <= 0)
                    throw new ArgumentException("ProductId must be greater than zero.");

                return await _repo.GetByIdAsync(Id);
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

        public async Task<IEnumerable<LoadBrandOptionDto>> LoadBrandOptionsAsync()
        {
            return await _repo.LoadBrandOptionsAsync();
        }

        public async Task<IEnumerable<SysCodeDto>> GetSysCodes(string Mod, List<string> ids)
        {
            return await _srepo.GetSysCodes(Mod, ids);
        }
    }
}
