using tHerdBackend.Core.DTOs.PROD;

namespace tHerdBackend.Core.Interfaces.PROD
{
    public interface IProductDetailForApiService
    {
		/// <summary>
		/// 取得前台商品詳細資料
		/// </summary>
		/// <param name="productId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		Task<ProdProductDetailDto?> GetFrontProductDetailAsync(int productId, CancellationToken ct = default);
    }
}
