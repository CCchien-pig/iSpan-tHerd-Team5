using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.Exceptions;
using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Core.Interfaces.Products;

namespace FlexBackend.Services.PROD
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
