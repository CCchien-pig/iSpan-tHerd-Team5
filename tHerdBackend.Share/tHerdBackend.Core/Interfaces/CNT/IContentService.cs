using System.Collections.Generic;
using System.Threading.Tasks;
using tHerdBackend.Share.DTOs.CNT;

namespace tHerdBackend.Core.Interfaces.CNT
{
	/// <summary>
	/// CNT 內容服務介面：提供文章清單、單篇內容（含付費判斷）
	/// </summary>
	public interface IContentService
	{
		/// <summary>
		/// 取得文章清單（支援分類、關鍵字、分頁）
		/// </summary>
		Task<(IReadOnlyList<ArticleListDto> Items, int Total)> GetArticleListAsync(
			int? pageTypeId, string? keyword, int page, int pageSize);

		/// <summary>
		/// 取得文章詳細（含 SEO、Blocks、Tags、付費預覽判斷）
		/// </summary>
		/// <param name="id">文章 PageId</param>
		/// <param name="userNumberId">登入會員編號（JWT 解析；訪客可為 null）</param>
		Task<ArticleDetailDto?> GetArticleDetailAsync(int id, int? userNumberId);

		/// <summary>
		/// 取得文章詳細＋同分類推薦清單（一次回傳，給前端直接渲染）
		/// </summary>
		/// <param name="id">文章 PageId</param>
		/// <param name="userNumberId">登入會員編號（JWT 解析；訪客可為 null）</param>
		Task<(ArticleDetailDto? Data, IReadOnlyList<ArticleListDto> Recommended)>
			GetArticleDetailWithRecommendedAsync(int id, int? userNumberId);
	}
}
