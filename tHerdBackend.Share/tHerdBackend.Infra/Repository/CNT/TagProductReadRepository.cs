using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Models;
using System.Data;
using Dapper;


namespace tHerdBackend.Infra.Repository.CNT
{
	public class TagProductReadRepository : ITagProductReadRepository
	{
		private readonly tHerdDBContext _db;

		public TagProductReadRepository(tHerdDBContext db)
		{
			_db = db;
		}

		// 1. 取得標籤底下的商品（已排序＋分頁）+ 基礎資料
		public async Task<(int total, List<TagProductRecord> rows)>
			GetTagProductsPageAsync(int tagId, int page, int pageSize)
		{
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = 24;

			var baseQuery =
				from pt in _db.CntProductTags
				where pt.TagId == tagId
					  && pt.IsVisible == true
					  && pt.IsDeleted == false
					  && pt.Product.IsPublished == true
				orderby
					pt.IsPrimary descending,
					pt.DisplayOrder ascending,
					// 最新修改(有修用 RevisedDate，沒修用 CreatedDate)
					(pt.Product.RevisedDate ?? pt.Product.CreatedDate) descending
				select new TagProductRecord
				{
					ProductId = pt.Product.ProductId,
					ProductName = pt.Product.ProductName,
					Badge = pt.Product.Badge,
					IsPublished = pt.Product.IsPublished,
					MainSkuIdRaw = pt.Product.MainSkuId,

					ProductCode = pt.Product.ProductCode,
					Creator = pt.Product.Creator,
					CreatedDate = pt.Product.CreatedDate,
					Reviser = pt.Product.Reviser,
					RevisedDate = pt.Product.RevisedDate,

					// ✅ 品牌名稱：我們先猜 SupBrand 裡的屬性叫 BrandName
					// 如果編譯說 SupBrand 沒有 BrandName，請改成實際欄位 (ex: BrandTitle)
					BrandName = pt.Product.Brand != null
									 ? pt.Product.Brand.BrandName
									 : string.Empty,

					// 分類名稱：需要看 ProdProductTypeConfig 才能確定叫什麼
					// 暫時留空，之後你貼 ProdProductTypeConfig 再補
					ProductTypeName = string.Empty
				};

			var total = await baseQuery.CountAsync();

			var rows = await baseQuery
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (total, rows);
		}

		// 2. 取 SKU 價格資訊：SalePrice / ListPrice / UnitPrice
		public async Task<Dictionary<int, (decimal? SalePrice, decimal? ListPrice, decimal? UnitPrice)>>
			GetSkuPriceMapAsync(IEnumerable<int> skuIds)
		{
			var idList = skuIds
				.Distinct()
				.ToList();

			if (!idList.Any())
			{
				return new Dictionary<int, (decimal?, decimal?, decimal?)>();
			}

			// 直接從 ProdProductSku (你貼的 model) 查價格
			var data = await _db.ProdProductSkus
				.Where(s => idList.Contains(s.SkuId))
				.Select(s => new
				{
					s.SkuId,
					s.SalePrice,
					s.ListPrice,
					s.UnitPrice
				})
				.ToListAsync();

			return data.ToDictionary(
				x => x.SkuId,
				x => (x.SalePrice, x.ListPrice, x.UnitPrice)
			);
		}

		// 3. 取主圖：依 ProductId，把主圖或排序第一張的 FileUrl 拉出來
		public async Task<Dictionary<int, string>> GetMainImageMapAsync(IEnumerable<int> productIds)
		{
			var pidList = productIds.Distinct().ToList();
			if (!pidList.Any())
				return new Dictionary<int, string>();

			// 我們有：
			// ProdProductImage {
			//    ProductId, IsMain(bool), OrderSeq(int),
			//    ImgId(int?) -> 導覽 Img : SysAssetFile { FileUrl }
			// }
			//
			// 策略：
			//   - 同一個 productId 的多張圖裡
			//   - 先挑 IsMain = true 的，再用 OrderSeq 最小
			//   - 如果沒有 IsMain，就至少會被 OrderSeq 排在前面
			//
			// 注意：要能拿到 Img.FileUrl，必須 Include Img 才能投影。
			// 但我們是用 GroupBy+First() 的型式來壓成一筆。
			//
			var data = await _db.ProdProductImages
					.Where(img => pidList.Contains(img.ProductId))
					.OrderByDescending(img => img.IsMain)  // 先排 IsMain=1
					.ThenBy(img => img.OrderSeq)           // 再排你的圖片順序
					.Select(img => new {
						img.ProductId,
						ImageUrl = img.Img.FileUrl
					})
					.ToListAsync();

			// 現在 data 可能有同一個 ProductId 多筆，因為同一個商品多張圖。
			// 我們要取每個 ProductId 的第一張。
			var dict = new Dictionary<int, string>();
			foreach (var row in data)
			{
				// 每個商品只取遇到的第一張（因為上面已經排好：主圖 → 排序小 → 其他）
				if (!dict.ContainsKey(row.ProductId))
					dict[row.ProductId] = row.ImageUrl ?? string.Empty;
			}

			return dict;
		}

		// 4. 取評價：平均星等、評論數
		//    👉 直接從 PROD_Product 的 AvgRating / ReviewCount 讀出來
		public async Task<Dictionary<int, (decimal? Avg, int? Count)>>
			GetReviewMapAsync(IEnumerable<int> productIds)
		{
			var pidList = productIds
				.Distinct()
				.ToList();

			if (!pidList.Any())
			{
				return new Dictionary<int, (decimal?, int?)>();
			}

			const string sql = @"
					SELECT ProductId, AvgRating, ReviewCount
					FROM PROD_Product
					WHERE ProductId IN @Ids;
				";

			// 從 EF 的 DbContext 拿同一條連線出來給 Dapper 用
			var conn = _db.Database.GetDbConnection();
			var needClose = conn.State != ConnectionState.Open;

			try
			{
				if (needClose)
				{
					await conn.OpenAsync();
				}

				// 用 Dapper 把資料撈回來
				var rows = await conn.QueryAsync<ProductRatingRow>(
					sql,
					new { Ids = pidList }
				);

				// 轉成 productId -> (Avg, Count) 的字典
				return rows.ToDictionary(
					x => x.ProductId,
					x => (x.AvgRating, x.ReviewCount)
				);
			}
			finally
			{
				// 如果原本是關閉狀態，就幫它關回去
				if (needClose && conn.State == ConnectionState.Open)
				{
					conn.Close();
				}
			}
		}

		// 給 Dapper 用的小 DTO（只在這個 Repository 裡用）
		private sealed class ProductRatingRow
		{
			public int ProductId { get; set; }
			public decimal? AvgRating { get; set; }
			public int? ReviewCount { get; set; }
		}
	}
}
