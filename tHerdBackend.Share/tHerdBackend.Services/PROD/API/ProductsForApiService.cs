using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.Exceptions;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.Interfaces.Products;

namespace tHerdBackend.Services.PROD.API
{
    public class ProductsForApiService : IProductsForApiService
	{
        private readonly IProdProductRepository _repo;

        // 伺服器端「單頁最大筆數」卡控
        private const int MaxPageSize = 20;        // 每頁最多回 20 筆（自行調整）

        public ProductsForApiService(IProdProductRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// 前台: 依傳入條件，取得產品清單
        /// </summary>
        /// <param name="query">查詢條件</param>
        /// <param name="ct">連線</param>
        /// <returns></returns>
        public async Task<PagedResult<ProdProductDto>> GetFrontProductListAsync(
            ProductFilterQueryDto query, CancellationToken ct = default)
        {
            try
            {
                // 參數防呆 + 卡控
                var pageIndex = Math.Max(1, query.PageIndex);
                var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

                // 查詢商品基本資料
                var (list, total) = await _repo.GetAllAsync(query, ct);

                // 回傳分頁結果
                return new PagedResult<ProdProductDto>
                {
                    TotalCount = total,
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Items = list?.ToList() ?? [] // 若 list 是 null，就給空集合
                };
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        /// <summary>
        /// 查詢商品詳細
        /// </summary>
        /// <param name="id">商品編號</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<ProdProductDetailDto?> GetFrontProductListAsync(int id, CancellationToken ct = default)
        {
            try
            {
                // 查詢商品詳細
                var item = await _repo.GetByIdAsync(id, ct);

                // 回傳分頁結果
                return item;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }
    }
}
