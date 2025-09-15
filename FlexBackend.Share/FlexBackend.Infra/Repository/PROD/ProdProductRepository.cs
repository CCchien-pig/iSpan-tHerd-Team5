using Dapper;
using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Infra.DBSetting;
using FlexBackend.Infra.Helpers;
using FlexBackend.Infra.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace FlexBackend.Infra.Repository.PROD
{
    public class ProdProductRepository : IProdProductRepository
    {
        private readonly ISqlConnectionFactory _factory;     // 給「純查詢」或「無交易時」使用
        private readonly tHerdDBContext _db;                 // 寫入與交易來源

        public ProdProductRepository(ISqlConnectionFactory factory, tHerdDBContext db)
        {
            _factory = factory;
            _db = db;
        }

        public async Task<IEnumerable<ProdProductDto>> GetAllAsync(CancellationToken ct = default)
        {
            string sql = @"SELECT p.ProductId, p.ProductName, su.SupplierId, su.SupplierName,
                p.BrandId, s.BrandName, p.SeoId, p.ProductCode,
                p.ShortDesc, p.FullDesc, p.IsPublished,
                p.Weight, p.VolumeCubicMeter, p.VolumeUnit, p.Creator,
                p.CreatedDate, p.Reviser, p.RevisedDate, tc.ProductTypeName
                FROM PROD_Product p
                JOIN SUP_Brand s ON s.BrandId=p.BrandId 
                LEFT JOIN SUP_Supplier su ON su.SupplierId=s.SupplierId
                LEFT JOIN PROD_ProductType t ON t.ProductId=p.ProductId
                LEFT JOIN PROD_ProductTypeConfig tc ON tc.ProductTypeId=t.ProductTypeId
                ORDER BY ProductId DESC;";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var cmd = new CommandDefinition(sql, transaction: tx, cancellationToken: ct);
                var repo = new AspnetusersNameRepository(_factory, _db);
                var emp = await repo.GetAllUserNameAsync(ct);
                // 原始查詢結果 (會有重複 ProductId)
                var raw = conn.Query<ProdProductDto, string, ProdProductDto>(
                    sql,
                    (p, typeName) =>
                    {
                        p.ProductTypeDesc = new List<string>();
                        if (!string.IsNullOrEmpty(typeName))
                            p.ProductTypeDesc.Add(typeName);
                        return p;
                    },
                    splitOn: "ProductTypeName",
                    transaction: tx);

                // GroupBy → 合併 ProductTypeNames
                var list = raw
                    .GroupBy(p => p.ProductId)
                    .Select(g =>
                    {
                        var first = g.First();
                        first.ProductTypeDesc = g.SelectMany(x => x.ProductTypeDesc).Distinct().ToList();
                        return first;
                    })
                    .ToList();

                foreach (var item in list) {
                    item.CreatorNm = emp.FirstOrDefault(e => e.UserNumberId == item.Creator)?.FullName;
                    item.ReviserNm = emp.FirstOrDefault(e => e.UserNumberId == item.Reviser)?.FullName;
                }
                return list;
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }

		public async Task<ProdProductDto?> GetByIdAsync(int ProductId, CancellationToken ct = default)
        {
            var sql = @"SELECT p.ProductId, p.BrandId, b.BrandName, p.SeoId, 
                            s.SupplierId, s.SupplierName, p.ProductCode, p.ProductName,
                            p.ShortDesc, p.FullDesc, p.IsPublished, p.Weight,
                            p.VolumeCubicMeter, p.VolumeUnit, p.Creator, 
                            p.CreatedDate, p.Reviser, p.RevisedDate
                            FROM PROD_Product p
                            JOIN SUP_Brand b ON b.BrandId=p.BrandId
                            JOIN SUP_Supplier s ON s.SupplierId=b.SupplierId
                                WHERE p.ProductId=@ProductId;";

            //         var seoSql = @"SELECT SeoId, RefTable, RefId, SeoSlug, 
            //                           SeoTitle, SeoDesc, CreatedDate, RevisedDate
            //                           FROM SEO_SeoConfig WHERE SeoId = @seoId;";

            //var skuSql = @"SELECT SkuId, SpecCode, SkuCode, Barcode, 
            //                         CostPrice, ListPrice, UnitPrice, SalePrice,
            //                         StockQty, SafetyStockQty, ReorderPoint,
            //                         MaxStockQty, IsAllowBackorder, ShelfLifeDays,
            //                         StartDate, EndDate, IsActive
            //                         FROM PROD_ProductSku
            //                         WHERE ProductId=@ProductId";

            //var specSql = @"SELECT SkuId, SpecificationOptionId, CreatedDate
            //                             FROM PROD_SkuSpecificationValue";

            //         var specOptionSql = @"";

            //         var specOptionSql = @"SELECT v.SkuId, v.SpecificationOptionId,
            //             o.SpecificationConfigId, o.OptionName,
            //             o.OrderSeq AS OptionOrderSeq, c.GroupName AS ConfigGroup, c.OrderSeq AS ConfigOrderSeq
            //             FROM PROD_SkuSpecificationValue v
            //             JOIN PROD_SpecificationOption o ON o.SpecificationOptionId=v.SpecificationOptionId
            //             JOIN PROD_SpecificationConfig c ON c.SpecificationConfigId=o.SpecificationConfigId;";

             


            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var cmd = new CommandDefinition(sql, new { ProductId }, tx, cancellationToken: ct);
                var item = await conn.QueryFirstOrDefaultAsync<ProdProductDto>(cmd);

                //var seocmd = new CommandDefinition(seoSql, new { Id = item.SeoId }, tx, cancellationToken: ct);
                //var seo = await conn.QueryFirstOrDefaultAsync<PRODSeoConfigDto>(cmd);

                var seo = await _db.SysSeoMeta.FirstOrDefaultAsync(s => s.SeoId == item.SeoId);

                item.Seo = new PRODSeoConfigDto
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

                // 取得 SKU 清單
                var sku = await _db.ProdProductSkus.FirstOrDefaultAsync(s => s.ProductId == item.ProductId);

				if (sku != null) {
					item.Sku = new ProdProductSkuDto
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
						IsActive = sku.IsActive
					};
                    var specValue = _db.ProdSkuSpecificationValues.Where(s => s.SkuId == item.Sku.SkuId);
                    var specOptionArr = specValue==null ? null : specValue.Select(s => s.SpecificationOptionId).ToArray();
                    var specOption = specOptionArr == null ? null : _db.ProdSpecificationOptions.Where(s => specOptionArr.Contains(s.SpecificationOptionId));
                    var specConfigArr = specOption == null ? null : specOption.Select(s => s.SpecificationConfigId).ToArray();
                    var specConfig = specConfigArr == null ? null : _db.ProdSpecificationConfigs.Where(s => specConfigArr.Contains(s.SpecificationConfigId));

                    item.SpecConfig = specConfig==null ? null : specConfig.Select(c => new ProdSpecificationConfigDto
                    {
                        SpecificationConfigId = c.SpecificationConfigId,
                        GroupName = c.GroupName,
                        OrderSeq = c.OrderSeq
                    }).ToList();

                    item.Sku.SpecOption = specOption==null ? null : specOption.Select(o => new ProdSpecificationOptionDto
                    {
                        SpecificationOptionId = o.SpecificationOptionId,
                        SpecificationConfigId = o.SpecificationConfigId,
                        OptionName = o.OptionName,
                        OrderSeq = o.OrderSeq
                    }).ToList();

                    item.Sku.SpecValue = specValue==null ? null : specValue.Select(v => new ProdSkuSpecificationValueDto
                    {
                        SkuId = v.SkuId,
                        SpecificationOptionId = v.SpecificationOptionId,
                        CreatedDate = v.CreatedDate
                    }).ToList();
                }

				return item;
			}
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }

        // 新增
        public async Task<int> AddAsync(ProdProductDto dto, CancellationToken ct = default)
        {
            var entity = dto.Adapt<ProdProduct>(MapsterConfig.Default);

            // 自動帶建立/異動者與時間（或放到 DbContext.SaveChangesAsync 統一處理）
            var now = DateTime.Now;
            var uid = 1003;

            entity.Creator = uid;
            entity.CreatedDate = now;
            entity.Reviser = uid;
            entity.RevisedDate = now;

            _db.ProdProducts.Add(entity);

            // 部分更新（PATCH：忽略 null）
            dto.Adapt(entity, MapsterConfig.Patch);
            //await _db.SaveChangesAsync(ct);

            var entry = _db.Entry(entity);
            System.Diagnostics.Debug.WriteLine($"BEFORE save: State={entry.State}, TempKey={entry.Property(p => p.ProductId).IsTemporary}");
            await _db.SaveChangesAsync(ct);
            entry = _db.Entry(entity);
            System.Diagnostics.Debug.WriteLine($"AFTER save:  State={entry.State}, TempKey={entry.Property(p => p.ProductId).IsTemporary}, Id={entity.ProductId}");


            return entity.ProductId;
        }

        public async Task<PagedResult<ProdProductDto>> QueryAsync(ProductQuery query, CancellationToken ct = default)
        {
            // 例：分頁 + 條件（查詢仍走 Dapper）
            var sb = new System.Text.StringBuilder(@"
                    SELECT ProductId, BrandId, SeoId, ProductName, ShortDesc, FullDesc, IsPublished,
                           Weight, VolumeCubicMeter, VolumeUnit, Creator, CreatedDate, Reviser, RevisedDate
                    FROM   PROD_Product WITH (NOLOCK)
                    WHERE  1 = 1 ");

            var param = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                sb.Append(" AND ProductName LIKE @kw ");
                param.Add("kw", $"%{query.Keyword}%");
            }
            if (query.IsPublished.HasValue)
            {
                sb.Append(" AND IsPublished = @pub ");
                param.Add("pub", query.IsPublished);
            }

            sb.Append(" ORDER BY ProductId DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY; ");
            param.Add("skip", (query.PageIndex - 1) * query.PageSize);
            param.Add("take", query.PageSize);

            const string sqlCount = @"SELECT COUNT(1) FROM PROD_Product WHERE 1=1
                                      /* 同條件…這裡可重用組裝或寫成 SP */";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var list = await conn.QueryAsync<ProdProductDto>(
                    new CommandDefinition(sb.ToString(), param, tx, cancellationToken: ct));

                // 這裡示範：為了簡潔，直接再跑一次 Count（正式可抽共用）
                var total = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(sqlCount, param, tx, cancellationToken: ct));

                return new PagedResult<ProdProductDto>
                {
                    Items = list.ToList(),
                    TotalCount = total,
                    Page = query.PageIndex,
                    PageSize = query.PageSize
                };
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }

        public async Task<bool> UpdateAsync(ProdProductDto dto, CancellationToken ct = default)
        {
            var item = await _db.ProdProducts.FirstOrDefaultAsync(x => x.ProductId == dto.ProductId, ct);
            if (item == null) return false;

            // 映射更新欄位
            item.BrandId = dto.BrandId;
            item.SeoId = dto.SeoId;
            item.ProductName = dto.ProductName;
            item.ShortDesc = dto.ShortDesc;
            item.FullDesc = dto.FullDesc;
            item.IsPublished = dto.IsPublished;
            item.Weight = dto.Weight;
            item.VolumeCubicMeter = dto.VolumeCubicMeter;
            item.VolumeUnit = dto.VolumeUnit;
            //entity.Reviser = dto.Reviser;
            item.RevisedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = _db.ProdProducts.Find(new object?[] { id }, ct);
            if (entity == null) return false;

            _db.ProdProducts.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<LoadBrandOptionDto>> LoadBrandOptionsAsync(CancellationToken ct = default)
        {
            const string sql = @"SELECT b.BrandId, b.BrandName, b.BrandCode,
                                b.SupplierId, s.SupplierName
                                FROM SUP_Brand b
                                JOIN SUP_Supplier s ON s.SupplierId = b.SupplierId";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var cmd = new CommandDefinition(sql, tx, cancellationToken: ct);
                return conn.Query<LoadBrandOptionDto>(cmd);
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }
    }
}
