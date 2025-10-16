using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.CS;

namespace tHerdBackend.Infra.Repository.CS
{
	public sealed class FaqRepository : IFaqRepository
	{
		private readonly string _connStr;
		public FaqRepository(IConfiguration config)
			=> _connStr = config.GetConnectionString("DefaultConnection");

		public async Task<List<CategoryWithFaqsDto>> GetListAsync()
		{
			const string sql = @"
SELECT CategoryId, CategoryName, OrderSeq
FROM CS_FaqCategory
WHERE IsActive = 1
ORDER BY OrderSeq, CategoryId;

SELECT FaqId, Title, AnswerHtml, CategoryId, OrderSeq
FROM CS_Faq
WHERE IsActive = 1
ORDER BY CategoryId, OrderSeq, FaqId;";

			using var cn = new SqlConnection(_connStr);
			using var multi = await cn.QueryMultipleAsync(sql);

			var cats = (await multi.ReadAsync<(int CategoryId, string CategoryName, int OrderSeq)>()).ToList();
			var faqs = (await multi.ReadAsync<(int FaqId, string Title, string AnswerHtml, int CategoryId, int OrderSeq)>()).ToList();

			var result = cats.Select(c => new CategoryWithFaqsDto
			{
				CategoryId = c.CategoryId,
				CategoryName = c.CategoryName,
				Faqs = faqs.Where(f => f.CategoryId == c.CategoryId)
						   .Select(f => new FaqBriefDto
						   {
							   FaqId = f.FaqId,
							   Title = f.Title,
							   AnswerHtml = f.AnswerHtml
						   }).ToList()
			}).ToList();

			return result;
		}

		public async Task<List<FaqSearchDto>> SearchAsync(string keyword)
		{
			const string sql = @"
DECLARE @kw NVARCHAR(100) = @Q;
SELECT TOP 20
    f.FaqId, f.Title, f.AnswerHtml, f.CategoryId, c.CategoryName,
    Score =
        (CASE WHEN f.Title      LIKE '%'+@kw+'%' THEN 10 ELSE 0 END) +
        (CASE WHEN f.AnswerHtml LIKE '%'+@kw+'%' THEN  4 ELSE 0 END) +
        (CASE WHEN EXISTS (SELECT 1 FROM CS_FaqKeyword k WHERE k.FaqId=f.FaqId AND k.Keyword LIKE '%'+@kw+'%') THEN 6 ELSE 0 END)
FROM CS_Faq f
JOIN CS_FaqCategory c ON c.CategoryId = f.CategoryId AND c.IsActive=1
WHERE f.IsActive = 1
  AND (f.Title LIKE '%'+@kw+'%' OR f.AnswerHtml LIKE '%'+@kw+'%' OR
       EXISTS (SELECT 1 FROM CS_FaqKeyword k WHERE k.FaqId=f.FaqId AND k.Keyword LIKE '%'+@kw+'%'))
ORDER BY Score DESC, f.OrderSeq, f.FaqId;";

			using var cn = new SqlConnection(_connStr);
			var rows = await cn.QueryAsync<FaqSearchDto>(sql, new { Q = keyword });
			return rows.ToList();
		}

		public async Task<int> AddFeedbackAsync(FaqFeedbackIn input)
		{
			const string sql = @"
INSERT INTO CS_FaqFeedback(FaqId, IsHelpful, UserId, ClientSessionKey)
VALUES(@FaqId, @IsHelpful, @UserId, @ClientSessionKey);";
			using var cn = new SqlConnection(_connStr);
			return await cn.ExecuteAsync(sql, input);
		}
	}
}
