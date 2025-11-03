using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.Interfaces.CNT
{
	public interface ITagProductReadRepository
	{
		// 取得某個 tagId 底下商品的分頁結果（排序後）
		// 回傳:
		// total 總筆數
		// rows 這一頁的商品清單 (包含 ProductId / ProductName / MainSkuIdRaw ...等欄位)
		Task<(int total, List<TagProductRecord> rows)>
			GetTagProductsPageAsync(int tagId, int page, int pageSize);

		// 給一批 skuId，取得價格資訊 (SalePrice / ListPrice / UnitPrice)
		// skuId -> (SalePrice, ListPrice, UnitPrice)
		Task<Dictionary<int, (decimal? SalePrice, decimal? ListPrice, decimal? UnitPrice)>>
			GetSkuPriceMapAsync(IEnumerable<int> skuIds);

		// 給一批 productId，取得主圖網址
		// productId -> 圖片URL
		Task<Dictionary<int, string>> GetMainImageMapAsync(IEnumerable<int> productIds);

		// 給一批 productId，取得評價 (平均星數、評論數)
		// productId -> (AvgRating, ReviewCount)
		Task<Dictionary<int, (decimal? Avg, int? Count)>>
			GetReviewMapAsync(IEnumerable<int> productIds);
	}
}
