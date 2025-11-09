using System.Text;
using tHerdBackend.Core.DTOs.PROD;

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

			// 多欄位模糊搜尋（商品名 + 品牌名 + 分類名）

			if (!string.IsNullOrWhiteSpace(query.Keyword))
			{
				sql.Append(@"
					AND (
						p.ProductName LIKE CONCAT('%', @Keyword, '%') 
						OR s.BrandName LIKE CONCAT('%', @Keyword, '%') 
						OR cp.ProductTypePath LIKE CONCAT('%', @Keyword, '%') 
					)
				");

                // 若 Keyword 是數字，加入 ProductId 精確比對
                if (int.TryParse(query.Keyword, out _))
                {
                    sql.Append(" OR p.ProductId = @Keyword ");
                }
            }

            // 品牌篩選（單選 / 多選兩者兼容）
            if (query.BrandIds != null && query.BrandIds.Any())
            {
                sql.Append(" AND p.BrandId IN @BrandIds ");
            }
            else if (query.BrandId.HasValue)
            {
                sql.Append(" AND p.BrandId = @BrandId ");
            }

            // 類別
            if (query.ProductTypeId.HasValue)
				sql.Append(" AND EXISTS (SELECT 1 FROM TypeHierarchy t2 WHERE t2.ProductId = p.ProductId AND t2.ProductTypeId = @ProductTypeId)");

            // 價格篩選 — 使用與排序相同的 COALESCE 優先順序
            if (query.MinPrice.HasValue)
            {
                sql.Append(@"
                    AND COALESCE(
                        NULLIF(ps.SalePrice, 0),
                        NULLIF(ps.UnitPrice, 0),
                        NULLIF(ps.ListPrice, 0)
                    ) >= @MinPrice
                ");
            }

            if (query.MaxPrice.HasValue && query.MaxPrice > 0)
            {
                sql.Append(@"
                    AND COALESCE(
                        NULLIF(ps.SalePrice, 0),
                        NULLIF(ps.UnitPrice, 0),
                        NULLIF(ps.ListPrice, 0)
                    ) <= @MaxPrice
                ");
            }

            // 評分篩選（支援多選）
            if (query.Rating != null && query.Rating.Any())
            {
                sql.Append(" AND (");
                sql.Append(" 0=1 "); // 用於開頭方便加 OR 條件

                foreach (var rating in query.Rating)
                {
                    // 例如選 5 和 4 星時： p.AvgRating >= 5 OR p.AvgRating >= 4
                    sql.Append($" OR (ISNULL(p.AvgRating, 0) >= {rating} AND ISNULL(p.AvgRating, 0) < {rating + 1}) ");
                }

                sql.Append(")");
            }

            // 商品發佈狀態
            if (query.IsPublished.HasValue)
				sql.Append(" AND p.IsPublished = @IsPublished");

            // 特殊標籤
            if (!string.IsNullOrWhiteSpace(query.Badge))
                sql.Append(" AND p.Badge = @Badge");

            // 特殊「其他條件」如 Hot / New 等
            if (!string.IsNullOrWhiteSpace(query.Other))
            {
                switch (query.Other.ToLower())
                {
                    case "new":
                        sql.Append(" AND DATEDIFF(DAY, p.CreatedDate, GETDATE()) <= 30 ");
                        break;
                }
            }

            // 多商品 ID 篩選
            if (query.ProductIdList != null && query.ProductIdList.Count()>0)
                sql.Append(" AND p.ProductId IN @ProductIdList");

            if (query.AttributeFilters != null && query.AttributeFilters.Any())
            {
                int index = 0;

                foreach (var attr in query.AttributeFilters)
                {
                    // 每個屬性用一層 EXISTS
                    sql.Append($@"
            AND EXISTS (
                SELECT 1
                FROM PROD_ProductAttribute pa{index}
                WHERE pa{index}.ProductId = p.ProductId
                  AND pa{index}.AttributeId = {attr.AttributeId}
        ");

                    // 若該屬性有多個選項，用 OR 組起來
                    if (attr.ValueNames != null && attr.ValueNames.Count > 0)
                    {
                        sql.Append(" AND (");

                        for (int j = 0; j < attr.ValueNames.Count; j++)
                        {
                            string paramName = $"@Attr{index}_{j}";

                            // 🔹 改這裡：比對 OptionName 或 OptionValue，而不是 ValueName
                            sql.Append($@"
                    pa{index}.AttributeOptionId IN (
                        SELECT ao.AttributeOptionId 
                        FROM PROD_AttributeOption ao 
                        WHERE ao.AttributeId = pa{index}.AttributeId 
                          AND (ao.OptionName = {paramName} OR ao.OptionValue = {paramName})
                    )");

                            if (j < attr.ValueNames.Count - 1)
                                sql.Append(" OR ");
                        }

                        sql.Append(")");
                    }

                    sql.Append(")");
                    index++;
                }
            }
        }

        /// <summary>
        /// 組合排序子句（支援 SortBy + SortDesc + 熱銷順位）
        /// </summary>
        public static string BuildOrderClause(ProductFrontFilterQueryDto query)
		{
			var sb = new StringBuilder(" ORDER BY ");

			var direction = query.SortDesc ? "DESC" : "ASC";

            // 熱銷商品：直接依 RankNo 排序（JOIN HotRank hr）
            if (query.Other == "Hot")
            {
                sb.Append(" MIN(hr.RankNo) ASC ");
                return sb.ToString();
            }

            // 根據 SortBy 決定排序欄位
            switch (query.SortBy?.ToLower())
			{
                case "price":
                case "unitprice":
                case "billingprice":
                    sb.Append($@"
							COALESCE(
								NULLIF(ps.SalePrice, 0),
								NULLIF(ps.UnitPrice, 0),
								NULLIF(ps.ListPrice, 0)
							) {direction}
						");
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

                case "rating":
                    sb.Append($"p.AvgRating {direction}");
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
