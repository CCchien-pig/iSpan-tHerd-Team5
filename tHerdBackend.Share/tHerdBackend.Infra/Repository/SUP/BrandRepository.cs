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
    Id     = bptf.[BrandProductTypeFilterId],  -- int
    Text   = bptf.[ButtonText],               -- nvarchar
    [Order]= bptf.[ButtonOrder]               -- int
FROM SUP_BrandProductTypeFilter bptf
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

	public async Task<BrandOverviewDto> GetBrandOverviewAsync(int brandId)
	{
		var brand = await _db.SupBrands
			.Include(x => x.ProdProducts)
			.Include(x => x.SupBrandFavorites)
			.Include(x => x.Supplier)
			.FirstOrDefaultAsync(x => x.BrandId == brandId);

		if (brand == null)
			return null;

		// 計算距今天數
		int daysAgo = (DateTime.Now - brand.CreatedDate).Days;

		// 從訂單明細累加該品牌所有產品銷量
		//先抓該品牌下所有商品 ProductId，再依 OrderItem 中符合商品的 ProductId 做銷量 Qty 加總
		var productIds = brand.ProdProducts.Select(p => p.ProductId).ToList();

		int totalSalesQty = 0;
		if (productIds.Any())
		{
			totalSalesQty = await _db.OrdOrderItems
				.Where(oi => productIds.Contains(oi.ProductId))
				.SumAsync(oi => (int?)oi.Qty) ?? 0;
		}

		return new BrandOverviewDto
		{
			ProductCount = brand.ProdProducts.Count,
			FavoriteCount = brand.SupBrandFavorites.Count,
			CreatedDaysAgo = daysAgo,
			SupplierName = brand.Supplier?.SupplierName ?? "",
			TotalSalesQty = totalSalesQty
		};
	}



	#endregion

}
