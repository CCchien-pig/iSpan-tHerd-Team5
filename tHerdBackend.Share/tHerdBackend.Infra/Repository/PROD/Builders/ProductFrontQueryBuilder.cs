using System.Text;
using tHerdBackend.Core.DTOs.PROD;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace tHerdBackend.Infra.Repository.PROD.Builders
{
	/// <summary>
	/// 組合商品查詢的 SQL 條件與排序邏輯
	/// 用於前台搜尋與後台管理共用
	/// </summary>
	public static class ProductFrontQueryBuilder
    {
		/// <summary>
		/// 組合查詢條件（WHERE 子句）
		/// </summary>
		public static void AppendFilters(StringBuilder sql, ProductFrontFilterQueryDto query)
        {
			if (query.ProductId.HasValue)
				sql.Append(" AND p.ProductId = @ProductId");

			// 🔹 多欄位模糊搜尋（商品名 + 品牌名 + 分類名）

			if (!string.IsNullOrWhiteSpace(query.Keyword))
			{
				sql.Append(@"
					AND (
						p.ProductName LIKE CONCAT('%', @Keyword, '%')
						OR s.BrandName LIKE CONCAT('%', @Keyword, '%')
						OR cp.ProductTypePath LIKE CONCAT('%', @Keyword, '%')
					)
				");
			}

			if (query.BrandId.HasValue)
				sql.Append(" AND p.BrandId = @BrandId");

			if (query.ProductTypeId.HasValue)
				sql.Append(" AND EXISTS (SELECT 1 FROM TypeHierarchy t2 WHERE t2.ProductId = p.ProductId AND t2.ProductTypeId = @ProductTypeId)");

			if (query.MinPrice.HasValue)
				sql.Append(" AND ps.UnitPrice >= @MinPrice");

			if (query.MaxPrice.HasValue)
				sql.Append(" AND ps.UnitPrice <= @MaxPrice");

			if (query.IsPublished.HasValue)
				sql.Append(" AND p.IsPublished = @IsPublished");

            if (!string.IsNullOrWhiteSpace(query.Badge))
                sql.Append(" AND p.Badge = @Badge");

            if (query.ProductIdList != null && query.ProductIdList.Count()>0)
                sql.Append(" AND p.ProductId IN @ProductIdList");
        }

		/// <summary>
		/// 組合排序子句（支援 SortBy + SortDesc）
		/// </summary>
		public static string BuildOrderClause(ProductFrontFilterQueryDto query)
		{
			var sb = new StringBuilder(" ORDER BY ");

			var direction = query.SortDesc ? "DESC" : "ASC";

			// 🔹 根據 SortBy 決定排序欄位
			switch (query.SortBy?.ToLower())
			{
				case "price":
				case "unitprice":
					sb.Append($"ps.UnitPrice {direction}");
					break;

				case "name":
				case "productname":
					sb.Append($"p.ProductName {direction}");
					break;

				case "brand":
				case "brandname":
					sb.Append($"s.BrandName {direction}");
					break;

				case "newest":
				case "createddate":
					sb.Append($"p.CreatedDate {direction}");
					break;

				case "id":
				case "productid":
					sb.Append($"p.ProductId {direction}");
					break;

				default:
					// 預設視為「相關性」＝最新
					sb.Append("p.ProductId DESC");
					break;
			}

			return sb.ToString();
		}
	}
}
