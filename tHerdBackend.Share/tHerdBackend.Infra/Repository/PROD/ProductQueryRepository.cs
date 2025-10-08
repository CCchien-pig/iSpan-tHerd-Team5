using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using Dapper;
using tHerdBackend.Core.Interfaces.Products;

namespace tHerdBackend.Infra.Repository.PROD
{
	public class ProductQueryRepository : IProdProductQueryRepository
    {
        private readonly ISqlConnectionFactory _factory;     // 給「純查詢」或「無交易時」使用
        private readonly tHerdDBContext _db;                 // 寫入與交易來源

        public ProductQueryRepository(ISqlConnectionFactory factory, tHerdDBContext db)
        {
            _factory = factory;
            _db = db;
        }

        public async Task<IEnumerable<ProdProductQueryDto>> GetAllProductQueryListAsync(int productId, CancellationToken ct = default)
		{
            string sql = @"SELECT sk.SkuId, sk.SkuCode, p.ProductId, p.ProductName, 
                sk.Barcode, sk.CostPrice, sk.ListPrice, sk.UnitPrice,
                sk.SalePrice, sk.StockQty, sk.SafetyStockQty, sk.ReorderPoint, sk.MaxStockQty,
                sk.IsAllowBackorder, sk.ShelfLifeDays, p.ShortDesc, 
                p.Weight, p.VolumeCubicMeter, p.VolumeUnit,
                p.BrandId, s.BrandName, s.DiscountRate AS BrandDisCntRate, 
                s.IsDiscountActive AS BrandDisCntActive,
                s.SupplierId AS SupId, su.SupplierName AS SupName, 
                su.ContactName AS SupContact, su.Phone AS SupPhone, su.Email AS SupEmail,
                a.AttributeName, pa.AttributeValue, tc.ProductTypeName,
                sc.GroupName AS SpecGroup, so.OptionName AS SpecName
                FROM PROD_Product p
                JOIN SUP_Brand s ON s.BrandId=p.BrandId 
                LEFT JOIN SUP_Supplier su ON su.SupplierId=s.SupplierId
                LEFT JOIN PROD_ProductAttribute pa ON pa.ProductId=p.ProductId
                LEFT JOIN PROD_Attribute a ON a.AttributeId=pa.AttributeId
                LEFT JOIN PROD_ProductType t ON t.ProductId=p.ProductId
                LEFT JOIN PROD_ProductTypeConfig tc ON tc.ProductTypeId=t.ProductTypeId
                LEFT JOIN PROD_ProductSku sk ON sk.ProductId=p.ProductId
                LEFT JOIN PROD_SpecificationConfig sc ON sc.ProductId=p.ProductId
                LEFT JOIN PROD_SkuSpecificationValue sv ON sv.SkuId=sk.SkuId
                LEFT JOIN PROD_SpecificationOption so on so.SpecificationOptionId=sv.SpecificationOptionId";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

            var lookup = new Dictionary<int, ProdProductQueryDto>();

            var result = await conn.QueryAsync<dynamic>(
                sql,
                new { ProductId = productId == 0 ? (int?)null : productId }, // 0 = 查全部
                transaction: tx
            );

            foreach (var row in result)
            {
                int pid = row.ProductId;
                if (!lookup.TryGetValue(pid, out var dto))
                {
                    dto = new ProdProductQueryDto
                    {
                        ProductId = pid,
                        ProductName = row.ProductName,

                        SupId = (int?)CheckValueNull(row.SupId),
                        SupName = row.SupName,
                        
                        BrandId = (int?)CheckValueNull(row.BrandId),
                        BrandName = row.BrandName,
                        BrandDisCntRate = (decimal?)CheckValueNull(row.BrandDisCntRate) ?? 0,
                        BrandDisCntActive = row.BrandDisCntActive,

                        SupContact = row.SupContact,
                        SupPhone = row.SupPhone,
                        SupEmail = row.SupEmail,

                        ShortDesc = row.ShortDesc,
                        Weight = (decimal?)CheckValueNull(row.Weight) ?? 0,
                        VolumeCubicMeter = (decimal?)CheckValueNull(row.VolumeCubicMeter) ?? 0,
                        VolumeUnit = row.VolumeUnit,

                        SkuId = (int?)CheckValueNull(row.skuId),
                        SpecCode = row.specCode,
                        SkuCode = row.SkuCode,
                        Barcode = row.Barcode,
                        CostPrice = (decimal?)CheckValueNull(row.CostPrice) ?? 0,
                        ListPrice = (decimal?)CheckValueNull(row.ListPrice) ?? 0,
                        UnitPrice = (decimal?)CheckValueNull(row.UnitPrice) ?? 0,
                        SalePrice = (decimal?)CheckValueNull(row.SalePrice) ?? 0,
                        StockQty = (int?)CheckValueNull(row.StockQty),
                        SafetyStockQty = (int?)CheckValueNull(row.SafetyStockQty),
                        ReorderPoint = (int?)CheckValueNull(row.ReorderPoint),
                        MaxStockQty = (int?)CheckValueNull(row.MaxStockQty),
                        IsAllowBackorder = row.IsAllowBackorder,
                        ShelfLifeDays = (int?)CheckValueNull(row.ShelfLifeDays),

                        ProductTypeName = row.ProductTypeName,
                        Types = new List<ProdProductTypeQueryDto>(),

                        Spec = string.Empty,
                        SpecOptions = new List<ProdSpecificationQueryDto>(),

                        Attribute = string.Empty,
                        AttributeOptions = new List<ProdAttributeOptionQueryDto>(),

                        Bundle = string.Empty,
                        BundleItems = new List<ProdBundleItemQueryDto>(),

                        Ingredient = string.Empty,
                        Ingredients = new List<ProdProductIngredientQueryDto>()
                    };
                    lookup.Add(pid, dto);
                }

                // 規格 Option
                if (row.SpecificationOptionId != null && !dto.SpecOptions.Any(x => x.SpecificationOptionId == row.SpecificationOptionId))
                {
                    dto.SpecOptions.Add(new ProdSpecificationQueryDto { SpecificationOptionId = row.SpecificationOptionId, OptionName = row.SpecName });
                    dto.Spec = string.Join(",", dto.SpecOptions.Select(s => $"{s.GroupName}:{s.OptionName}"));
                }

                // 屬性 Option
                if (row.AttributeOptionId != null && !dto.AttributeOptions.Any(x => x.AttributeOptionId == row.AttributeOptionId))
                {
                    dto.AttributeOptions.Add(new ProdAttributeOptionQueryDto { AttributeOptionId = row.AttributeOptionId, OptionName = row.AttributeOptionName });
                    dto.Attribute = string.Join(",", dto.AttributeOptions.Select(s => $"{s.AttributeName}:{s.OptionName}{(string.IsNullOrWhiteSpace(s.OptionValue)==false ? s.OptionValue : string.Empty)}"));
                }
            }

            if (needDispose) conn.Dispose();

            return lookup.Values.ToList();
        }

        /// <summary>
        /// 檢查數值是否正確
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object? CheckValueNull(dynamic value)
        {
            if (value == null || value is DBNull)
                return null;

            return value;
        }
    }
}
