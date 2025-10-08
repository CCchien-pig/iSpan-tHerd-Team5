using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.Exceptions;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.Interfaces.Products;

namespace tHerdBackend.Services.PROD
{
    public class ProductQueryService : IProductQueryService
    {
        private readonly IProdProductQueryRepository _repo;

        public ProductQueryService(IProdProductQueryRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// 商品完整資料清單
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ProdProductQueryDto>> GetAllProductQueryListAsync(int ProductId)
        {
            try
            {
                return await _repo.GetAllProductQueryListAsync(ProductId);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }
    }
}
