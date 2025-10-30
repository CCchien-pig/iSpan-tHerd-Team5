using System.Text;
using tHerdBackend.Core.DTOs.PROD;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace tHerdBackend.Infra.Repository.PROD.Builders
{
    /// <summary>
    /// 組 SQL 條件、排序、分頁
    /// </summary>
    public static class ProductQueryBuilder
    {
        public static void AppendFilters(StringBuilder sql, ProductFilterQueryDto query)
        {
			if (query.ProductId.HasValue)
				sql.Append(" AND p.ProductId = @ProductId");
			if (!string.IsNullOrWhiteSpace(query.Keyword))
                sql.Append(" AND p.ProductName LIKE CONCAT('%', @Keyword, '%')");
            if (query.BrandId.HasValue)
                sql.Append(" AND p.BrandId = @BrandId");
            if (query.ProductTypeId.HasValue)
                sql.Append(" AND t.ProductTypeId = @ProductTypeId");
            if (query.MinPrice.HasValue)
                sql.Append(" AND sp.MinSkuPrice >= @MinPrice");
            if (query.MaxPrice.HasValue)
                sql.Append(" AND sp.MaxSkuPrice <= @MaxPrice");
            if (query.IsPublished.HasValue)
                sql.Append(" AND p.IsPublished = @IsPublished");
        }

        public static string BuildOrderClause(ProductFilterQueryDto query) => query.SortBy switch
        {
            "price" when query.SortDesc => " ORDER BY p.UnitPrice DESC",
            "price" => " ORDER BY p.UnitPrice ASC",
            "name" when query.SortDesc => " ORDER BY p.ProductName DESC",
            "name" => " ORDER BY p.ProductName ASC",
            _ => " ORDER BY p.ProductId DESC"
        };
    }
}
