using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Infra.DBSetting;
using FlexBackend.Infra.Helpers;
using FlexBackend.Infra.Models;
using Dapper;
using FlexBackend.Core.Interfaces.Products;

namespace FlexBackend.Infra.Repository.PROD
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
            string sql = @"SELECT p.ProductId, p.ProductName, p.ShortDesc, 
                p.Weight, p.VolumeCubicMeter, p.VolumeUnit,
                p.BrandId, s.BrandName, s.DiscountRate AS BrandDisCntRate, 
                s.IsDiscountActive AS BrandDisCntActive,
                s.SupplierId AS SupId, su.SupplierName AS SupName, 
                su.ContactName AS SupContact, su.Phone AS SupPhone, su.Email AS SupEmail,
                a.AttributeName, pa.AttributeValue, tc.ProductTypeName,
                sk.SkuId, sk.SkuCode, sk.Barcode, sk.CostPrice, sk.ListPrice, sk.UnitPrice,
                sk.SalePrice, sk.StockQty, sk.SafetyStockQty, sk.ReorderPoint, sk.MaxStockQty,
                sk.IsAllowBackorder, sk.ShelfLifeDays, 
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
                LEFT JOIN PROD_SpecificationOption so on so.SpecificationOptionId=sv.SpecificationOptionId
                WHERE p.ProductId = @ProductId;";

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
                        ShortDesc = row.ShortDesc,
                        Weight = row.Weight,
                        VolumeCubicMeter = row.VolumeCubicMeter,
                        VolumeUnit = row.VolumeUnit,
                        BrandId = row.BrandId,
                        //BrandName = row.BrandName,
                        //BrandDisCntRate = row.BrandDisCntRate,
                        //BrandDisCntActive = row.BrandDisCntActive,
                        //SupId = row.SupId,
                        //SupName = row.SupName,
                        //SupContact = row.SupContact,
                        //SupPhone = row.SupPhone,
                        //SupEmail = row.SupEmail,
                        //ProductTypeName = row.ProductTypeName,
                        Skus = new List<ProdProductSkuDto>(),
                        Images = new List<ProdProductImageDto>(),
                        Types = new List<ProdProductTypeDto>(),
                        SpecConfigs = new List<ProdSpecificationConfigDto>(),
                        SpecOptions = new List<ProdSpecificationOptionDto>(),
                        AttributeOptions = new List<ProdAttributeOptionDto>(),
                        BundleItems = new List<ProdBundleItemDto>(),
                        Ingredients = new List<ProdProductIngredientDto>()
                    };
                    lookup.Add(pid, dto);
                }

                // SKU
                if (row.SkuId != null && !dto.Skus.Any(x => x.SkuId == row.SkuId))
                    dto.Skus.Add(new ProdProductSkuDto { SkuId = row.SkuId, SkuCode = row.SkuCode, Barcode = row.Barcode });

                // 規格 Config
                if (row.SpecificationConfigId != null && !dto.SpecConfigs.Any(x => x.SpecificationConfigId == row.SpecificationConfigId))
                    dto.SpecConfigs.Add(new ProdSpecificationConfigDto { SpecificationConfigId = row.SpecificationConfigId, GroupName = row.SpecGroup });

                // 規格 Option
                if (row.SpecificationOptionId != null && !dto.SpecOptions.Any(x => x.SpecificationOptionId == row.SpecificationOptionId))
                    dto.SpecOptions.Add(new ProdSpecificationOptionDto { SpecificationOptionId = row.SpecificationOptionId, OptionName = row.SpecName });

                // 屬性 Option
                if (row.AttributeOptionId != null && !dto.AttributeOptions.Any(x => x.AttributeOptionId == row.AttributeOptionId))
                    dto.AttributeOptions.Add(new ProdAttributeOptionDto { AttributeOptionId = row.AttributeOptionId, OptionName = row.AttributeOptionName });

                //// BundleItem
                //if (row.BundleItemId != null && !dto.BundleItems.Any(x => x.BundleItemId == row.BundleItemId))
                //    dto.BundleItems.Add(new ProdBundleItemDto { BundleItemId = row.BundleItemId, Quantity = row.BundleQty });

                //// Ingredient
                //if (row.IngredientId != null && !dto.Ingredients.Any(x => x.IngredientId == row.IngredientId))
                //    dto.Ingredients.Add(new ProdProductIngredientDto { IngredientId = row.IngredientId, IngredientName = row.IngredientName });
            }

            if (needDispose) conn.Dispose();

            return lookup.Values.ToList();
        }
	}
}
