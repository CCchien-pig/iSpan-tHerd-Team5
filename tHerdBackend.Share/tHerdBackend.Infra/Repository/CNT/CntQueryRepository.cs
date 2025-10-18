using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Share.DTOs.CNT;

namespace tHerdBackend.Infra.Repository.CNT
{
	/// <summary>
	/// CNT 前台查詢 Repository（Dapper + EF 混合）
	/// A階段修正版：
	/// - 修正只顯示一篇問題（COUNT 與主查詢條件一致、分頁正確）
	/// - 僅 Status=1（已發佈）會顯示，排序 PublishedDate DESC（NULL 置底）
	/// - 列表回傳 CategoryName + Tags[]，詳細頁包含 SEO 欄位
	/// - 內建「同分類推薦文章」查詢，交由 Service 合併回傳
	/// </summary>
	public class CntQueryRepository : ICntQueryRepository
	{
		private readonly ISqlConnectionFactory _factory;
		private readonly tHerdDBContext _db;

		public CntQueryRepository(ISqlConnectionFactory factory, tHerdDBContext db)
		{
			_factory = factory;
			_db = db;
		}

		/// <summary>
		/// 文章清單（支援分類、關鍵字、分頁）
		/// - Status = 1（已發佈）
		/// - 排序：PublishedDate DESC（NULL 最後）
		/// - 回傳：CategoryName、Tags[]
		/// </summary>
		public async Task<(IReadOnlyList<ArticleListDto> Items, int Total)> GetArticleListAsync(
			int? pageTypeId, string? keyword, int page, int pageSize, CancellationToken ct = default)
		{
			if (page <= 0) page = 1;
			if (pageSize <= 0 || pageSize > 100) pageSize = 12;

			int skip = (page - 1) * pageSize;
			int take = pageSize;

			var sql = new StringBuilder(@"
SELECT 
    p.PageId,
    p.Title,
    p.PageTypeId,
    pt.TypeName                                        AS CategoryName,
    p.PublishedDate,
    p.IsPaidContent,
    ISNULL(p.PreviewLength, 150)                       AS PreviewLength,
    s.SeoSlug                                          AS Slug,
    (
        SELECT TOP 1 b.Content
        FROM CNT_PageBlock b
        WHERE b.PageId = p.PageId 
          AND (b.BlockType IN ('paragraph','richtext','text'))
        ORDER BY b.OrderSeq
    )                                                  AS FirstText,
    (
        SELECT TOP 1 b.Content
        FROM CNT_PageBlock b
        WHERE b.PageId = p.PageId AND b.BlockType = 'image'
        ORDER BY b.OrderSeq
    )                                                  AS FirstImage,
    (
        SELECT STRING_AGG(t.TagName, ',')
        FROM CNT_PageTag pt2
        JOIN CNT_Tag     t   ON t.TagId = pt2.TagId
        WHERE pt2.PageId = p.PageId
    )                                                  AS TagList
FROM CNT_Page p
LEFT JOIN CNT_PageType pt ON pt.PageTypeId = p.PageTypeId
LEFT JOIN SYS_SeoMeta  s  ON s.SeoId = p.SeoId AND s.RefTable = 'CNT_Page'
WHERE p.IsDeleted = 0
  AND p.Status    = 1
");

			var countSql = new StringBuilder(@"
SELECT COUNT(1)
FROM CNT_Page p
WHERE p.IsDeleted = 0
  AND p.Status    = 1
");

			var param = new DynamicParameters();

			// 🔍 關鍵字（先鎖定 Title；B 階段可擴充全文/內容）
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				sql.Append(" AND p.Title LIKE CONCAT('%', @Keyword, '%')");
				countSql.Append(" AND p.Title LIKE CONCAT('%', @Keyword, '%')");
				param.Add("Keyword", keyword.Trim());
			}

			// 🗂 分類（pageTypeId）
			if (pageTypeId.HasValue)
			{
				sql.Append(" AND p.PageTypeId = @PageTypeId");
				countSql.Append(" AND p.PageTypeId = @PageTypeId");
				param.Add("PageTypeId", pageTypeId.Value);
			}

			// ✅ 最新文章優先；PublishedDate 為 NULL 時排最後
			sql.Append(@"
ORDER BY 
    CASE WHEN p.PublishedDate IS NULL THEN 1 ELSE 0 END,
    p.PublishedDate DESC
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;");

			param.Add("Skip", skip);
			param.Add("Take", take);

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				var rows = (await conn.QueryAsync(sql.ToString(), param, tx)).ToList();
				var total = await conn.ExecuteScalarAsync<int>(countSql.ToString(), param, tx);

				var items = rows.Select(r =>
				{
					int previewLen = (int)r.PreviewLength;
					string firstText = (r.FirstText as string) ?? string.Empty;
					var excerpt = TrimToPreview(firstText, previewLen);

					string cover = (r.FirstImage as string) ?? "/images/placeholder-article.jpg";
					DateTime pub = r.PublishedDate ?? DateTime.MinValue;

					// Tags：以逗號字串轉陣列；移除空白與重複
					string[] tags = Array.Empty<string>();
					if (r.TagList is string tagCsv && !string.IsNullOrWhiteSpace(tagCsv))
						tags = tagCsv.Split(',')
							.Select(s => s.Trim())
							.Where(s => s.Length > 0)
							.Distinct()
							.ToArray();

					return new ArticleListDto
					{
						PageId = r.PageId,
						Title = r.Title,
						Slug = (r.Slug as string) ?? r.PageId.ToString(),
						Excerpt = excerpt,
						CoverImage = cover,
						CategoryName = r.CategoryName ?? "未分類",
						PublishedDate = pub,
						IsPaidContent = (bool)r.IsPaidContent,
						Tags = tags
					};
				}).ToList();

				return (items, total);
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		/// <summary>
		/// 文章詳細（含 SEO 與 Tags；Blocks 依付費狀態回傳）
		/// - 免費或已購：回傳 Blocks
		/// - 未購：不回 Blocks（交由前端顯示遮罩；或 Service 塞預覽片段）
		/// </summary>
		public async Task<ArticleDetailDto?> GetArticleDetailAsync(
			int pageId, int? userNumberId, CancellationToken ct = default)
		{
			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				// 主檔 + SEO（帶 PageTypeId 以便推薦用）
				const string mainSql = @"
SELECT 
    p.PageId, p.Title, p.PublishedDate, p.PageTypeId,
    p.IsPaidContent, ISNULL(p.PreviewLength,150) AS PreviewLength,
    s.SeoSlug AS Slug, s.SeoTitle, s.SeoDesc
FROM CNT_Page p
LEFT JOIN SYS_SeoMeta s ON s.SeoId = p.SeoId AND s.RefTable = 'CNT_Page'
WHERE p.PageId = @PageId AND p.IsDeleted = 0;";
				var main = await conn.QueryFirstOrDefaultAsync(mainSql, new { PageId = pageId }, tx);
				if (main == null) return null;

				// Tags
				const string tagSql = @"
SELECT t.TagName
FROM CNT_PageTag pt
JOIN CNT_Tag t ON t.TagId = pt.TagId
WHERE pt.PageId = @PageId
ORDER BY t.TagName;";
				var tags = (await conn.QueryAsync<string>(tagSql, new { PageId = pageId }, tx)).ToList();

				// 付費狀態判斷（目前你會先全部設0，但邏輯入口保留）
				bool hasPurchased = false;
				if ((bool)main.IsPaidContent && userNumberId.HasValue)
				{
					const string paidSql = @"
SELECT TOP 1 1 
FROM CNT_Purchase 
WHERE PageId=@PageId AND UserNumberId=@Uid AND IsPaid=1;";
					hasPurchased = await conn.ExecuteScalarAsync<int?>(paidSql, new { PageId = pageId, Uid = userNumberId.Value }, tx) == 1;
				}

				var dto = new ArticleDetailDto
				{
					PageId = main.PageId,
					Title = main.Title,
					Slug = (main.Slug as string) ?? main.PageId.ToString(),
					SeoTitle = main.SeoTitle as string,
					SeoDesc = main.SeoDesc as string,
					PublishedDate = main.PublishedDate ?? DateTime.MinValue,
					IsPaidContent = main.IsPaidContent,
					HasPurchased = hasPurchased,
					PreviewLength = (int)main.PreviewLength,
					Tags = tags
				};

				// Blocks：免費或已購 → 回傳全文；未購買 → 不回 Blocks
				if (!dto.IsPaidContent || dto.HasPurchased)
				{
					const string blockSql = @"
SELECT OrderSeq   AS [Order],
       BlockType  AS BlockType,
       Content    AS Content
FROM CNT_PageBlock
WHERE PageId = @PageId
ORDER BY OrderSeq;";
					var blocks = await conn.QueryAsync<PageBlockDto>(blockSql, new { PageId = pageId }, tx);
					dto.Blocks = blocks.ToList();
				}

				return dto;
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		/// <summary>
		/// 取得「同分類推薦文章」（不含自己；最新發佈優先）
		/// - 用於詳細頁右側／底部推薦卡
		/// - 建議由 ContentService 呼叫並合併到對外 ViewModel 返回
		/// </summary>
		public async Task<IReadOnlyList<ArticleListDto>> GetRecommendedByCategoryAsync(
			int currentPageId, int pageTypeId, int topN = 6, CancellationToken ct = default)
		{
			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				const string recSql = @"
SELECT TOP (@TopN)
    p.PageId,
    p.Title,
    p.PublishedDate,
    s.SeoSlug AS Slug,
    (
        SELECT TOP 1 b.Content
        FROM CNT_PageBlock b
        WHERE b.PageId = p.PageId AND b.BlockType = 'image'
        ORDER BY b.OrderSeq
    ) AS FirstImage
FROM CNT_Page p
LEFT JOIN SYS_SeoMeta s ON s.SeoId = p.SeoId AND s.RefTable = 'CNT_Page'
WHERE p.IsDeleted = 0
  AND p.Status    = 1
  AND p.PageTypeId = @PageTypeId
  AND p.PageId    <> @CurrentId
ORDER BY 
    CASE WHEN p.PublishedDate IS NULL THEN 1 ELSE 0 END,
    p.PublishedDate DESC;";

				var rows = (await conn.QueryAsync(recSql, new
				{
					TopN = topN,
					PageTypeId = pageTypeId,
					CurrentId = currentPageId
				}, tx)).ToList();

				var list = rows.Select(r => new ArticleListDto
				{
					PageId = r.PageId,
					Title = r.Title,
					Slug = (r.Slug as string) ?? r.PageId.ToString(),
					Excerpt = string.Empty, // 推薦卡不需要摘要
					CoverImage = (r.FirstImage as string) ?? "/images/placeholder-article.jpg",
					CategoryName = string.Empty,
					PublishedDate = r.PublishedDate ?? DateTime.MinValue,
					IsPaidContent = false,
					Tags = Array.Empty<string>()
				}).ToList();

				return list;
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		// ========= Helpers =========
		private static string TrimToPreview(string raw, int limit)
		{
			if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
			var s = StripBasicHtml(raw);
			return s.Length <= limit ? s : s[..Math.Max(0, limit)].Trim() + "…";
		}

		private static string StripBasicHtml(string html)
		{
			var sb = new StringBuilder(html.Length);
			bool inside = false;
			foreach (var ch in html)
			{
				if (ch == '<') { inside = true; continue; }
				if (ch == '>') { inside = false; continue; }
				if (!inside) sb.Append(ch);
			}
			return sb.ToString();
		}

		/// <summary>
		/// 產生「標籤導購商品」的前端 URL。
		/// 規範：/prod/search?q={tag}  （前端 router 依 SEO 規範處理）
		/// </summary>
		private static string BuildProductSearchUrl(string tag)
		{
			// 後端僅提供規範示例；實際導購連結可由前端在渲染 Tag 時組合
			// 參考：/prod/search?q=vitamin+c
			return $"/prod/search?q={Uri.EscapeDataString(tag ?? string.Empty)}";
		}
	}
}
