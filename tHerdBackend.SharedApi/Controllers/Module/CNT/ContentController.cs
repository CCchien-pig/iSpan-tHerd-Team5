using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.CNT;

namespace tHerdBackend.SharedApi.Controllers.Module.CNT
{
	/// <summary>
	/// CNT 前台內容 API（RESTful）
	/// - /api/cnt/list                  文章清單
	/// - /api/cnt/articles/{id}         文章詳情 + 推薦
	/// </summary>
	[ApiController]
	[Route("api/cnt")]
	public class ContentController : ControllerBase
	{
		private readonly IContentService _svc;

		public ContentController(IContentService svc)
		{
			_svc = svc;
		}

		/// <summary>
		/// 文章清單（支援分類、關鍵字、分頁）
		/// GET /api/cnt/list?categoryId=1&q=魚油&page=1&pageSize=12
		/// </summary>
		[HttpGet("list")]
		public async Task<IActionResult> GetList(
			[FromQuery] int? categoryId,
			[FromQuery] string? q,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 12)
		{
			var (items, total) = await _svc.GetArticleListAsync(categoryId, q, page, pageSize);
			return Ok(new { items, total, page, pageSize });
		}

		/// <summary>
		/// 單篇文章（含 SEO / Blocks / Tags / 付費檢查 / 同分類推薦）
		/// GET /api/cnt/articles/{id}
		/// </summary>
		[HttpGet("articles/{id:int}")]
		public async Task<IActionResult> GetArticle([FromRoute] int id)
		{
			// 自動偵測 JWT 的可能使用者編號 Claim（user_number_id/sub/uid/NameIdentifier）
			int? userNumberId = TryGetUserNumberIdFromClaims(User);

			var (dto, rec) = await _svc.GetArticleDetailWithRecommendedAsync(id, userNumberId);
			if (dto == null) return NotFound();

			// 方便前端判斷是否顯示全文 / 遮罩
			var canViewFullContent = !dto.IsPaidContent || dto.HasPurchased;

			return Ok(new
			{
				canViewFullContent,
				data = dto,
				recommended = rec
			});
		}

		// ===== Helpers =====
		private static int? TryGetUserNumberIdFromClaims(ClaimsPrincipal user)
		{
			var claim = user.FindFirst("user_number_id")
					 ?? user.FindFirst("sub")
					 ?? user.FindFirst("uid")
					 ?? user.FindFirst(ClaimTypes.NameIdentifier);

			return (claim != null && int.TryParse(claim.Value, out var id)) ? id : (int?)null;
		}
	}
}
