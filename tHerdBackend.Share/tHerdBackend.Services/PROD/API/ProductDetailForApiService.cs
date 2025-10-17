using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.Exceptions;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.Interfaces.Products;

namespace tHerdBackend.Services.PROD.API
{
    public class ProductDetailForApiService : IProductDetailForApiService
	{
        private readonly IProdProductRepository _repo;

        public ProductDetailForApiService(IProdProductRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// 前台：依商品ID 取得商品詳細資料
        /// </summary>
        /// <param name="productId">商品 ID</param>
        /// <param name="ct">連線中斷控制</param>
        /// <returns>商品詳情 DTO</returns>
        public async Task<ProdProductDetailDto?> GetFrontProductDetailAsync(int productId, CancellationToken ct = default)
        {
            try
            {
                // 防呆
                if (productId <= 0)
                    throw new ArgumentException("商品 ID 不可為 0 或負數");

                // 查詢資料
                var detail = await _repo.GetByIdAsync(productId, ct);

                // 找不到時回報 NotFound
                if (detail == null)
                    throw new ArgumentException($"查無商品 ID: {productId}");

                return detail;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }
    }
}
