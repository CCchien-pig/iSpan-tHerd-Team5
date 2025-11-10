//using CsvHelper;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.Products;
using tHerdBackend.Core.Models;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Repository.Common;
using tHerdBackend.Infra.Repository.PROD.Assemblers;
using tHerdBackend.Infra.Repository.PROD.Builders;
using tHerdBackend.Infra.Repository.PROD.Services;
using static Dapper.SqlMapper;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace tHerdBackend.Infra.Repository.PROD
{
    /// <summary>
    /// 主查詢與維護商品資料入口
    /// </summary>
    public class ProdProductRepository : BaseRepository, IProdProductRepository
    {
        //private readonly ISqlConnectionFactory _factory;     // 給「純查詢」或「無交易時」使用
        //private readonly tHerdDBContext _db;                 // 寫入與交易來源
        private readonly ICurrentUser _currentUser;          // 目前登入使用者

        private readonly UserManager<ApplicationUser>? _userMgr;    // Identity UserManager (後台有 Identity 時使用)
        private readonly SignInManager<ApplicationUser>? _signInMgr;// Identity SignInManager (後台有 Identity 時使用)

        private readonly ProductRelationService _relationSvc; // 商品關聯服務

        public ProdProductRepository(ISqlConnectionFactory factory, tHerdDBContext db,
            ICurrentUser currentUser, UserManager<ApplicationUser>? userMgr = null, SignInManager<ApplicationUser>? signInMgr = null)
            : base(factory, db)
        {
            _currentUser = currentUser;
            _userMgr = userMgr;
            _signInMgr = signInMgr;
            _relationSvc = new ProductRelationService();
        }

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

		/// <summary>
		/// 前台: 查詢商品清單 (增加效率)
		/// </summary>
		/// <param name="query"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<(IEnumerable<ProdProductSearchDto> list, int totalCount)> GetAllFrontAsync(
			ProductFrontFilterQueryDto query, CancellationToken ct = default)
		{
            var sql = new StringBuilder();
            var includeHotRank = query.Other == "Hot";

            // === Step 1. 若為熱銷模式，建立 HotRank CTE ===
            if (includeHotRank)
            {
                var hotList = (await GetOtherFilter(query.PageSize)).ToList();
                if (hotList.Any())
                {
                    // 生成 HotRank 暫存表
                    var tempValues = string.Join(",", hotList.Select(x => $"({x.ProductId}, {x.Rank})"));
                    sql.AppendLine($@"
                        ;WITH HotRank(ProductId, RankNo) AS (
                            SELECT * FROM (VALUES {tempValues}) AS T(ProductId, RankNo)
                        ),");
                }
                else
                {
                    // 若查無熱銷，仍保留正常結構
                    sql.AppendLine(";WITH");
                }
            }
            else
            {
                sql.AppendLine(";WITH");
            }

            // === Step 2. TypeHierarchy 與 CategoryPaths CTE ===
            sql.AppendLine(@"TypeHierarchy (ProductId, ProductTypeId, ParentId, ProductTypeName, FullPath) AS (
                    SELECT 
                        pt.ProductId,
                        ptc.ProductTypeId,
                        ptc.ParentId,
                        ptc.ProductTypeName,
                        CAST(ptc.ProductTypeName AS NVARCHAR(MAX)) AS FullPath
                    FROM PROD_ProductType pt
                    JOIN PROD_ProductTypeConfig ptc ON pt.ProductTypeId = ptc.ProductTypeId

                    UNION ALL

                    SELECT 
                        th.ProductId,
                        p2.ProductTypeId,
                        p2.ParentId,
                        p2.ProductTypeName,
                        CAST(p2.ProductTypeName + N' > ' + th.FullPath AS NVARCHAR(MAX)) AS FullPath
                    FROM PROD_ProductTypeConfig p2
                    JOIN TypeHierarchy th ON th.ParentId = p2.ProductTypeId
                ),
                CategoryPaths AS (
                    SELECT 
                        th.ProductId,
                        STUFF((
                            SELECT DISTINCT N' / ' + th2.FullPath
                            FROM TypeHierarchy th2
                            WHERE th2.ProductId = th.ProductId
                            FOR XML PATH(''), TYPE
                        ).value('.', 'NVARCHAR(MAX)'), 1, 3, '') AS ProductTypePath
                    FROM TypeHierarchy th
                    GROUP BY th.ProductId
                )
                SELECT 
                    p.ProductId, p.ProductName,
                    p.BrandId, s.BrandName,
                    p.SeoId, p.Badge, sc.CodeDesc BadgeName,
                    p.MainSkuId, p.AvgRating, p.ReviewCount,
                    af.FileUrl AS ImageUrl,
                    ps.ListPrice, ps.UnitPrice, ps.SalePrice,
                    cp.ProductTypePath
                FROM PROD_Product p
                JOIN SUP_Brand s ON s.BrandId = p.BrandId
                LEFT JOIN SYS_Code sc ON sc.ModuleId = 'PROD' AND sc.CodeId = '02' AND sc.CodeNo=p.Badge
                LEFT JOIN PROD_ProductSku ps ON ps.SkuId = p.MainSkuId
                LEFT JOIN PROD_ProductImage i ON i.ProductId = p.ProductId AND i.IsMain = 1
                LEFT JOIN SYS_AssetFile af ON af.FileId = i.ImgId
                LEFT JOIN CategoryPaths cp ON cp.ProductId = p.ProductId
                " + (includeHotRank ? "JOIN HotRank hr ON hr.ProductId = p.ProductId" : "") + @"
                WHERE 1 = 1
                ");

            // === Step 2. 條件過濾 ===
            ProductFrontQueryBuilder.AppendFilters(sql, query);

			sql.AppendLine(@"
                GROUP BY 
                    p.ProductId, p.ProductName, p.CreatedDate,
                    p.BrandId, s.BrandName, 
                    p.SeoId, p.Badge, sc.CodeDesc,
                    p.MainSkuId, af.FileUrl, p.AvgRating, p.ReviewCount,
                    ps.ListPrice, ps.UnitPrice, ps.SalePrice, 
                    cp.ProductTypePath
                " + (includeHotRank ? ", hr.RankNo" : ""));

            // === Step 3. 排序與分頁 ===
            var orderByClause = ProductFrontQueryBuilder.BuildOrderClause(query);
            if (string.IsNullOrWhiteSpace(orderByClause) || !orderByClause.TrimStart().StartsWith("ORDER", StringComparison.OrdinalIgnoreCase))
            {
                orderByClause = includeHotRank ? " ORDER BY hr.RankNo ASC" : " ORDER BY p.ProductId DESC";
            }

            sql.AppendLine(orderByClause);
			sql.AppendLine(" OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;");

			// === Step 4. 統計筆數 ===
			var countSql = new StringBuilder(@"
                ;WITH TypeHierarchy (ProductId, ProductTypeId, ParentId, ProductTypeName, FullPath) AS (
                    SELECT 
                        pt.ProductId,
                        ptc.ProductTypeId,
                        ptc.ParentId,
                        ptc.ProductTypeName,
                        CAST(ptc.ProductTypeName AS NVARCHAR(MAX)) AS FullPath
                    FROM PROD_ProductType pt
                    JOIN PROD_ProductTypeConfig ptc ON pt.ProductTypeId = ptc.ProductTypeId

                    UNION ALL

                    SELECT 
                        th.ProductId,
                        p2.ProductTypeId,
                        p2.ParentId,
                        p2.ProductTypeName,
                        CAST(p2.ProductTypeName + N' > ' + th.FullPath AS NVARCHAR(MAX)) AS FullPath
                    FROM PROD_ProductTypeConfig p2
                    JOIN TypeHierarchy th ON th.ParentId = p2.ProductTypeId
                ),
                CategoryPaths AS (
                    SELECT 
                        th.ProductId,
                        STUFF((
                            SELECT DISTINCT N' / ' + th2.FullPath
                            FROM TypeHierarchy th2
                            WHERE th2.ProductId = th.ProductId
                            FOR XML PATH(''), TYPE
                        ).value('.', 'NVARCHAR(MAX)'), 1, 3, '') AS ProductTypePath
                    FROM TypeHierarchy th
                    GROUP BY th.ProductId
                )
                SELECT COUNT(DISTINCT p.ProductId)
                FROM PROD_Product p
                JOIN SUP_Brand s ON s.BrandId = p.BrandId
                LEFT JOIN PROD_ProductSku ps ON ps.SkuId = p.MainSkuId
                LEFT JOIN CategoryPaths cp ON cp.ProductId = p.ProductId
                WHERE 1 = 1
                ");

			ProductFrontQueryBuilder.AppendFilters(countSql, query);

			// === Step 5. 查詢執行 ===
			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
                // 改成 DynamicParameters，支援多屬性傳值
                var parameters = new DynamicParameters();
                parameters.Add("Skip", (query.PageIndex - 1) * query.PageSize);
                parameters.Add("Take", query.PageSize);
                parameters.Add("ProductId", query.ProductId);
                parameters.Add("Keyword", query.Keyword);
                parameters.Add("BrandId", query.BrandId);
                parameters.Add("ProductTypeId", query.ProductTypeId);
                parameters.Add("MinPrice", query.MinPrice);
                parameters.Add("MaxPrice", query.MaxPrice);
                parameters.Add("IsPublished", query.IsPublished);
                parameters.Add("Badge", query.Badge);
                parameters.Add("ProductIdList", query.ProductIdList);
                parameters.Add("BrandIds", query.BrandIds);
                parameters.Add("Rating", query.Rating); // ✅ 改成多選評價

                // 展開多層屬性篩選參數
                if (query.AttributeFilters != null && query.AttributeFilters.Any())
                {
                    int i = 0;
                    foreach (var attr in query.AttributeFilters)
                    {
                        for (int j = 0; j < attr.ValueNames.Count; j++)
                        {
                            parameters.Add($"Attr{i}_{j}", attr.ValueNames[j]);
                        }
                        i++;
                    }
                }


                using var multi = await conn.QueryMultipleAsync($"{sql} {countSql}", parameters, tx);
				var list = await multi.ReadAsync<ProdProductSearchDto>();
				var total = await multi.ReadSingleAsync<int>();

				return (list, total);
			}
			catch (Exception ex)
			{
				Console.WriteLine("❌ [GetAllFrontAsync] " + ex);
				return (Enumerable.Empty<ProdProductSearchDto>(), 0);
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

        /// <summary>
        /// 熱銷商品
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IEnumerable<(int ProductId, int Rank)>> GetOtherFilter(int size, CancellationToken ct = default)
        {
            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var sql = @"
                    SELECT TOP (@Size)
                        oi.ProductId,
                        RANK() OVER (ORDER BY COUNT(1) DESC) AS RankNo
                    FROM ORD_Order o
                    INNER JOIN ORD_OrderItem oi ON oi.OrderId = o.OrderId
                    INNER JOIN PROD_Product p ON p.ProductId = oi.ProductId AND p.IsPublished = 1
                    WHERE o.CreatedDate >= DATEADD(YEAR, -1, GETDATE())
                    GROUP BY oi.ProductId
                    ORDER BY COUNT(1) DESC;
                ";

                return await conn.QueryAsync<(int ProductId, int Rank)>(sql, new { Size = size }, transaction: tx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [GetOtherFilter] {ex}");
                return Enumerable.Empty<(int, int)>();
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }

        /// <summary>
        /// 查詢商品清單
        /// </summary>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<(IEnumerable<ProdProductDto> list, int totalCount)> GetAllAsync(
			ProductFilterQueryDto query, CancellationToken ct = default)
		{

			// === Step 1. 組 SQL：條件 + 排序 + 分頁 ===
			var sql = new StringBuilder(@"
                SELECT p.ProductId, p.ProductName, su.SupplierId, su.SupplierName,
                       p.BrandId, s.BrandName, p.SeoId, p.ProductCode,
                       p.ShortDesc, p.FullDesc, p.IsPublished, p.Badge, 
                       p.Weight, p.VolumeCubicMeter, p.VolumeUnit, p.Creator,
                       p.CreatedDate, p.Reviser, p.RevisedDate, 
                       p.mainSkuId, af.FileUrl ImageUrl, 
                       ps.ListPrice, ps.UnitPrice, ps.SalePrice, tc.ProductTypeName
                  FROM PROD_Product p
                  JOIN SUP_Brand s ON s.BrandId=p.BrandId
                  LEFT JOIN SUP_Supplier su ON su.SupplierId=s.SupplierId
                  LEFT JOIN PROD_ProductType t ON t.ProductId=p.ProductId
                  LEFT JOIN PROD_ProductTypeConfig tc ON tc.ProductTypeId=t.ProductTypeId
                  LEFT JOIN PROD_ProductSku ps ON ps.SkuId=p.MainSkuId
                  LEFT JOIN (
                      SELECT ProductId, 
                             MIN(COALESCE(SalePrice, UnitPrice)) AS MinSkuPrice,
                             MAX(COALESCE(SalePrice, UnitPrice)) AS MaxSkuPrice
                        FROM PROD_ProductSku
                       WHERE IsActive = 1
                       GROUP BY ProductId
                  ) sp ON sp.ProductId = p.ProductId
                  LEFT JOIN SYS_SeoMetaAsset sma ON sma.SeoId=p.SeoId AND sma.IsPrimary=1
                  LEFT JOIN SYS_AssetFile af ON af.FileId=sma.FileId
                 WHERE 1 = 1 ");

			// === Step 2. 條件過濾 ===
			ProductQueryBuilder.AppendFilters(sql, query);

			// === Step 3. 排序 ===
			sql.Append(ProductQueryBuilder.BuildOrderClause(query));
			sql.AppendLine(" OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY ");

			// === Step 4. 統計筆數 ===
			var countSql = new StringBuilder(@"
                SELECT COUNT(DISTINCT p.ProductId)
                  FROM PROD_Product p
                  JOIN SUP_Brand s ON s.BrandId=p.BrandId
                  LEFT JOIN SUP_Supplier su ON su.SupplierId=s.SupplierId
                  LEFT JOIN PROD_ProductType t ON t.ProductId=p.ProductId
                  LEFT JOIN PROD_ProductSku ps ON ps.SkuId=p.MainSkuId
                  LEFT JOIN (
                      SELECT ProductId, 
                             MIN(COALESCE(SalePrice, UnitPrice)) AS MinSkuPrice,
                             MAX(COALESCE(SalePrice, UnitPrice)) AS MaxSkuPrice
                        FROM PROD_ProductSku
                       WHERE IsActive = 1
                       GROUP BY ProductId
                  ) sp ON sp.ProductId = p.ProductId
                 WHERE 1=1 ");

			ProductQueryBuilder.AppendFilters(countSql, query);

			// === Step 5. 查詢執行 ===
			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				// 查詢參數
				var parameters = new
				{
					Skip = (query.PageIndex - 1) * query.PageSize,
					Take = query.PageSize,
					query.ProductId,
					query.Keyword,
					query.BrandId,
					query.ProductTypeId,
					query.MinPrice,
					query.MaxPrice,
					query.IsPublished
				};

				// 查詢
				var multiSql = $"{sql}; {countSql};";
				using var multi = await conn.QueryMultipleAsync(multiSql, parameters, tx);

				// 讀清單
				var raw = await multi.ReadAsync<ProdProductDto>();

				// 讀總數
				var total = await multi.ReadSingleAsync<int>();


				// 整理類別名稱集合
				var list = raw.GroupBy(p => p.ProductId).Select(g =>
				{
					var first = g.First();
					first.ProductTypeDesc = g
						.Select(x => x.ProductTypeName ?? string.Empty)
						.Where(n => !string.IsNullOrEmpty(n))
						.Distinct()
						.ToList();
					return first;
				}).ToList();

				// === Step 7. 補 Creator / Reviser 名稱 ===
				if (query.IsFrontEnd != true)  // 自訂 flag，前台不載入
				{
					var resolver = new UserNameResolver(_factory, _db);
					await resolver.LoadAsync(ct);
					resolver.Apply(list);
				}

				return (list, total);
			}
			catch (Exception ex)
			{
				Console.WriteLine("❌ [GetAllAsync] " + ex);
				return (Enumerable.Empty<ProdProductDto>(), 0);
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		/// <summary>
		/// 查詢單筆商品完整資料
		/// </summary>
		/// <param name="ProductId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<ProdProductDetailDto?> GetByIdAsync(int productId, int? skuId, CancellationToken ct = default)
        {
            var sql = @"SELECT p.ProductId, p.BrandId, b.BrandName, b.BrandCode, p.SeoId,
                           s.SupplierId, s.SupplierName, p.ProductCode, p.ProductName,
                           p.ShortDesc, p.FullDesc, p.IsPublished, p.Weight, 
                           p.badge, sc.CodeDesc BadgeName, p.MainSkuId, p.AvgRating, p.ReviewCount,
                           p.VolumeCubicMeter, p.VolumeUnit, p.Creator, 
                           p.CreatedDate, p.Reviser, p.RevisedDate
                      FROM PROD_Product p
                      JOIN SUP_Brand b ON b.BrandId=p.BrandId
                      JOIN SUP_Supplier s ON s.SupplierId=b.SupplierId
                      LEFT JOIN SYS_Code sc ON sc.ModuleId = 'PROD' AND sc.CodeId = '02' AND sc.CodeNo=p.Badge
                     WHERE p.ProductId=@ProductId;";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var item = await conn.QueryFirstOrDefaultAsync<ProdProductDetailDto>(new CommandDefinition(sql, new { ProductId = productId }, tx, cancellationToken: ct));
                if (item == null) return null;

                var assembler = new ProductAssembler(_db, _factory);
                await assembler.AssembleDetailsAsync(item, skuId, conn, tx, ct);

                var resolver = new UserNameResolver(_factory, _db);
                await resolver.LoadAsync(ct);
                resolver.Apply(item);

                return item;
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }

        // 新增
        public async Task<int> AddAsync(ProdProductDetailDto dto, CancellationToken ct = default)
        {
            int? userNumberId = null;

            if (_userMgr != null) // 後台有 Identity
            {
                var u = await _userMgr.Users.AsNoTracking()
                             .FirstOrDefaultAsync(x => x.Id == _currentUser.Id, ct);
                userNumberId = u?.UserNumberId;
            }
            else
            {
                // 前台沒有 Identity，就用 JWT 的 userId / 或 default 值
                userNumberId = int.TryParse(_currentUser.Id, out var parsed) ? parsed : 0;
            }

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
                        Creator = userNumberId,
                        Reviser = userNumberId,
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
        public async Task<bool> UpdateAsync(ProdProductDetailDto dto, CancellationToken ct = default)
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

                await UpsertImagesAsync(conn, tran, dto);

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 圖片處理
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="tran"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        private async Task UpsertImagesAsync(IDbConnection conn, IDbTransaction tran, ProdProductDetailDto dto)
        {
            var images = dto.Images.Select(i => i.ImgId).ToList();

            // === Step 1: 刪除舊的圖片關聯 ===
            await conn.ExecuteAsync(
                "DELETE FROM PROD_ProductImage WHERE ProductId = @ProductId AND ImgId NOT IN @ImgIds;",
                new { dto.ProductId, ImgIds = images }, tran);

            foreach (var img in dto.Images ?? new())
            {
                // 判斷是否存過
                var isSaved = await conn.ExecuteScalarAsync<bool>(
                    "SELECT COUNT(1) FROM PROD_ProductImage WHERE ProductId = @ProductId AND ImgId = @ImgId;",
                new { dto.ProductId, img.ImgId }, tran);
                // === 需要新建 圖片 ===
                if (isSaved == false)
                {

                    await conn.ExecuteScalarAsync<int>(
                        @" INSERT INTO PROD_ProductImage
                           (ProductId, SkuId, ImgId, IsMain, OrderSeq)
                          VALUES
                          (@ProductId, @SkuId, @ImgId, @IsMain, @OrderSeq); ",
                        new
                        {
                            dto.ProductId,
                            img.SkuId,
                            img.ImgId,
                            img.IsMain,
                            img.OrderSeq
                        }, tran);
                }
                else
                {
                    // === 更新圖片關聯資料 ===
                    await conn.ExecuteAsync(
                        @" UPDATE PROD_ProductImage
                           SET SkuId   = @SkuId,
                               IsMain  = @IsMain,
                               OrderSeq= @OrderSeq
                         WHERE ImageId = @ImageId; ",
                        new
                        {
                            img.SkuId,
                            img.IsMain,
                            img.OrderSeq,
                            img.ImageId
                        }, tran);
                }
            }
        }

        private async Task UpsertRelationsAsync(IDbConnection conn, IDbTransaction tran, ProdProductDetailDto dto)
        {
			// === SKU處理邏輯 ===
			foreach (var sku in dto.Skus ?? new())
			{
				sku.IsActive = true;

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

        /// <summary>
        /// 取得商品分類樹狀結構（含子分類）
        /// 用於前台 MegaMenu 或分類篩選
        /// </summary>
        /// <summary>
        /// 取得商品分類樹狀結構（含子分類）
        /// 用於前台 MegaMenu 或分類篩選
        /// </summary>

        public async Task<List<ProductTypeTreeDto>> GetProductTypeTreeAsync(int? id, CancellationToken ct = default)
        {
            const string sql = @"
                ;WITH RecursiveTypes AS (
                    -- 起點：指定的 ProductTypeId
                    SELECT ProductTypeId, ParentId, ProductTypeCode, ProductTypeName, OrderSeq, IsActive
                    FROM PROD_ProductTypeConfig
                    WHERE ProductTypeId = @Id AND IsActive = 1

                    UNION ALL

                    -- 遞迴抓出所有子節點
                    SELECT c.ProductTypeId, c.ParentId, c.ProductTypeCode, c.ProductTypeName, c.OrderSeq, c.IsActive
                    FROM PROD_ProductTypeConfig c
                    INNER JOIN RecursiveTypes p ON c.ParentId = p.ProductTypeId
                    WHERE c.IsActive = 1
                )
                SELECT * FROM RecursiveTypes
                ORDER BY ParentId, OrderSeq, ProductTypeName;
            ";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var list = (await conn.QueryAsync<ProductTypeTreeDto>(
                    new CommandDefinition(sql, new { Id = id }, tx, cancellationToken: ct))).ToList();

                if (!list.Any())
                    return new List<ProductTypeTreeDto>();

                // === Step 1. 建立查找字典 ===
                var lookup = list.ToDictionary(x => x.ProductTypeId, x => x);

                // === Step 2. 初始化 Children ===
                foreach (var item in list)
                {
                    item.Children = new List<ProductTypeTreeNodeDto>();
                }

                // === Step 3. 將子節點加入父節點 ===
                foreach (var item in list)
                {
                    if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
                    {
                        parent.Children.Add(new ProductTypeTreeNodeDto
                        {
                            ProductTypeId = item.ProductTypeId,
                            ParentId = item.ParentId,
                            ProductTypeCode = item.ProductTypeCode,
                            ProductTypeName = item.ProductTypeName,
                            OrderSeq = item.OrderSeq,
                            IsActive = item.IsActive,
                            Children = new List<ProductTypeTreeNodeDto>()
                        });
                    }
                }

                // === Step 4. 找出根節點（即傳入的 id 對應節點） ===
                var roots = list.Where(x => x.ProductTypeId == id).ToList();

                return roots;
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }

		/// <summary>
		/// 按讚或取消讚（由 API 傳入使用者ID）
		/// </summary>
		public async Task<(bool IsLiked, string Message)> ToggleLikeAsync(int userNumberId, int productId, CancellationToken ct = default)
		{
			if (productId <= 0 || userNumberId <= 0)
				throw new ArgumentException("商品 ID 或 使用者 ID 錯誤");

			var (conn, _, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				// 檢查是否已經按讚過
				var exists = await conn.ExecuteScalarAsync<bool>(
					"SELECT COUNT(1) FROM PROD_ProductLike WHERE ProductId = @ProductId AND UserNumberId = @UserId",
					new { ProductId = productId, UserId = userNumberId });

				if (exists)
				{
					await conn.ExecuteAsync(
						"DELETE FROM PROD_ProductLike WHERE ProductId = @ProductId AND UserNumberId = @UserId",
						new { ProductId = productId, UserId = userNumberId });
					return (false, "已取消按讚");
				}
				else
				{
					await conn.ExecuteAsync(
						"INSERT INTO PROD_ProductLike (ProductId, UserNumberId) VALUES (@ProductId, @UserId)",
						new { ProductId = productId, UserId = userNumberId });
					return (true, "已按讚");
				}
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		/// <summary>
		/// 檢查使用者是否對指定商品按讚過
		/// </summary>
		public async Task<bool> HasUserLikedProductAsync(int userNumberId, int productId, CancellationToken ct = default)
		{
			if (userNumberId <= 0 || productId <= 0)
				return false;

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				var sql = @"SELECT COUNT(1) 
                    FROM PROD_ProductLike 
                    WHERE ProductId = @ProductId AND UserNumberId = @UserId";
				var count = await conn.ExecuteScalarAsync<int>(sql, new { ProductId = productId, UserId = userNumberId }, tx);
				return count > 0;
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}
	}
}
