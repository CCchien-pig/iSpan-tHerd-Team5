using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Models;
using System.Linq;
using tHerdBackend.Services.CNT;

namespace tHerdBackend.SharedApi.Controllers.Module.CNT
{
	/// <summary>
	/// CNT 前台內容 API（RESTful）
	/// - GET /api/cnt/list                 文章清單（含分類 / 搜尋 / 分頁）
	/// - GET /api/cnt/articles/{id}        單篇文章 + 推薦文章
	/// </summary>
	[ApiController]
	[Route("api/cnt")]
	public class ContentController : ControllerBase
	{
		private readonly IContentService _svc;
		private readonly tHerdDBContext _db;   // ★ 新增
		public ContentController(IContentService svc, tHerdDBContext db)
		{
			_svc = svc;
			_db = db;
		}

		/// <summary>
		/// 取得所有文章分類（含每個分類的文章數量）
		/// </summary>
		/// <remarks>
		/// 用於前台 Sidebar 與文章列表上方分類區塊。  
		/// 資料來源：CNT_PageType + CNT_Page  
		/// 
		/// **範例：**
		/// GET `/api/cnt/categories`
		/// 
		/// **回傳範例：**
		/// ```json
		/// {
		///   "items": [
		///     { "id": 1, "name": "健康飲食", "articleCount": 12 },
		///     { "id": 2, "name": "運動營養", "articleCount": 8 },
		///     { "id": 3, "name": "保健知識", "articleCount": 5 }
		///   ]
		/// }
		/// ```
		/// </remarks>
		[HttpGet("categories")]
		public async Task<IActionResult> GetCategories()
		{
			var list = await _svc.GetCategoriesAsync();
			return Ok(new { items = list });
		}


		/// <summary>
		/// 文章清單（支援分類、關鍵字搜尋與分頁）
		/// </summary>
		/// <remarks>
		/// 用於前台文章列表頁顯示。  
		/// 可根據分類、關鍵字查詢，並支援分頁。
		/// 
		/// **範例：**
		/// GET `/api/cnt/list?categoryId=1&q=魚油&page=1&pageSize=12`
		/// 
		/// **回傳欄位：**
		/// - `PageId`: 文章ID  
		/// - `Title`: 標題  
		/// - `Slug`: SEO 友善網址  
		/// - `Excerpt`: 文章摘要  
		/// - `CoverImage`: 封面圖路徑  
		/// - `CategoryName`: 所屬分類名稱  
		/// - `PublishedDate`: 發佈日期  
		/// - `IsPaidContent`: 是否為付費文章  
		/// - `Tags`: 標籤集合
		/// </remarks>
		[HttpGet("list")]
		public async Task<IActionResult> GetList(
			[FromQuery] int? categoryId,
			[FromQuery] string? q,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 12)
		{
			var (items, total) = await _svc.GetArticleListAsync(categoryId, q, page, pageSize);
			return Ok(new
			{
				items,
				total,
				page,
				pageSize
			});
		}

		/// <summary>
		/// 單篇文章詳情 + 同分類推薦
		/// 範例：/api/cnt/articles/1007
		/// </summary>
		[HttpGet("articles/{id:int}")]
		public async Task<IActionResult> GetArticle([FromRoute] int id)
		{
			// 取得使用者會員編號（JWT Token 解析，未登入則為 null）
			int? userNumberId = TryGetUserNumberIdFromClaims(User);

			// 呼叫 Service（含推薦文章）
			var (dto, rec) = await _svc.GetArticleDetailWithRecommendedAsync(id, userNumberId);
			if (dto == null)
				return NotFound();

			// canViewFullContent = 前端用於決定是否顯示全文 or 局部預覽
			var canViewFullContent = !dto.IsPaidContent || dto.HasPurchased;

			return Ok(new
			{
				canViewFullContent,
				data = dto,
				recommended = rec
			});
		}

		// ===== Helper Method =====

		/// <summary>
		/// 嘗試從 JWT Token Claims 取得使用者編號（UserNumberId）
		/// 若找不到則回傳 null（代表訪客或未登入）
		/// </summary>
		// 嘗試從 JWT 取得 UserNumberId，失敗就回傳 null（代表訪客）
		private int? TryGetUserNumberIdFromClaims(ClaimsPrincipal user)
		{
			// 1) 如果 token 裡本來就有 user_number_id，就直接用
			var numClaim = user.FindFirst("user_number_id");
			if (numClaim != null && int.TryParse(numClaim.Value, out var numId))
			{
				return numId;
			}

			// 2) 否則用 Identity 的 NameIdentifier 找 AspNetUsers.UserNumberId
			var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				// 沒登入 / 沒 claim → 訪客
				return null;
			}

			var userEntity = _db.AspNetUsers.FirstOrDefault(u => u.Id == userId);
			return userEntity?.UserNumberId; // 找不到就 null
		}
	}
}


