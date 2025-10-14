using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace tHerdBackend.SharedApi.Controllers.Module.CS
{
    [ApiController]
    [Route("api/cs/faq/[action]")]
    public class FaqController : ControllerBase
    {
        private readonly string _connStr;
        public FaqController(IConfiguration config)
            => _connStr = config.GetConnectionString("DefaultConnection");

        // GET /api/cs/faq/list
        [HttpGet]
        public async Task<IActionResult> List()
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

            var cats = (await multi.ReadAsync<CategoryRow>()).ToList();
            var faqs = (await multi.ReadAsync<FaqRow>()).ToList();

            var result = cats.Select(c => new
            {
                categoryId = c.CategoryId,
                categoryName = c.CategoryName,
                faqs = faqs.Where(f => f.CategoryId == c.CategoryId)
                           .Select(f => new { faqId = f.FaqId, title = f.Title, answerHtml = f.AnswerHtml })
            });

            return Ok(result);
        }

        // GET /api/cs/faq/search?q=退款
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
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
            var rows = await cn.QueryAsync<SearchRow>(sql, new { Q = q.Trim() });

            var result = rows.Select(r => new {
                faqId = r.FaqId,
                title = r.Title,
                answerHtml = r.AnswerHtml,
                categoryId = r.CategoryId,
                categoryName = r.CategoryName
            });
            return Ok(result);
        }

        // POST /api/cs/faq/feedback
        [HttpPost]
        public async Task<IActionResult> Feedback([FromBody] FeedbackIn dto)
        {
            const string sql = @"
INSERT INTO CS_FaqFeedback(FaqId, IsHelpful, UserId, ClientSessionKey)
VALUES(@FaqId, @IsHelpful, @UserId, @ClientSessionKey);";
            using var cn = new SqlConnection(_connStr);
            await cn.ExecuteAsync(sql, dto);
            return Ok(new { ok = true });
        }

        private sealed class CategoryRow { public int CategoryId { get; set; } public string CategoryName { get; set; } = ""; public int OrderSeq { get; set; } }
        private  class FaqRow { public int FaqId { get; set; } public string Title { get; set; } = ""; public string AnswerHtml { get; set; } = ""; public int CategoryId { get; set; } public int OrderSeq { get; set; } }
        private sealed class SearchRow : FaqRow { public string CategoryName { get; set; } = ""; }
        public sealed class FeedbackIn { public int FaqId { get; set; } public bool IsHelpful { get; set; } public int? UserId { get; set; } public string? ClientSessionKey { get; set; } }
    }

}
