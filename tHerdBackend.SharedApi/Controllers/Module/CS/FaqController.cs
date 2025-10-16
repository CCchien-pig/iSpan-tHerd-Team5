using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

using tHerdBackend.Core.ValueObjects; // 放 ApiResponse<T> 的命名空間

namespace tHerdBackend.SharedApi.Controllers.Module.CS
{
    /// <summary>
    /// 常見問題（FAQ）查詢、搜尋與回饋
    /// </summary>
    [ApiController]
    [Authorize] // 規範：預設受保護；公開端點另加 [AllowAnonymous]
    [Route("api/cs/[controller]")] // => /api/cs/Faqs/...
    public class FaqsController : ControllerBase
    {
        private readonly string _connStr;
        public FaqsController(IConfiguration config)
            => _connStr = config.GetConnectionString("DefaultConnection");

        /// <summary>
        /// 取得「分類＋其下 FAQ 列表」（僅啟用）
        /// </summary>
        /// <remarks>前台頁使用。回傳每個分類含該分類 FAQ 的集合。</remarks>
        /// <response code="200">成功</response>
        [HttpGet("list")]
        [AllowAnonymous] // 前台公開查詢
        public async Task<IActionResult> GetListAsync()
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

            try
            {
                using var cn = new SqlConnection(_connStr);
                using var multi = await cn.QueryMultipleAsync(sql);

                var cats = (await multi.ReadAsync<CategoryRow>()).ToList();
                var faqs = (await multi.ReadAsync<FaqRow>()).ToList();

                var data = cats.Select(c => new CategoryWithFaqsDto
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

                return Ok(ApiResponse<List<CategoryWithFaqsDto>>.Ok(data));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// 搜尋 FAQ（標題 / 內容 / 關鍵字）
        /// </summary>
        /// <param name="q">關鍵字（必填）</param>
        /// <response code="200">成功</response>
        [HttpGet("search")]
        [AllowAnonymous] // 前台公開查詢
        public async Task<IActionResult> SearchAsync([FromQuery, Required] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(ApiResponse<List<FaqSearchDto>>.Ok(new List<FaqSearchDto>()));

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

            try
            {
                using var cn = new SqlConnection(_connStr);
                var rows = await cn.QueryAsync<SearchRow>(sql, new { Q = q.Trim() });

                var data = rows.Select(r => new FaqSearchDto
                {
                    FaqId = r.FaqId,
                    Title = r.Title,
                    AnswerHtml = r.AnswerHtml,
                    CategoryId = r.CategoryId,
                    CategoryName = r.CategoryName
                }).ToList();

                return Ok(ApiResponse<List<FaqSearchDto>>.Ok(data));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// 回報 FAQ 是否有幫助（👍/👎）
        /// </summary>
        /// <remarks>預設需要登入；若要開放匿名，請改成 [AllowAnonymous] 並在資料表用 ClientSessionKey 區分。</remarks>
        /// <response code="200">成功</response>
        [HttpPost("feedback")]
        public async Task<IActionResult> FeedbackAsync([FromBody] FeedbackIn dto)
        {
            if (dto is null) return BadRequest(ApiResponse<string>.Fail("Invalid payload"));

            const string sql = @"
INSERT INTO CS_FaqFeedback(FaqId, IsHelpful, UserId, ClientSessionKey)
VALUES(@FaqId, @IsHelpful, @UserId, @ClientSessionKey);";

            try
            {
                using var cn = new SqlConnection(_connStr);
                var affected = await cn.ExecuteAsync(sql, dto);
                if (affected <= 0) return BadRequest(ApiResponse<string>.Fail("Insert failed"));

                return Ok(ApiResponse<object>.Ok(new { ok = true }, "新增成功"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
        }

        #region Rows / DTOs

        private sealed class CategoryRow
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
            public int OrderSeq { get; set; }
        }

        private class FaqRow
        {
            public int FaqId { get; set; }
            public string Title { get; set; } = "";
            public string AnswerHtml { get; set; } = "";
            public int CategoryId { get; set; }
            public int OrderSeq { get; set; }
        }

        private sealed class SearchRow : FaqRow
        {
            public string CategoryName { get; set; } = "";
        }

        public sealed class FeedbackIn
        {
            public int FaqId { get; set; }
            public bool IsHelpful { get; set; }
            public int? UserId { get; set; }
            public string? ClientSessionKey { get; set; }
        }

        public sealed class FaqBriefDto
        {
            public int FaqId { get; set; }
            public string Title { get; set; } = "";
            public string AnswerHtml { get; set; } = "";
        }

        public sealed class CategoryWithFaqsDto
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
            public List<FaqBriefDto> Faqs { get; set; } = new();
        }

        public sealed class FaqSearchDto
        {
            public int FaqId { get; set; }
            public string Title { get; set; } = "";
            public string AnswerHtml { get; set; } = "";
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
        }

        #endregion
    }
}
