using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Share.DTOs.CNT;

namespace tHerdBackend.Infra.Repository.CNT
{
	/// <summary>
	/// CNT 前台查詢 Repository（Dapper 為主 + EF 供連線管理）
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

		// ===== List =====
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

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				sql.Append(" AND p.Title LIKE CONCAT('%', @Keyword, '%')");
				countSql.Append(" AND p.Title LIKE CONCAT('%', @Keyword, '%')");
				param.Add("Keyword", keyword.Trim());
			}

			if (pageTypeId.HasValue)
			{
				sql.Append(" AND p.PageTypeId = @PageTypeId");
				countSql.Append(" AND p.PageTypeId = @PageTypeId");
				param.Add("PageTypeId", pageTypeId.Value);
			}

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

					string[] tags = Array.Empty<string>();
					if (r.TagList is string tagCsv && !string.IsNullOrWhiteSpace(tagCsv))
						tags = tagCsv.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToArray();

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

		// ===== Detail =====
		public async Task<ArticleDetailDto?> GetArticleDetailAsync(
			int pageId, int? userNumberId, CancellationToken ct = default)
		{
			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
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

				const string tagSql = @"
SELECT t.TagName
FROM CNT_PageTag pt
JOIN CNT_Tag t ON t.TagId = pt.TagId
WHERE pt.PageId = @PageId
ORDER BY t.TagName;";
				var tags = (await conn.QueryAsync<string>(tagSql, new { PageId = pageId }, tx)).ToList();

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
					Tags = tags,
					// ★ 關鍵：把 PageTypeId 塞進 DTO，給 Service 作推薦使用
					PageTypeId = (int)main.PageTypeId
				};

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
		/// 取得所有文章分類與文章數量
		/// </summary>
		public async Task<IEnumerable<ArticleCategoryDto>> GetArticleCategoriesAsync(CancellationToken ct = default)
		{
			const string sql = @"
                SELECT 
                    t.PageTypeId AS Id,
                    t.TypeName   AS Name,
                    COUNT(p.PageId) AS ArticleCount
                FROM CNT_PageType t
                LEFT JOIN CNT_Page p
                    ON p.PageTypeId = t.PageTypeId
                    AND p.IsDeleted = 0
                    AND p.Status = 0   -- 只顯示正常狀態文章
                GROUP BY t.PageTypeId, t.TypeName
                ORDER BY t.PageTypeId;
            ";

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				var result = await conn.QueryAsync<ArticleCategoryDto>(sql, null, tx);
				return result.ToList();
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		// ===== Recommended =====
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
					Excerpt = string.Empty,
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

		// ===== Helpers =====
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
	}
}
