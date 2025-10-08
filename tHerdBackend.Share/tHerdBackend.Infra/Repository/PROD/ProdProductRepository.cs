//using CsvHelper;
using Dapper;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.Products;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static Dapper.SqlMapper;
using tHerdBackend.Core.DTOs.Common;

namespace tHerdBackend.Infra.Repository.PROD
{
    public class ProdProductRepository : IProdProductRepository
    {
        private readonly ISqlConnectionFactory _factory;     // 給「純查詢」或「無交易時」使用
        private readonly tHerdDBContext _db;                 // 寫入與交易來源
        private readonly ICurrentUser _currentUser;

		private readonly UserManager<ApplicationUser> _userMgr;

		public ProdProductRepository(ISqlConnectionFactory factory,
			UserManager<ApplicationUser> userMgr,
			SignInManager<ApplicationUser> signInMgr,
			tHerdDBContext db, ICurrentUser currentUser)
        {
            _factory = factory;
            _db = db;
            _currentUser = currentUser;
			_userMgr = userMgr;
		}

		/// <summary>
		/// 前台: 依傳入條件，取得產品清單
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		//public async Task<List<ProdProductDto>> GetFrontProductListAsync(CancellationToken ct = default)
		//{
		//	// 等待非同步結果
		//	var list = await GetAllAsync(ct);

		//	// 判斷是否有資料
		//	if (list == null || !list.Any())
		//		return new List<ProdProductDto>();

		//	// 篩選回前台需要的欄位
		//	return list
		//		.Select(x => new ProdProductDto
		//		{
		//			ProductId = x.ProductId,
		//			ProductName = x.ProductName,
		//			ImageUrl = x.ImageUrl,
		//			Badge = x.Badge, // 標籤
		//			ListPrice = x.ListPrice, // 建議售價
		//			UnitPrice = x.UnitPrice, // 單價
		//			SalePrice = x.SalePrice // 特價
		//		})
		//		.ToList();
		//}

		/// <summary>
		/// 取得所有有效分類
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<List<ProdProductTypeConfigDto>> GetAllProductTypesAsync(CancellationToken ct = default)
        {
            string sql = @"SELECT ProductTypeId, ParentId, ProductTypeCode, 
                            ProductTypeName
                            FROM PROD_ProductTypeConfig
                            WHERE IsActive=1
                            ORDER BY OrderSeq";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var cmd = new CommandDefinition(sql, transaction: tx, cancellationToken: ct);
                return conn.Query<ProdProductTypeConfigDto>(cmd).ToList();
			}
            finally
            {
                if (needDispose) conn.Dispose();
            }
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
            var sql = @"SELECT p.ProductId, p.BrandId, b.BrandName, b.BrandCode, p.SeoId, 
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

                var repo = new AspnetusersNameRepository(_factory, _db);
                var emp = await repo.GetAllUserNameAsync(ct);
                item.CreatorNm = emp.FirstOrDefault(e => e.UserNumberId == item.Creator)?.FullName;
                item.ReviserNm = emp.FirstOrDefault(e => e.UserNumberId == item.Reviser)?.FullName;
                
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
				var specOptionIds = item.Skus
					.SelectMany(s => s.SpecValues.Select(v => v.SpecificationOptionId))
					.Distinct()
					.ToList();


				if (specOptionIds.Any())
                {
                    var specOptions= await _db.ProdSpecificationOptions
                        .Where(c => specOptionIds.Contains(c.SpecificationOptionId))
                        .ToListAsync();

                    var specConfigIds = await _db.ProdSpecificationConfigs
                        .Where(c => specOptions.Select(o => o.SpecificationConfigId).Distinct().Contains(c.SpecificationConfigId))
                        .ToListAsync();

                    item.SpecConfigs = specConfigIds.Select(c => new ProdSpecificationConfigDto
                    {
                        SpecificationConfigId = c.SpecificationConfigId,
                        GroupName = c.GroupName,
                        OrderSeq = c.OrderSeq,
                        SpecOptions = specOptions
                            .Where(o => o.SpecificationConfigId == c.SpecificationConfigId)
                            .Select(o => new ProdSpecificationOptionDto
                            {
                                SpecificationOptionId = o.SpecificationOptionId,
                                SpecificationConfigId = o.SpecificationConfigId,
                                OptionName = o.OptionName,
                                OrderSeq = o.OrderSeq,
                                // 找對應的 SkuId
                                SkuId = item.Skus
                                    .FirstOrDefault(s => s.SpecValues.Any(v => v.SpecificationOptionId == o.SpecificationOptionId))
                                    ?.SkuId ?? 0
                            }).ToList()
					}).ToList();
                }
                else
                {
                    item.SpecConfigs = new List<ProdSpecificationConfigDto>();
                }

                item.Types = await _db.ProdProductTypes
                    .Where(t => t.ProductId == item.ProductId)
                    .Select(t => new ProdProductTypeDto
                    {
                        ProductTypeId = t.ProductTypeId,
                        ProductId = t.ProductId,
                        IsPrimary = t.IsPrimary
                    }).OrderByDescending(a=>a.IsPrimary).ToListAsync();

				var img_sql = @"
                        SELECT pi.ImageId, pi.ProductId, pi.SkuId, pi.IsMain, pi.OrderSeq,
                               af.FileUrl, af.AltText
                        FROM   PROD_ProductImage pi
                        JOIN   SYS_AssetFile af ON pi.ImgId = af.FileId
                        WHERE  pi.ProductId = @ProductId
                        ORDER BY pi.IsMain DESC, pi.OrderSeq ASC;";

				var img_cmd = new CommandDefinition(img_sql, new { ProductId }, tx, cancellationToken: ct);
				item.Images = conn.Query<ProductImageDto>(img_cmd).ToList();

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
			var u = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == _currentUser.Id);

			var (conn, _, _) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            using var tran = conn.BeginTransaction();

            try
            {
                var now = DateTime.Now;

                // === 1. 新增 Product 主檔 ===
                var productId = await conn.ExecuteScalarAsync<int>(@"
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
                     @Reviser, SYSDATETIME());",
                    new
                    {
                        dto.BrandId,
                        dto.SeoId,
                        ProductCode = string.Empty,
                        dto.ProductName,
                        dto.ShortDesc,
                        dto.FullDesc,
                        dto.IsPublished,
                        dto.Weight,
                        dto.VolumeCubicMeter,
                        dto.VolumeUnit,
                        Creator = u.UserNumberId,
                        Reviser = u.UserNumberId,
                        CreatedDate = now,
                        RevisedDate = now
                    }, tran);

                // 更新正式 ProductCode
                var newProductCode = "P" + productId.ToString("D4");
                await conn.ExecuteAsync(
                    "UPDATE PROD_Product SET ProductCode=@ProductCode WHERE ProductId=@ProductId",
                    new { ProductCode = newProductCode, ProductId = productId }, tran);

                dto.ProductId = productId;
                dto.ProductCode = newProductCode;

                // === 共用處理 ===
                await UpsertRelationsAsync(conn, tran, dto);

                tran.Commit();
                return productId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

		/// <summary>
		/// 檢查 條碼
		/// </summary>
		/// <param name="barcodes"></param>
		/// <param name="excludeSkuIds"></param>
		/// <returns></returns>
		public async Task<List<string>> GetDuplicateBarcodesAsync(IEnumerable<string> barcodes, IEnumerable<int> excludeSkuIds)
		{
			return await _db.ProdProductSkus
				.Where(s => barcodes.Contains(s.Barcode) && !excludeSkuIds.Contains(s.SkuId))
				.Select(s => s.Barcode)
				.ToListAsync();
		}

        // 修改
        public async Task<bool> UpdateAsync(ProdProductDto dto, CancellationToken ct = default)
        {
			var u = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == _currentUser.Id);

			var (conn, _, _) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            using var tran = conn.BeginTransaction();

            try
            {
                // === 1. 更新 Product 主檔 ===
                await conn.ExecuteAsync(@"
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
                        WHERE ProductId = @ProductId;",
                    new
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
                        Reviser = u.UserNumberId,
                        RevisedDate = DateTime.Now,
                        dto.ProductId
                    }, tran);

                // === 共用處理 ===
                await UpsertRelationsAsync(conn, tran, dto);

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        private async Task UpsertRelationsAsync(IDbConnection conn, IDbTransaction tran, ProdProductDto dto)
        {
			// === SKU處理邏輯 ===
			foreach (var sku in dto.Skus ?? new())
			{
				// 需要新建 SkuCode
				if (sku.SkuId == 0)
				{
					var maxSeq = (await conn.ExecuteScalarAsync<int?>(
						"SELECT ISNULL(MAX(SkuId),0) FROM PROD_ProductSku;", transaction: tran)) ?? 0;

					sku.SkuCode = GenerateSkuCode(dto.BrandCode, dto.ProductTypeCode, dto.ProductCode, maxSeq + 1, sku.SpecCode);

					sku.SkuId = await conn.ExecuteScalarAsync<int>(
						@"INSERT INTO PROD_ProductSku
                  (ProductId, SpecCode, SkuCode, Barcode,
                   CostPrice, ListPrice, UnitPrice, SalePrice,
                   StockQty, SafetyStockQty, ReorderPoint, MaxStockQty,
                   IsAllowBackorder, ShelfLifeDays, StartDate, EndDate, IsActive)
                  OUTPUT INSERTED.SkuId
                  VALUES
                  (@ProductId, @SpecCode, @SkuCode, @Barcode,
                   @CostPrice, @ListPrice, @UnitPrice, @SalePrice,
                   @StockQty, @SafetyStockQty, @ReorderPoint, @MaxStockQty,
                   @IsAllowBackorder, @ShelfLifeDays, @StartDate, @EndDate, @IsActive);",
						new
						{
							dto.ProductId,
							sku.SpecCode,
							sku.SkuCode,
							sku.Barcode,
							sku.CostPrice,
							sku.ListPrice,
							sku.UnitPrice,
							sku.SalePrice,
							sku.StockQty,
							sku.SafetyStockQty,
							sku.ReorderPoint,
							sku.MaxStockQty,
							sku.IsAllowBackorder,
							sku.ShelfLifeDays,
							sku.StartDate,
							sku.EndDate,
							sku.IsActive
						}, tran);
				}
				var ptc = _db.ProdProductTypes
	                .Where(t => t.ProductId == dto.ProductId && t.IsPrimary)
	                .FirstOrDefault()?.ProductTypeId;

				string? typeCode = null;

				if (ptc != null)
				{
					typeCode = _db.ProdProductTypeConfigs
						.Where(t => t.ProductTypeId == ptc)
						.Select(t => t.ProductTypeCode)
						.FirstOrDefault();
				}

				dto.ProductTypeCode = typeCode ?? "PT";

				// === 不論新增 / 修改，都重新生成正式的 SkuCode ===
				sku.SkuCode = GenerateSkuCode(
					dto.BrandCode,
					dto.ProductTypeCode,
					dto.ProductCode,
					sku.SkuId,
					sku.SpecCode);

				await conn.ExecuteAsync(
					        @"UPDATE PROD_ProductSku
                  SET SpecCode=@SpecCode, SkuCode=@SkuCode, Barcode=@Barcode,
                      CostPrice=@CostPrice, ListPrice=@ListPrice, UnitPrice=@UnitPrice, SalePrice=@SalePrice,
                      StockQty=@StockQty, SafetyStockQty=@SafetyStockQty, ReorderPoint=@ReorderPoint, MaxStockQty=@MaxStockQty,
                      IsAllowBackorder=@IsAllowBackorder, ShelfLifeDays=@ShelfLifeDays, StartDate=@StartDate, EndDate=@EndDate, IsActive=@IsActive
                  WHERE SkuId=@SkuId",
					        sku, tran);
			}

			// === 規格群組 & 規格選項 ===
			// === Step 2: 刪除舊的 Config / Option / Relation ===
			var cfgIds = await conn.QueryAsync<int>(
				"SELECT SpecificationConfigId FROM PROD_SpecificationConfig WHERE ProductId=@ProductId",
				new { dto.ProductId }, tran);

			if (cfgIds.Any())
			{
				await conn.ExecuteAsync(
					"DELETE FROM PROD_SkuSpecificationValue WHERE SpecificationOptionId IN (SELECT SpecificationOptionId FROM PROD_SpecificationOption WHERE SpecificationConfigId IN @Ids)",
					new { Ids = cfgIds }, tran);

				await conn.ExecuteAsync(
					"DELETE FROM PROD_SpecificationOption WHERE SpecificationConfigId IN @Ids",
					new { Ids = cfgIds }, tran);

				await conn.ExecuteAsync(
					"DELETE FROM PROD_SpecificationConfig WHERE SpecificationConfigId IN @Ids",
					new { Ids = cfgIds }, tran);
			}

			// === Step 3: 重建 SpecConfigs & SpecOptions & Relations ===
			foreach (var cfg in dto.SpecConfigs ?? new())
			{
				var cfgId = await conn.ExecuteScalarAsync<int>(
					@"INSERT INTO PROD_SpecificationConfig (ProductId, GroupName, OrderSeq)
                      OUTPUT INSERTED.SpecificationConfigId
                      VALUES (@ProductId, @GroupName, @OrderSeq);",
					new { dto.ProductId, cfg.GroupName, cfg.OrderSeq }, tran);

				for (int i = 0; i < (cfg.SpecOptions?.Count ?? 0); i++)
				{
					var opt = cfg.SpecOptions[i];

                    if (opt == null) continue;

					var optionId = await conn.ExecuteScalarAsync<int>(
						@"INSERT INTO PROD_SpecificationOption (SpecificationConfigId, OptionName, OrderSeq)
                          OUTPUT INSERTED.SpecificationOptionId
                          VALUES (@SpecificationConfigId, @OptionName, @OrderSeq);",
						new { SpecificationConfigId = cfgId, opt.OptionName, opt.OrderSeq }, tran);

					// === 用 index 來對應 dto.Skus ===
					if (opt.SkuId == 0 && i < dto.Skus.Count)
					{
						opt.SkuId = dto.Skus[i].SkuId;
					}

					// 關聯到 SKU
					if (opt.SkuId > 0)
					{
						await conn.ExecuteAsync(
							@"INSERT INTO PROD_SkuSpecificationValue (SkuId, SpecificationOptionId)
                              VALUES (@SkuId, @SpecificationOptionId);",
							new { opt.SkuId, SpecificationOptionId = optionId }, tran);
					}
				}
            }

            // === Step 4: 商品分類處理 (ProdProductType) ===
            await conn.ExecuteAsync(
				"DELETE FROM PROD_ProductType WHERE ProductId = @ProductId",
                new { dto.ProductId }, tran);

            foreach (var opt in dto.Types ?? new())
            {
                var optionId = await conn.ExecuteScalarAsync<int>(
                    @"INSERT INTO PROD_ProductType (ProductTypeId, ProductId, IsPrimary)
                          VALUES (@ProductTypeId, @ProductId, @IsPrimary);",
                    new { opt.ProductTypeId, dto.ProductId, opt.IsPrimary }, tran);
            }
        }

        /// <summary>
        /// 取得 SKU Code
        /// </summary>
        /// <param name="brandCode"></param>
        /// <param name="productTypeCode"></param>
        /// <param name="productCode"></param>
        /// <param name="seqNo"></param>
        /// <param name="specCode"></param>
        /// <returns></returns>
        private string GenerateSkuCode(string brandCode, string productTypeCode, string productCode, int seqNo, string specCode)
        {
            return $"{brandCode ?? "BR"}-{productTypeCode ?? "PT"}-{productCode ?? "P"}-{seqNo:D4}-{specCode ?? "NA"}";
        }

        public async Task<IEnumerable<(int SkuId, string NewSkuCode)>> RefreshSkuCodesAsync(IDbConnection? conn = null, IDbTransaction? tran = null)
        {
            var disposeLocal = false;
            if (conn == null)
            {
                (conn, _, disposeLocal) = await DbConnectionHelper.GetConnectionAsync(_db, _factory);
            }

            // 直接更新所有 TEMP- 的 SKU
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

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = _db.ProdProducts.Find(new object?[] { id }, ct);
            if (entity == null) return false;

            _db.ProdProducts.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return true;
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
					PageIndex = query.PageIndex,
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

		public async Task<bool> GetByProductNameAsync(string name, int id, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(name))
				return false;

			return _db.ProdProducts
	                .AsNoTracking()
	                .Where(p => p.ProductName == name && p.ProductId != id).Count()>0;
		}

		//public async Task<string> CheckUniqulByBarcodeAsync(List<string> barcodes, CancellationToken ct = default)
		//{
		//	if (barcodes == null || !barcodes.Any())
		//		return string.Empty;

		//	var exists = await _db.ProdProductSkus
		//		.AsNoTracking()
		//		.Where(p => barcodes.Contains(p.Barcode))
		//		.Select(p => p.Barcode)   // 只取出條碼字串
		//		.ToListAsync(ct);

		//	return string.Join("、", exists);
		//}
	}
}
