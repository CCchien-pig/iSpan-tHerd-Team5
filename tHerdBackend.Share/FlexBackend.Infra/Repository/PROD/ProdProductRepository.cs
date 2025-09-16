using Dapper;
using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Infra.DBSetting;
using FlexBackend.Infra.Helpers;
using FlexBackend.Infra.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static Dapper.SqlMapper;

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

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var cmd = new CommandDefinition(sql, new { ProductId }, tx, cancellationToken: ct);
                var item = await conn.QueryFirstOrDefaultAsync<ProdProductDto>(cmd);

                if (item == null) return null;

                var seo = await _db.SysSeoMeta.FirstOrDefaultAsync(s => s.SeoId == item.SeoId);

                item.Seo = seo == null ? null : new PRODSeoConfigDto
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

                var skus = await _db.ProdProductSkus
                    .Include(s => s.SpecificationOptions)
                    .Where(s => s.ProductId == item.ProductId)
                    .ToListAsync();

                if (skus != null && skus.Any())
                {
                    item.Skus = skus.Select(_sku => new ProdProductSkuDto
                    {
                        SkuId = _sku.SkuId,
                        SpecCode = _sku.SpecCode,
                        SkuCode = _sku.SkuCode,
                        Barcode = _sku.Barcode,
                        CostPrice = _sku.CostPrice,
                        ListPrice = _sku.ListPrice,
                        UnitPrice = _sku.UnitPrice,
                        SalePrice = _sku.SalePrice,
                        StockQty = _sku.StockQty,
                        SafetyStockQty = _sku.SafetyStockQty,
                        ReorderPoint = _sku.ReorderPoint,
                        MaxStockQty = _sku.MaxStockQty,
                        IsAllowBackorder = _sku.IsAllowBackorder,
                        ShelfLifeDays = _sku.ShelfLifeDays,
                        StartDate = _sku.StartDate,
                        EndDate = _sku.EndDate,
                        IsActive = _sku.IsActive,
						SpecValues = _sku.SpecificationOptions.Select(o => new ProdSkuSpecificationValueDto
						{
							SkuId = _sku.SkuId,
							SpecificationOptionId = o.SpecificationOptionId
						}).ToList()
					}).ToList();
                }
                else
                {
                    item.Skus = new List<ProdProductSkuDto>(); // 沒有的話給空集合，避免 null reference
                }
                // 找出所有該商品用到的「規格群組」
                var specConfigIds = item.Skus
                    .SelectMany(s => s.SpecValues)
                    .Select(o => o.SpecificationOptionId)
                    .Distinct()
                    .ToList();

                if (specConfigIds.Any())
                {
                    var specConfigs = await _db.ProdSpecificationConfigs
                        .Where(c => specConfigIds.Contains(c.SpecificationConfigId))
                        .ToListAsync();

                    item.SpecConfigs = specConfigs.Select(c => new ProdSpecificationConfigDto
                    {
                        SpecificationConfigId = c.SpecificationConfigId,
                        GroupName = c.GroupName,
                        OrderSeq = c.OrderSeq,
						SpecOptions = c.ProdSpecificationOptions.Select(o => new ProdSpecificationOptionDto
						{
							SpecificationOptionId = o.SpecificationOptionId,
							SpecificationConfigId = o.SpecificationConfigId,
							OptionName = o.OptionName,
							OrderSeq = o.OrderSeq
						}).ToList()
					}).ToList();
                }
                else
                {
                    item.SpecConfigs = new List<ProdSpecificationConfigDto>();
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
			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

			using var tran = conn.BeginTransaction();
			try
			{
				// === 1. 新增 Product 主檔 ===
				var insertProductSql = @"
                    INSERT INTO PROD_Product
                    (BrandId, SeoId, ProductCode, ProductName,
                     ShortDesc, FullDesc, IsPublished, Weight,
                     VolumeCubicMeter, VolumeUnit, Creator, CreatedDate,
                     Reviser, RevisedDate)
                    OUTPUT INSERTED.ProductId
                    VALUES
                    (@BrandId, @SeoId, @ProductCode, @ProductName,
                     @ShortDesc, @FullDesc, @IsPublished, @Weight,
                     @VolumeCubicMeter, @VolumeUnit, @Creator, SYSDATETIME(),
                     @Reviser, SYSDATETIME());
                ";

				var productId = await conn.ExecuteScalarAsync<int>(insertProductSql, new
				{
					dto.BrandId,
					dto.SeoId,
					dto.ProductCode,
					dto.ProductName,
					dto.ShortDesc,
					dto.FullDesc,
					dto.IsPublished,
					dto.Weight,
					dto.VolumeCubicMeter,
					dto.VolumeUnit,
					Creator = 1003,
					Reviser = 1003
				}, tran);

				// === 2. 規格群組 & 選項 ===
				var insertCfgSql = @"
                    INSERT INTO PROD_SpecificationConfig (ProductId, GroupName, OrderSeq)
                    OUTPUT INSERTED.SpecificationConfigId
                    VALUES (@ProductId, @GroupName, @OrderSeq);
                ";

				var insertOptSql = @"
                    INSERT INTO PROD_SpecificationOption (SpecificationConfigId, OptionName, OrderSeq)
                    OUTPUT INSERTED.SpecificationOptionId
                    VALUES (@SpecificationConfigId, @OptionName, @OrderSeq);
                ";

				foreach (var cfg in dto.SpecConfigs ?? new())
				{
					var cfgId = await conn.ExecuteScalarAsync<int>(insertCfgSql, new
					{
						ProductId = productId,
						cfg.GroupName,
						cfg.OrderSeq
					}, tran);

					foreach (var opt in cfg.SpecOptions ?? new())
					{
						var optId = await conn.ExecuteScalarAsync<int>(insertOptSql, new
						{
							SpecificationConfigId = cfgId,
							opt.OptionName,
							opt.OrderSeq
						}, tran);

						// 回填回 DTO（方便後續用）
						opt.SpecificationOptionId = optId;
					}
				}

				// === 3. SKU & 規格對應 ===
				var insertSkuSql = @"
                    INSERT INTO PROD_ProductSku
                    (ProductId, SpecCode, SkuCode, Barcode,
                     CostPrice, ListPrice, UnitPrice, SalePrice,
                     StockQty, SafetyStockQty, ReorderPoint, MaxStockQty,
                     IsAllowBackorder, ShelfLifeDays, StartDate, EndDate,
                     IsActive)
                    OUTPUT INSERTED.SkuId
                    VALUES
                    (@ProductId, @SpecCode, @SkuCode, @Barcode,
                     @CostPrice, @ListPrice, @UnitPrice, @SalePrice,
                     @StockQty, @SafetyStockQty, @ReorderPoint, @MaxStockQty,
                     @IsAllowBackorder, @ShelfLifeDays, @StartDate, @EndDate,
                     @IsActive);
                ";

				var insertSkuSpecValueSql = @"
                    INSERT INTO PROD_SkuSpecificationValue (SkuId, SpecificationOptionId)
                    VALUES (@SkuId, @SpecificationOptionId);
                ";

				foreach (var skuDto in dto.Skus ?? new())
				{
					// 如果前端沒給 SkuCode → 預設 TEMP-xxxx
					if (string.IsNullOrEmpty(skuDto.SkuCode))
						skuDto.SkuCode = $"TEMP-{Guid.NewGuid():N}".Substring(0, 16);

					var skuId = await conn.ExecuteScalarAsync<int>(insertSkuSql, new
					{
						ProductId = productId,
						skuDto.SpecCode,
						skuDto.SkuCode,
						skuDto.Barcode,
						skuDto.CostPrice,
						skuDto.ListPrice,
						skuDto.UnitPrice,
						skuDto.SalePrice,
						skuDto.StockQty,
						skuDto.SafetyStockQty,
						skuDto.ReorderPoint,
						skuDto.MaxStockQty,
						skuDto.IsAllowBackorder,
						skuDto.ShelfLifeDays,
						skuDto.StartDate,
						skuDto.EndDate,
						skuDto.IsActive
					}, tran);

					// 建立 SKU 與規格選項對應
					foreach (var opt in skuDto.SpecValues ?? new())
					{
						if (opt.SpecificationOptionId > 0)
						{
							await conn.ExecuteAsync(insertSkuSpecValueSql, new
							{
								SkuId = skuId,
								opt.SpecificationOptionId
							}, tran);
						}
					}
				}

				// === 4. 刷新 SkuCode (把 TEMP- 改成正式碼) ===
				await RefreshSkuCodesAsync(productId, conn, tran);

				tran.Commit();
				return productId;
			}
			catch
			{
				tran.Rollback();
				throw;
			}
		}

        public async Task<bool> UpdateAsync(ProdProductDto dto, CancellationToken ct = default)
        {
            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

            using var tran = conn.BeginTransaction();

            try
            {
				// === 1. 更新 Product 主檔 ===
				var updateProductSql = @"
                    UPDATE PROD_Product
                    SET BrandId          = @BrandId,
                        SeoId            = @SeoId,
                        ProductCode      = @ProductCode,
                        ProductName      = @ProductName,
                        ShortDesc        = @ShortDesc,
                        FullDesc         = @FullDesc,
                        IsPublished      = @IsPublished,
                        Weight           = @Weight,
                        VolumeCubicMeter = @VolumeCubicMeter,
                        VolumeUnit       = @VolumeUnit,
                        Reviser          = @Reviser,
                        RevisedDate      = @RevisedDate
                    WHERE ProductId = @ProductId;
                ";

				await conn.ExecuteAsync(updateProductSql, new
				{
					dto.BrandId,
					dto.SeoId,
					dto.ProductCode,
					dto.ProductName,
					dto.ShortDesc,
					dto.FullDesc,
					dto.IsPublished,
					dto.Weight,
					dto.VolumeCubicMeter,
					dto.VolumeUnit,
					Reviser = 1003,              // 或 dto.Reviser
					RevisedDate = DateTime.Now,
					dto.ProductId
				}, tran);

				// === 2. 規格群組 SpecConfig ===
				var cfgIds = dto.SpecConfigs?.Select(c => c.SpecificationConfigId).ToList() ?? new();

                // 2-1 刪除不存在的
                var deleteCfgSql = @"
                    DELETE FROM PROD_SpecificationConfig
                    WHERE ProductId = @ProductId 
                      AND SpecificationConfigId NOT IN @CfgIds;
                ";
                await conn.ExecuteAsync(deleteCfgSql, new
                {
                    dto.ProductId,
                    CfgIds = cfgIds.Any() ? cfgIds : new List<int> { -1 }
                }, tran);

                // 2-2 新增/更新
                var upsertCfgSql = @"
                    MERGE PROD_SpecificationConfig AS target
                    USING (SELECT @SpecificationConfigId AS SpecificationConfigId) AS source
                    ON target.SpecificationConfigId = source.SpecificationConfigId
                    WHEN MATCHED THEN
                        UPDATE SET GroupName = @GroupName, OrderSeq = @OrderSeq
                    WHEN NOT MATCHED THEN
                        INSERT (ProductId, GroupName, OrderSeq)
                        VALUES (@ProductId, @GroupName, @OrderSeq);
                ";

				// 刪除不存在的選項
				var deleteOptSql = @"
                    DELETE FROM PROD_SpecificationOption
                    WHERE SpecificationConfigId = @SpecificationConfigId
                      AND SpecificationOptionId NOT IN @OptIds;
                ";

				var upsertOptSql = @"
                    MERGE PROD_SpecificationOption AS target
                    USING (SELECT @SpecificationOptionId AS SpecificationOptionId) AS source
                    ON target.SpecificationOptionId = source.SpecificationOptionId
                    WHEN MATCHED THEN
                        UPDATE SET OptionName = @OptionName, OrderSeq = @OrderSeq
                    WHEN NOT MATCHED THEN
                        INSERT (SpecificationConfigId, OptionName, OrderSeq)
                        VALUES (@SpecificationConfigId, @OptionName, @OrderSeq);
                ";

				foreach (var cfg in dto.SpecConfigs ?? new())
				{
					// 1 Upsert Config
					await conn.ExecuteAsync(upsertCfgSql, new
					{
						cfg.SpecificationConfigId,
						cfg.GroupName,
						cfg.OrderSeq,
						dto.ProductId
					}, tran);

					// 2️ Upsert Config 底下的 Options
					var optDtos = cfg.SpecOptions ?? new();
					var optIds = optDtos.Select(o => o.SpecificationOptionId ?? 0).ToList();

					// 2-1 刪除不存在的
					await conn.ExecuteAsync(deleteOptSql, new
					{
						cfg.SpecificationConfigId,
						OptIds = optIds.Any() ? optIds : new List<int> { -1 }
					}, tran);

					// 2-2 Upsert 每個 Option
					foreach (var optDto in optDtos)
					{
						await conn.ExecuteAsync(upsertOptSql, new
						{
							optDto.SpecificationOptionId,
							cfg.SpecificationConfigId,
							optDto.OptionName,
							optDto.OrderSeq
						}, tran);
					}
				}

				// === 3. SKU 與規格選項 ===
				var skuUpsertSql = @"
                    MERGE PROD_ProductSku AS target
                    USING (SELECT @SkuId AS SkuId) AS source
                    ON target.SkuId = source.SkuId
                    WHEN MATCHED THEN
                        UPDATE SET 
                            SkuCode          = @SkuCode, 
                            SpecCode         = @SpecCode, 
                            Barcode          = @Barcode,
                            CostPrice        = @CostPrice, 
                            ListPrice        = @ListPrice, 
                            UnitPrice        = @UnitPrice, 
                            SalePrice        = @SalePrice,
                            StockQty         = @StockQty,
                            SafetyStockQty   = @SafetyStockQty,
                            ReorderPoint     = @ReorderPoint,
                            MaxStockQty      = @MaxStockQty,
                            IsAllowBackorder = @IsAllowBackorder,
                            ShelfLifeDays    = @ShelfLifeDays,
                            StartDate        = @StartDate,
                            EndDate          = @EndDate,
                            IsActive         = @IsActive
                    WHEN NOT MATCHED AND @SkuCode LIKE 'TEMP-%' THEN
                        INSERT (
                            ProductId, SpecCode, SkuCode, Barcode,
                            CostPrice, ListPrice, UnitPrice, SalePrice,
                            StockQty, SafetyStockQty, ReorderPoint, MaxStockQty,
                            IsAllowBackorder, ShelfLifeDays, StartDate, EndDate,
                            IsActive
                        )
                        VALUES (
                            @ProductId, @SpecCode, @SkuCode, @Barcode,
                            @CostPrice, @ListPrice, @UnitPrice, @SalePrice,
                            @StockQty, @SafetyStockQty, @ReorderPoint, @MaxStockQty,
                            @IsAllowBackorder, @ShelfLifeDays, @StartDate, @EndDate,
                            @IsActive
                        );
                    ";

				// === SKU 與多個規格選項對應 ===
				var deleteSkuSpecValueSql = @"
                        DELETE FROM PROD_SkuSpecificationValue
                        WHERE SkuId = @SkuId;
                    ";

				var insertSkuSpecValueSql = @"
                        INSERT INTO PROD_SkuSpecificationValue (SkuId, SpecificationOptionId)
                        VALUES (@SkuId, @SpecificationOptionId);
                    ";

				foreach (var skuDto in dto.Skus ?? new())
                {
                    // 如果是 TEMP- 代表新建 → 之後要更新成正式碼
                    if (skuDto.SkuCode.StartsWith("TEMP-"))
                    {
                        var previews = await RefreshSkuCodesAsync(productId: dto.ProductId, conn, tran);
                        skuDto.SkuCode = previews.FirstOrDefault(r => r.SkuId == skuDto.SkuId).NewSkuCode;
                    }

					// 先 upsert SKU
					//await conn.ExecuteAsync(skuUpsertSql, skuDto, tran);
					await conn.ExecuteAsync(skuUpsertSql, new
					{
						skuDto.SkuId,
						skuDto.ProductId,
						skuDto.SpecCode,
						skuDto.SkuCode,
						skuDto.Barcode,
						skuDto.CostPrice,
						skuDto.ListPrice,
						skuDto.UnitPrice,   // 確保這裡真的有值
						skuDto.SalePrice,
						skuDto.StockQty,
						skuDto.SafetyStockQty,
						skuDto.ReorderPoint,
						skuDto.MaxStockQty,
						skuDto.IsAllowBackorder,
						skuDto.ShelfLifeDays,
						skuDto.StartDate,
						skuDto.EndDate,
						skuDto.IsActive
					}, tran);


					var optDtos = skuDto.SpecValues ?? new();
                    var optIds = optDtos.Select(o => o.SpecificationOptionId).ToList();

					// 先刪除舊的
					await conn.ExecuteAsync(deleteSkuSpecValueSql, new { skuDto.SkuId }, tran);

					// 再新增新的
					foreach (var optDto in optDtos)
					{
						if (optDto.SpecificationOptionId > 0)
						{
							await conn.ExecuteAsync(insertSkuSpecValueSql, new
							{
								skuDto.SkuId,
								optDto.SpecificationOptionId
							}, tran);
						}
					}
				}

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = _db.ProdProducts.Find(new object?[] { id }, ct);
            if (entity == null) return false;

            _db.ProdProducts.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<(int SkuId, string NewSkuCode)>> RefreshSkuCodesAsync(
    int? productId = null,
    IDbConnection? conn = null,
    IDbTransaction? tran = null)
        {
            var disposeLocal = false;
            if (conn == null)
            {
                (conn, _, disposeLocal) = await DbConnectionHelper.GetConnectionAsync(_db, _factory);
            }

            if (productId.HasValue)
            {
                // 👉 單一 ProductId：只回傳，不更新
                //    var sqlPreview = @"
                //;WITH sku_seq AS (
                //    SELECT
                //        s.SkuId,
                //        s.ProductId,
                //        s.SpecCode,
                //        p.ProductCode,
                //        b.BrandCode,
                //        tc.ProductTypeCode,
                //        ROW_NUMBER() OVER (PARTITION BY p.ProductId ORDER BY s.SkuId) AS SeqNo
                //    FROM PROD_ProductSku s
                //    JOIN PROD_Product p ON p.ProductId = s.ProductId
                //    JOIN SUP_Brand b ON b.BrandId = p.BrandId
                //    LEFT JOIN PROD_ProductType t ON t.ProductId = p.ProductId AND t.IsPrimary = 1
                //    JOIN PROD_ProductTypeConfig tc ON tc.ProductTypeId = t.ProductTypeId
                //    WHERE s.ProductId = @ProductId
                //      AND (s.SkuCode IS NULL OR s.SkuCode LIKE 'TEMP-%')
                //)
                //    SELECT 
                //        q.SkuId,
                //        CONCAT_WS('-', 
                //            q.BrandCode, 
                //            q.ProductTypeCode, 
                //            q.ProductCode, 
                //            RIGHT('0000' + CAST(q.SeqNo AS VARCHAR(4)), 4), 
                //            q.SpecCode
                //        ) AS NewSkuCode
                //    FROM sku_seq q;
                //";

                var sqlPreview = @";WITH sku_seq AS (
                                    SELECT
                                        s.SkuId,
                                        s.ProductId,
                                        s.SpecCode,
                                        p.ProductCode,
                                        b.BrandCode,
                                        COALESCE(tc.ProductTypeCode, 'PT') AS ProductTypeCode, -- 預設 ""PT""
                                        ROW_NUMBER() OVER (PARTITION BY p.ProductId ORDER BY s.SkuId) AS SeqNo
                                    FROM PROD_ProductSku s
                                    JOIN PROD_Product p ON p.ProductId = s.ProductId
                                    JOIN SUP_Brand b ON b.BrandId = p.BrandId
                                    LEFT JOIN PROD_ProductType t ON t.ProductId = p.ProductId AND t.IsPrimary = 1
                                    LEFT JOIN PROD_ProductTypeConfig tc ON tc.ProductTypeId = t.ProductTypeId
                                    WHERE s.ProductId = @ProductId
                                      AND (s.SkuCode IS NULL OR s.SkuCode LIKE 'TEMP-%')
                                )
                                SELECT 
                                    q.SkuId,
                                    CONCAT_WS('-', 
                                        q.BrandCode, 
                                        q.ProductTypeCode, 
                                        q.ProductCode, 
                                        RIGHT('0000' + CAST(q.SeqNo AS VARCHAR(4)), 4), 
                                        q.SpecCode
                                    ) AS NewSkuCode
                                FROM sku_seq q;
                                ";

                var results = await conn.QueryAsync<(int SkuId, string NewSkuCode)>(
                    sqlPreview, new { ProductId = productId }, tran);

                if (disposeLocal)
                    conn.Dispose();

                return results;
            }
            else
            {
                // 👉 沒有指定 productId → 直接更新所有 TEMP- 的 SKU
                var sqlUpdate = @"
            ;WITH sku_seq AS (
                SELECT
                    s.SkuId,
                    s.ProductId,
                    s.SpecCode,
                    p.ProductCode,
                    b.BrandCode,
                    tc.ProductTypeCode,
                    ROW_NUMBER() OVER (PARTITION BY p.ProductId ORDER BY s.SkuId) AS SeqNo
                FROM PROD_ProductSku s
                JOIN PROD_Product p ON p.ProductId = s.ProductId
                JOIN SUP_Brand b ON b.BrandId = p.BrandId
                LEFT JOIN PROD_ProductType t ON t.ProductId = p.ProductId AND t.IsPrimary = 1
                JOIN PROD_ProductTypeConfig tc ON tc.ProductTypeId = t.ProductTypeId
                WHERE (s.SkuCode IS NULL OR s.SkuCode LIKE 'TEMP-%')
            )
            UPDATE s
            SET s.SkuCode = CONCAT_WS('-', 
                                q.BrandCode, 
                                q.ProductTypeCode, 
                                q.ProductCode, 
                                RIGHT('0000' + CAST(q.SeqNo AS VARCHAR(4)), 4), 
                                q.SpecCode
                            )
            FROM PROD_ProductSku s
            JOIN sku_seq q ON s.SkuId = q.SkuId;
        ";

                var affected = await conn.ExecuteAsync(sqlUpdate, null, tran);

                if (disposeLocal)
                    conn.Dispose();

                return Array.Empty<(int, string)>(); // 沒有回傳新值，只回傳空集合
            }
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
