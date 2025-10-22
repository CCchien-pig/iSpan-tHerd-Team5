using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using tHerdBackend.Core.Dtos;
using tHerdBackend.Core.Interfaces.Nutrition; // INutritionRepository
using tHerdBackend.Infra.DBSetting;           // ISqlConnectionFactory
using tHerdBackend.Infra.Helpers;             // DbConnectionHelper
using tHerdBackend.Infra.Models;              // tHerdDBContext

namespace tHerdBackend.Infra.Repository.CNT
{
	/// <summary>
	/// 營養分析模組 Repository（Dapper 為主，EF Core 僅管理連線生命週期）
	/// 表名全面使用 CNT_ 前綴版：
	///   CNT_Sample / CNT_FoodCategory / CNT_Measurement / CNT_Analyte / CNT_AnalyteCategory
	/// </summary>
	public class NutritionRepository : INutritionRepository
	{
		private readonly ISqlConnectionFactory _factory;
		private readonly tHerdDBContext _db;

		private static readonly Regex NutrientSortRegex =
			new Regex(@"^nutrient:(?<aid>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public NutritionRepository(ISqlConnectionFactory factory, tHerdDBContext db)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_db = db ?? throw new ArgumentNullException(nameof(db));
		}

		// =========================================================================
		// 🆕 GetSampleByIdAsync：查單一 Sample（Detail 專用）
		// =========================================================================
		public async Task<dynamic?> GetSampleByIdAsync(int sampleId, CancellationToken ct = default)
		{
			const string sql = @"
SELECT
    s.SampleId,
    s.SampleName,
    s.SampleNameEn,
    s.ContentDesc,
    c.CategoryName
FROM dbo.CNT_Sample s
JOIN dbo.CNT_FoodCategory c ON c.CategoryId = s.CategoryId
WHERE s.SampleId = @SampleId;
";

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				return await conn.QueryFirstOrDefaultAsync(sql, new { SampleId = sampleId }, tx);
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		// =========================================================================
		// GetSamplesAsync（原本列表查詢保留）
		// =========================================================================
		public async Task<(IReadOnlyList<dynamic> Items, int TotalCount)> GetSamplesAsync(
			string? keyword,
			int? categoryId,
			string? sort,
			int page,
			int pageSize,
			CancellationToken ct = default)
		{
			if (page <= 0) page = 1;
			if (pageSize <= 0 || pageSize > 100) pageSize = 20;

			int skip = (page - 1) * pageSize;
			int take = pageSize;

			int? nutrientAnalyteId = TryParseNutrientSort(sort);

			var sb = new StringBuilder(@"
SELECT
    s.SampleId,
    CASE 
        WHEN NULLIF(LTRIM(RTRIM(s.SampleNameEn)),'') IS NOT NULL 
             THEN CONCAT(s.SampleName, ' (', s.SampleNameEn, ')')
        ELSE s.SampleName
    END                 AS DisplayName,
    NULLIF(LTRIM(RTRIM(s.AliasName)),'')   AS AliasName,
    s.CategoryId,
    c.CategoryName,
    NULLIF(LTRIM(RTRIM(s.ContentDesc)),'') AS ContentDesc");

			if (nutrientAnalyteId.HasValue)
			{
				sb.Append(@",
    MAX(CASE WHEN m.AnalyteId = @NutrientAid THEN m.ValuePer100g END) AS NutrientValue");
			}

			sb.Append(@"
FROM dbo.CNT_Sample s
JOIN dbo.CNT_FoodCategory c ON c.CategoryId = s.CategoryId
");

			if (nutrientAnalyteId.HasValue)
			{
				sb.Append(@"
LEFT JOIN dbo.CNT_Measurement m 
       ON m.SampleId = s.SampleId 
      AND m.AnalyteId = @NutrientAid
");
			}

			sb.Append(@"
WHERE 1=1
");

			var cnt = new StringBuilder(@"
SELECT COUNT(1)
FROM dbo.CNT_Sample s
WHERE 1=1
");

			var param = new DynamicParameters();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				param.Add("Keyword", keyword.Trim());
				sb.Append(@"
  AND (
        s.SampleName      LIKE CONCAT('%', @Keyword, '%') OR
        s.SampleNameEn    LIKE CONCAT('%', @Keyword, '%') OR
        s.AliasName       LIKE CONCAT('%', @Keyword, '%')
      )
");
				cnt.Append(@"
  AND (
        s.SampleName      LIKE CONCAT('%', @Keyword, '%') OR
        s.SampleNameEn    LIKE CONCAT('%', @Keyword, '%') OR
        s.AliasName       LIKE CONCAT('%', @Keyword, '%')
      )
");
			}

			if (categoryId.HasValue)
			{
				param.Add("CategoryId", categoryId.Value);
				sb.Append("  AND s.CategoryId = @CategoryId\n");
				cnt.Append("  AND s.CategoryId = @CategoryId\n");
			}

			if (nutrientAnalyteId.HasValue)
			{
				param.Add("NutrientAid", nutrientAnalyteId.Value);
			}

			sb.Append(@"
GROUP BY 
    s.SampleId, s.SampleName, s.SampleNameEn, s.AliasName, s.CategoryId, c.CategoryName, s.ContentDesc
");

			AppendOrderBy(sb, sort, nutrientAnalyteId.HasValue);

			sb.Append(@"
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
");
			param.Add("Skip", skip);
			param.Add("Take", take);

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				var rows = (await conn.QueryAsync(sb.ToString(), param, tx)).ToList();
				int total = await conn.ExecuteScalarAsync<int>(cnt.ToString(), param, tx);
				return (rows, total);
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		// =========================================================================
		// GetNutrientsBySampleIdAsync（原樣保留）
		// =========================================================================
		public async Task<IReadOnlyList<dynamic>> GetNutrientsBySampleIdAsync(
			int sampleId,
			CancellationToken ct = default)
		{
			const string sql = @"
SELECT
    ac.CategoryName                                        AS Category,
    a.AnalyteName                                          AS [Name],
    ISNULL(NULLIF(LTRIM(RTRIM(m.Unit)),''), a.DefaultUnit) AS [Unit],
    m.ValuePer100g                                         AS ValuePer100g
FROM dbo.CNT_Measurement      m
JOIN dbo.CNT_Analyte          a  ON a.AnalyteId = m.AnalyteId
JOIN dbo.CNT_AnalyteCategory  ac ON ac.AnalyteCategoryId = a.AnalyteCategoryId
WHERE m.SampleId = @SampleId
ORDER BY
    ac.CategoryName ASC,
    a.AnalyteName  ASC;";

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				var rows = (await conn.QueryAsync(sql, new { SampleId = sampleId }, tx)).ToList();
				return rows;
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		public async Task<IReadOnlyList<dynamic>> CompareNutritionAsync(
			IEnumerable<int> sampleIds,
			IEnumerable<int> analyteIds,
			CancellationToken ct = default)
		{
			const string sql = @"
WITH sid AS (
  SELECT TRY_CAST(value AS int) AS SampleId
  FROM STRING_SPLIT(@SampleIds, ',')
),
aid AS (
  SELECT TRY_CAST(value AS int) AS AnalyteId
  FROM STRING_SPLIT(@AnalyteIds, ',')
)
SELECT 
    s.SampleId,
    sm.SampleName,
    a.AnalyteId,
    a.AnalyteName,
    ISNULL(m.Unit, a.DefaultUnit) AS Unit,
    ISNULL(m.ValuePer100g, 0) AS ValuePer100g
FROM sid s
JOIN CNT_Sample sm ON sm.SampleId = s.SampleId
JOIN aid aa ON 1=1
JOIN CNT_Analyte a ON a.AnalyteId = aa.AnalyteId
LEFT JOIN CNT_Measurement m 
       ON m.SampleId = s.SampleId AND m.AnalyteId = aa.AnalyteId
ORDER BY a.AnalyteCategoryId, a.AnalyteName;
";

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				var rows = await conn.QueryAsync(sql, new
				{
					SampleIds = string.Join(",", sampleIds),
					AnalyteIds = string.Join(",", analyteIds)
				}, tx);
				return rows.ToList();
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		public async Task<IReadOnlyList<dynamic>> GetAnalytesAsync(bool isPopular, CancellationToken ct = default)
		{
			var popularList = new[]
			{
		"熱量", "總碳水化合物", "水分", "粗蛋白", "膳食纖維", "粗脂肪", "飽和脂肪", 
		"鉀","磷", "鎂", "鈣", "鈉","鐵", "鋅", "維生素B1", "維生素C", 
		"α-維生素E當量(α-TE)", "維生素E總量"
	};

			var sql = @"
SELECT 
    a.AnalyteId,
    a.AnalyteName,
    a.DefaultUnit AS Unit,
    ac.CategoryName AS Category
FROM dbo.CNT_Analyte a
JOIN dbo.CNT_AnalyteCategory ac ON ac.AnalyteCategoryId = a.AnalyteCategoryId
";

			if (isPopular)
				sql += "WHERE a.AnalyteName IN @Names\n";

			sql += "ORDER BY ac.CategoryName, a.AnalyteName;";

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				var rows = await conn.QueryAsync(sql, new { Names = popularList }, tx);
				return rows.ToList();
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}



		public async Task<IReadOnlyList<FoodCategoryDto>> GetFoodCategoriesAsync(CancellationToken ct = default)
		{
			const string sql = @"
SELECT
    c.CategoryId AS [Id],
    c.CategoryName AS [Name]
FROM dbo.CNT_FoodCategory c
ORDER BY c.CategoryId ASC;";

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				var rows = await conn.QueryAsync<FoodCategoryDto>(sql, transaction: tx);
				return rows.ToList();
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}

		public async Task<IReadOnlyList<dynamic>> GetAllSamplesAsync(
			string? keyword,
			int? categoryId,
			string? sort,
			CancellationToken ct = default)
		{
			int? nutrientAnalyteId = TryParseNutrientSort(sort);

			var sb = new StringBuilder(@"
SELECT
    s.SampleId,
    CASE 
        WHEN NULLIF(LTRIM(RTRIM(s.SampleNameEn)),'') IS NOT NULL 
             THEN CONCAT(s.SampleName, ' (', s.SampleNameEn, ')')
        ELSE s.SampleName
    END                 AS DisplayName,
    NULLIF(LTRIM(RTRIM(s.AliasName)),'')   AS AliasName,
    s.CategoryId,
    c.CategoryName,
    NULLIF(LTRIM(RTRIM(s.ContentDesc)),'') AS ContentDesc");

			if (nutrientAnalyteId.HasValue)
			{
				sb.Append(@",
    MAX(CASE WHEN m.AnalyteId = @NutrientAid THEN m.ValuePer100g END) AS NutrientValue");
			}

			sb.Append(@"
FROM dbo.CNT_Sample s
JOIN dbo.CNT_FoodCategory c ON c.CategoryId = s.CategoryId
");

			if (nutrientAnalyteId.HasValue)
			{
				sb.Append(@"
LEFT JOIN dbo.CNT_Measurement m 
       ON m.SampleId = s.SampleId 
      AND m.AnalyteId = @NutrientAid
");
			}

			sb.Append(@"
WHERE 1=1
");

			var param = new DynamicParameters();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				param.Add("Keyword", keyword.Trim());
				sb.Append(@"
  AND (
        s.SampleName      LIKE CONCAT('%', @Keyword, '%') OR
        s.SampleNameEn    LIKE CONCAT('%', @Keyword, '%') OR
        s.AliasName       LIKE CONCAT('%', @Keyword, '%')
      )
");
			}

			if (categoryId.HasValue)
			{
				param.Add("CategoryId", categoryId.Value);
				sb.Append("  AND s.CategoryId = @CategoryId\n");
			}

			if (nutrientAnalyteId.HasValue)
				param.Add("NutrientAid", nutrientAnalyteId.Value);

			sb.Append(@"
GROUP BY 
    s.SampleId, s.SampleName, s.SampleNameEn, s.AliasName, s.CategoryId, c.CategoryName, s.ContentDesc
");

			// ⬅️ 不加 OFFSET/FETCH（不分頁）
			AppendOrderBy(sb, sort, nutrientAnalyteId.HasValue);

			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
			try
			{
				var rows = (await conn.QueryAsync(sb.ToString(), param, tx)).ToList();
				return rows;
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}


		private static int? TryParseNutrientSort(string? sort)
		{
			if (string.IsNullOrWhiteSpace(sort)) return null;

			var m = NutrientSortRegex.Match(sort.Trim());
			if (m.Success && int.TryParse(m.Groups["aid"].Value, out int aid))
				return aid;

			return null;
		}

		private static void AppendOrderBy(StringBuilder sb, string? sort, bool hasNutrient)
		{
			var s = (sort ?? string.Empty).Trim().ToLowerInvariant();

			if (s.StartsWith("nutrient:") && hasNutrient)
			{
				sb.Append(@"
ORDER BY 
    NutrientValue DESC,
    s.SampleName ASC
");
				return;
			}

			switch (s)
			{
				case "newest":
					sb.Append(@"
ORDER BY 
    s.SampleId DESC, 
    s.SampleName ASC
");
					break;

				case "category":
					sb.Append(@"
ORDER BY 
    s.CategoryId ASC, 
    s.SampleName ASC
");
					break;

				case "popular":
					sb.Append(@"
ORDER BY 
    s.SampleName ASC
");
					break;

				case "name":
				default:
					sb.Append(@"
ORDER BY 
    s.SampleName ASC
");
					break;
			}
		}
	}
}
