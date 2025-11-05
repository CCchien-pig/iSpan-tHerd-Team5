using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.PROD.Assemblers
{
    /// <summary>
    /// 商品組裝服務
    /// </summary>
    public class ProductAssembler
    {
        private readonly tHerdDBContext _db;
        private readonly ISqlConnectionFactory _factory;

        public ProductAssembler(tHerdDBContext db, ISqlConnectionFactory factory)
        {
            _db = db;
            _factory = factory;
        }

        public async Task AssembleDetailsAsync(ProdProductDetailDto item, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
        {
            // 1. SEO
            var seo = await _db.SysSeoMeta.FirstOrDefaultAsync(s => 
            s.SeoId == item.SeoId && 
            s.RefTable == "Products" && 
            s.RefId == item.ProductId, ct);

            if (seo != null)
            {
                item.Seo = new ProdSeoConfigDto
                {
                    SeoId = seo.SeoId,
                    RefTable = seo.RefTable,
                    RefId = seo.RefId,
                    SeoSlug = seo.SeoSlug,
                    SeoTitle = seo.SeoTitle,
                    SeoDesc = seo.SeoDesc,
                    CreatedDate = seo.CreatedDate,
                    RevisedDate = seo.RevisedDate
                };
            }

            // 2. SKU
            var skus = await _db.ProdProductSkus.Include(s => s.SpecificationOptions)
                .Where(s => s.ProductId == item.ProductId).ToListAsync(ct);
            item.Skus = skus.Select(MapSku).ToList();

			if (item.Skus?.Any() == true)
			{
				var main = item.Skus.FirstOrDefault(x => x.SkuId == item.MainSkuId)
						   ?? item.Skus.First();

				item.ListPrice = main.ListPrice;
				item.UnitPrice = main.UnitPrice;
				item.SalePrice = main.SalePrice;
			}

			// 3. 規格群組
			item.SpecConfigs = await BuildSpecConfigsAsync(item, _db);

            var configNames = item.SpecConfigs.Select(s => s.SpecificationConfigId);

			foreach (var s in item.Skus)
			{
				var optionNames = new List<string>();

				foreach (var c in configNames)
				{
					var config = item.SpecConfigs.FirstOrDefault(a => a.SpecificationConfigId == c);
					var option = config?.SpecOptions.FirstOrDefault(so => so.SkuId == s.SkuId);

					if (!string.IsNullOrWhiteSpace(option?.OptionName))
						optionNames.Add(option.OptionName);
				}

				s.OptionName = string.Join(" / ", optionNames);
			}

			// 4. 分類
			item.Types = await _db.ProdProductTypes
                .Where(t => t.ProductId == item.ProductId)
                .Select(t => new ProdProductTypeDto { ProductTypeId = t.ProductTypeId, ProductId = t.ProductId, IsPrimary = t.IsPrimary })
                .OrderByDescending(t => t.IsPrimary).ToListAsync(ct);

            // 5. 圖片
            const string imgSql = @"SELECT pi.ImageId, pi.ProductId, pi.SkuId, pi.ImgId, pi.IsMain, 
                                pi.OrderSeq, af.FileUrl, af.AltText,
                                af.Caption, af.Width, af.Height, af.FileKey, 
                                af.FileExt, af.IsExternal, af.MimeType, 
                                af.FileSizeBytes, af.CreatedDate
                                FROM PROD_ProductImage pi
                                JOIN SYS_AssetFile af ON pi.ImgId = af.FileId
                                WHERE pi.ProductId = @ProductId
                                ORDER BY pi.IsMain DESC, pi.OrderSeq ASC;";
            var imgCmd = new CommandDefinition(imgSql, new { item.ProductId }, tx, cancellationToken: ct);
            item.Images = conn.Query<ProductImageDto>(imgCmd).OrderByDescending(x => x.IsMain).ThenBy(x=>x.OrderSeq).ToList();

            if (item.Images != null) {
                item.ImageUrl = item.Images
                    .Where(x => x.IsMain)
                    .Select(x => x.FileUrl)
                    .FirstOrDefault()
                    ?? item.Images.Select(x => x.FileUrl).FirstOrDefault()
                    ?? "/images/no-image.png";
            }
        }

        private ProdProductSkuDto MapSku(ProdProductSku sku) => new()
        {
            SkuId = sku.SkuId,
            SpecCode = sku.SpecCode,
            SkuCode = sku.SkuCode,
            Barcode = sku.Barcode,
            CostPrice = sku.CostPrice,
            ListPrice = sku.ListPrice,
            UnitPrice = sku.UnitPrice,
            SalePrice = sku.SalePrice,
            StockQty = sku.StockQty,
            SafetyStockQty = sku.SafetyStockQty,
            ReorderPoint = sku.ReorderPoint,
            MaxStockQty = sku.MaxStockQty,
            IsAllowBackorder = sku.IsAllowBackorder,
            ShelfLifeDays = sku.ShelfLifeDays,
            StartDate = sku.StartDate,
            EndDate = sku.EndDate,
            IsActive = sku.IsActive,
            SpecValues = sku.SpecificationOptions.Select(o => new ProdSkuSpecificationValueDto
            {
                SkuId = sku.SkuId,
                SpecificationOptionId = o.SpecificationOptionId
            }).ToList()
        };

        private async Task<List<ProdSpecificationConfigDto>> BuildSpecConfigsAsync(ProdProductDetailDto item, tHerdDBContext db)
        {
            // 可直接複用先前 ProductSpecAssembler 實作
            return await ProductSpecAssembler.BuildSpecConfigsAsync(db, item.Skus);
        }
    }
}
