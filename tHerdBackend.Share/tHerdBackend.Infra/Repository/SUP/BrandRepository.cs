using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Models.Sup;

public class BrandRepository : IBrandRepository
{
	private readonly tHerdDBContext _db;
	private readonly ISqlConnectionFactory _factory;
	private readonly ILogger<IBrandRepository> _logger;
	public BrandRepository(tHerdDBContext db, ISqlConnectionFactory factory, ILogger<IBrandRepository> logger)
	{
		_db = db;
		_factory = factory;
		_logger = logger;
	}


	#region 品牌

	public async Task<List<BrandDto>> GetAllAsync()
	{
		return await _db.SupBrands.AsNoTracking()
			.Select(b => new BrandDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				BrandCode = b.BrandCode,
				SupplierId = b.SupplierId,
				//SeoId = b.SeoId.HasValue ? b.SeoId.Value.ToString() : null,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate,
				IsFeatured = b.IsFeatured,
				LikeCount = b.LikeCount,
				IsActive = b.IsActive,
				Creator = b.Creator,
				CreatedDate = b.CreatedDate,
				Reviser = b.Reviser,
				RevisedDate = b.RevisedDate
			})
			.ToListAsync();
	}

	public async Task<BrandDto?> GetByIdAsync(int id)
	{
		return await _db.SupBrands.AsNoTracking()
			.Where(b => b.BrandId == id)
			.Select(b => new BrandDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				BrandCode = b.BrandCode,
				SupplierId = b.SupplierId,
				//SeoId = b.SeoId.HasValue ? b.SeoId.Value.ToString() : null,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate,
				IsFeatured = b.IsFeatured,
				LikeCount = b.LikeCount,
				IsActive = b.IsActive,
				Creator = b.Creator,
				CreatedDate = b.CreatedDate,
				Reviser = b.Reviser,
				RevisedDate = b.RevisedDate
			})
			.FirstOrDefaultAsync();
	}

	public async Task<List<BrandDto>> GetFilteredAsync(
		bool? isActive = null,
		bool? isDiscountActive = null,
		bool? isFeatured = null)
	{
		var query = _db.SupBrands.AsNoTracking().AsQueryable();

		// 篩選品牌啟用狀態
		if (isActive.HasValue)
			query = query.Where(b => b.IsActive == isActive.Value);

		// 篩選折扣活動，只依 IsDiscountActive
		if (isDiscountActive.HasValue)
			query = query.Where(b => b.IsDiscountActive == isDiscountActive.Value);

		// 篩選精選品牌
		if (isFeatured.HasValue)
			query = query.Where(b => b.IsFeatured == isFeatured.Value);

		// 投影成 DTO
		return await query
			.Select(b => new BrandDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				BrandCode = b.BrandCode,
				SupplierId = b.SupplierId,
				//SeoId = b.SeoId.HasValue ? b.SeoId.Value.ToString() : null,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate,
				IsFeatured = b.IsFeatured,
				LikeCount = b.LikeCount,
				IsActive = b.IsActive,
				Creator = b.Creator,
				CreatedDate = b.CreatedDate,
				Reviser = b.Reviser,
				RevisedDate = b.RevisedDate
			})
			.ToListAsync();
	}
	public async Task<bool> CheckBrandExistsAsync(int brandId)
	{
		return await _db.Set<SupBrand>()
			.AsNoTracking()
			.AnyAsync(b => b.BrandId == brandId);
	}

	#endregion

	public async Task<int?> GetLikeCountAsync(int id)
	{
		return await _db.SupBrands
			.Where(b => b.BrandId == id)
			.Select(b => (int?)b.LikeCount)
			.FirstOrDefaultAsync();
	}


	#region 品牌折扣

	public async Task<List<BrandDiscountDto>> GetAllDiscountsAsync()
	{
		return await _db.Set<SupBrand>().AsNoTracking()
			.Select(b => new BrandDiscountDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate
			})
			.ToListAsync();
	}

	public async Task<BrandDiscountDto?> GetDiscountByBrandIdAsync(int brandId)
	{
		return await _db.SupBrands.AsNoTracking()
			.Where(b => b.BrandId == brandId)
			.Select(b => new BrandDiscountDto
			{
				BrandId = b.BrandId,
				BrandName = b.BrandName,
				DiscountRate = b.DiscountRate,
				IsDiscountActive = b.IsDiscountActive,
				StartDate = b.StartDate,
				EndDate = b.EndDate
			})
			.FirstOrDefaultAsync();
	}

	#endregion

	#region 前台查詳情

	public async Task<(int brandId, string brandName, int? imgId)> GetBrandAsync(int brandId, CancellationToken ct)
	{
		// EF 讀品牌）
		var entity = await _db.Set<SupBrand>()
								.AsNoTracking()
								.Where(x => x.BrandId == brandId && x.IsActive == true)
								.Select(x => new { x.BrandId, x.BrandName, ImgId = (int?)x.ImgId })
								.FirstOrDefaultAsync(ct);

		if (entity == null) return (0, string.Empty, null);
		return (entity.BrandId, entity.BrandName, entity.ImgId);
	}

	public async Task<string?> GetAssetFileUrlByFileIdAsync(int fileId, CancellationToken ct)
	{
		// EF 讀檔案 URL
		var url = await _db.Set<SysAssetFile>()
							.AsNoTracking()
							.Where(x => x.FileId == fileId)
							.Select(x => x.FileUrl)
							.FirstOrDefaultAsync(ct);
		return url;
	}

	public async Task<List<BrandButtonDto>> GetBrandButtonsAsync(int brandId, CancellationToken ct)
	{
		// 用 Dapper 取分類按鈕
		var (conn, tx, shouldDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
		try
		{
			var sql = @"
				SELECT
					Id     = bptf.[BrandProductTypeFilterId],   -- int
					Text   = bptf.[ButtonText],                 -- nvarchar
					[Order]= bptf.[ButtonOrder],                -- int
					Slug   = CONCAT(LOWER(ptc.ProductTypeCode), '-', bptf.ProductTypeId)
				FROM SUP_BrandProductTypeFilter bptf
				LEFT JOIN PROD_ProductTypeConfig ptc ON ptc.ProductTypeId = bptf.ProductTypeId
				WHERE bptf.BrandId = @brandId AND bptf.IsActive = 1
				ORDER BY bptf.[ButtonOrder] ASC;";

			var rows = await conn.QueryAsync<BrandButtonDto>(
					new CommandDefinition(sql, new { brandId }, tx, cancellationToken: ct));
			return rows.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Query BrandButtons failed. brandId={BrandId}", brandId);
			throw;
		}
		finally
		{
			if (shouldDispose) conn.Dispose();
		}
	}

	public async Task<List<(string contentKey, BrandAccordionItemDto item)>> GetBrandAccordionRawAsync(int brandId, CancellationToken ct)
	{
		var (conn, tx, shouldDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
		try
		{
			var sql = @"
SELECT
    ContentKey = bac.[ContentId],     -- int
    Title      = bac.[ContentTitle],  -- nvarchar
    Body       = bac.[Content],       -- nvarchar(max)
    [Order]    = bac.[OrderSeq]       -- int
FROM SUP_BrandAccordionContent bac
WHERE bac.BrandId = @brandId AND bac.IsActive = 1
ORDER BY bac.[ContentId], bac.[OrderSeq];";

			// 直接映射成強型別，避免 dynamic
			var rows = await conn.QueryAsync<AccordionRawRow>(
				new CommandDefinition(sql, new { brandId }, tx, cancellationToken: ct));

			// 統一轉成 (string contentKey, BrandAccordionItemDto) 給 Service 分組
			var list = rows
				.Select(r => (
					contentKey: r.ContentKey.ToString(), // 轉字串，後續分組鍵用 string 最穩
					item: new BrandAccordionItemDto
					{
						Title = r.Title ?? string.Empty,
						Body = r.Body ?? string.Empty,
						Order = r.Order
					}))
				.ToList();

			return list;
		}
		finally
		{
			if (shouldDispose) conn.Dispose();
		}
	}

	// 內部專用：接 Dapper 的原始列，避免 dynamic 轉型錯誤
	private sealed class AccordionRawRow
	{
		public int ContentKey { get; set; }   // ← 用 ContentId（int）
		public string Title { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;
		public int Order { get; set; }
	}

		
	// 取得品牌綜合資訊
	public async Task<BrandOverviewDto> GetBrandOverviewAsync(int brandId)
	{
		var brand = await _db.SupBrands
			.Include(x => x.ProdProducts)
			.Include(x => x.SupBrandFavorites)
			.Include(x => x.Supplier)
			.FirstOrDefaultAsync(x => x.BrandId == brandId);

		if (brand == null)
			return null;

		// 取得品牌相關的所有產品ID
		var productIds = brand.ProdProducts.Select(x => x.ProductId).ToList();

		// 計算所有產品的總銷量
		int totalSales = 0;
		if (productIds.Count > 0)
		{
			totalSales = await _db.OrdOrderItems
				.Where(x => productIds.Contains(x.ProductId))
				.SumAsync(x => (int?)x.Qty) ?? 0;
		}

		int daysAgo = (DateTime.Now - brand.CreatedDate).Days;

		return new BrandOverviewDto
		{
			ProductCount = brand.ProdProducts.Count,
			FavoriteCount = brand.SupBrandFavorites.Count,
			CreatedDaysAgo = daysAgo,
			SupplierName = brand.Supplier?.SupplierName ?? "",
			TotalSales = totalSales
		};
	}

	// 取得品牌銷售Top10排行榜
	public async Task<List<BrandSalesRankingDto>> GetTopBrandsBySalesAsync(int topN = 10)
	{
		// 1) 只以可比較欄位分組彙總：BrandId -> TotalSales
		var salesAgg = await (
			from p in _db.ProdProducts
			join i in _db.OrdOrderItems on p.ProductId equals i.ProductId			
			group i by p.BrandId into g
			select new
			{
				BrandId = g.Key,
				TotalSales = g.Sum(x => x.Qty)
			}
		)
		.OrderByDescending(x => x.TotalSales)
		.Take(topN)
		.ToListAsync();

		var brandIds = salesAgg.Select(x => x.BrandId).ToList();

		// 2) 取品牌名稱
		var brands = await _db.SupBrands
			.Where(b => brandIds.Contains(b.BrandId))
			.Select(b => new { b.BrandId, b.BrandName })
			.ToListAsync();


		// 3) 取 Logo：限定 FolderId=56、IsActive=1、AltText=BrandName，若同名多張則取最早建立（或最新，依需求）
		var logoQuery = from f in _db.SysAssetFiles
						join b in _db.SupBrands on f.AltText equals b.BrandName
						where f.FolderId == 56 && f.IsActive == true && brandIds.Contains(b.BrandId)
						select new
						{
							b.BrandId,
							f.FileUrl,
							f.CreatedDate
						};

		var logos = await logoQuery
			.GroupBy(x => x.BrandId)
			.Select(g => new
			{
				BrandId = g.Key,
				// 取最早建立的那張；若要最新就改 OrderByDescending
				FileUrl = g.OrderBy(x => x.CreatedDate).Select(x => x.FileUrl).FirstOrDefault()
			})
			.ToListAsync();

		// 4) 組裝結果（依銷量排序）
		var result = salesAgg
			.Join(brands, s => s.BrandId, b => b.BrandId, (s, b) => new { s, b })
			.GroupJoin(logos, sb => sb.b.BrandId, l => l.BrandId, (sb, lg) => new { sb, lg = lg.FirstOrDefault() })
			.Select(x => new BrandSalesRankingDto
			{
				BrandId = x.sb.b.BrandId,
				BrandName = x.sb.b.BrandName,
				LogoUrl = x.lg?.FileUrl ?? string.Empty,
				TotalSales = x.sb.s.TotalSales
			})
			.OrderByDescending(x => x.TotalSales)
			.ToList();

		return result;
	}


	public async Task<BrandAccordionContentDto?> GetAccordionByIdAsync(int contentId, CancellationToken ct)
	{
		var q = await _db.SupBrandAccordionContents
			.Where(x => x.ContentId == contentId && x.IsActive)
			.Select(x => new BrandAccordionContentDto
			{
				ContentId = x.ContentId,
				BrandId = x.BrandId,
				ContentTitle = x.ContentTitle,
				Content = x.Content,
				OrderSeq = x.OrderSeq,
				ImgId = x.ImgId,
				IsActive = x.IsActive,
				Creator = x.Creator,
				CreatedDate = x.CreatedDate,
				Reviser = x.Reviser,
				RevisedDate = x.RevisedDate
			})
			.FirstOrDefaultAsync(ct);

		return q;
	}

	public async Task<BrandArticleDto?> GetArticleByIdAsync(int contentId, CancellationToken ct)
	{
		var q = await _db.SupBrandArticles
			.Where(x => x.ContentId == contentId && x.IsActive)
			.Select(x => new BrandArticleDto
			{
				ContentId = x.ContentId,
				BrandId = x.BrandId,
				ImgId = x.ImgId,
				ContentTitle = x.ContentTitle,
				Content = x.Content,
				ContentType = x.ContentType,
				OrderSeq = x.OrderSeq,
				IsActive = x.IsActive,
				PublishDate = x.PublishDate,
				Creator = x.Creator,
				CreatedDate = x.CreatedDate,
				Reviser = x.Reviser,
				RevisedDate = x.RevisedDate
			})
			.FirstOrDefaultAsync(ct);

		return q;
	}

	public async Task<int?> GetBrandImgIdAsync(int brandId, CancellationToken ct)
	{
		return await _db.SupBrands
			.Where(b => b.BrandId == brandId && b.IsActive)
			.Select(b => b.ImgId)
			.FirstOrDefaultAsync(ct);
	}

	public async Task<BannerDto?> GetAssetFileAsBannerAsync(int fileId, CancellationToken ct)
	{
		var f = await _db.SysAssetFiles
			.Where(x => x.FileId == fileId && x.IsActive)
			.Select(x => new BannerDto
			{
				FileId = x.FileId,
				FileUrl = x.FileUrl,
				AltText = x.AltText,
				Caption = x.Caption,
				IsExternal = x.IsExternal,
				IsActive = x.IsActive,
				FileKey = x.FileKey
			})
			.FirstOrDefaultAsync(ct);

		return f;
	}


	#endregion

}
