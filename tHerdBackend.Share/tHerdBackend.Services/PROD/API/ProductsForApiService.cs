using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.PROD.ord;
using tHerdBackend.Core.Exceptions;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.Interfaces.Products;

namespace tHerdBackend.Services.PROD.API
{
    public class ProductsForApiService : IProductsForApiService
	{
        private readonly IProdProductRepository _repo;
		private readonly IShoppingCartRepository _srepo;

		// 伺服器端「單頁最大筆數」卡控
		private const int MaxPageSize = 20;        // 每頁最多回 20 筆（自行調整）

        public ProductsForApiService(IProdProductRepository repo, IShoppingCartRepository srepo)
        {
            _repo = repo;
			_srepo = srepo;
		}

        /// <summary>
        /// 前台: 依傳入條件，取得產品清單
        /// </summary>
        /// <param name="query">查詢條件</param>
        /// <param name="ct">連線</param>
        /// <returns></returns>
        public async Task<PagedResult<ProdProductSearchDto>> GetFrontProductListAsync(
            ProductFilterQueryDto query, CancellationToken ct = default)
        {
            try
            {
                // 參數防呆 + 卡控
                var pageIndex = Math.Max(1, query.PageIndex);
                var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

                // 查詢商品基本資料
                var (list, total) = await _repo.GetAllFrontAsync(query, ct);

                // 回傳分頁結果
                return new PagedResult<ProdProductSearchDto>
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
        public async Task<ProdProductDetailDto?> GetFrontProductListAsync(int id, int? skuId, CancellationToken ct = default)
        {
            try
            {
                // 查詢商品詳細
                var item = await _repo.GetByIdAsync(id, skuId, ct);

                // 回傳分頁結果
                return item;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

        /// <summary>
        /// 取得商品分類樹狀結構（含子分類）
        /// 用於前台 MegaMenu 或分類篩選
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ProductTypeTreeDto>> GetProductTypeTreeAsync(int? id, CancellationToken ct = default)
        {
            try
            {
                // 查詢商品詳細
                var item = await _repo.GetProductTypeTreeAsync(id, ct);

                // 回傳分頁結果
                return item;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleErrorMsg(ex);
                throw;
            }
        }

		/// <summary>
		/// 新增購物車
		/// </summary>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<int> AddShoppingCartAsync(AddToCartDto dto, CancellationToken ct = default)
		{
			try
			{
				// 新增購物車
				var item = await _srepo.AddShoppingCartAsync(dto, ct);

				return item;
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleErrorMsg(ex);
				throw;
			}
		}

        /// <summary>
        /// 查詢購物車數量
        /// </summary>
        /// <param name="userNumberId"></param>
        /// <param name="sessionId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<dynamic?> GetCartSummaryAsync(int? userNumberId, string? sessionId, CancellationToken ct = default)
        {
            try
            {
                var item = await _srepo.GetCartSummaryAsync(userNumberId, sessionId, ct);

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
