using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Share.DTOs.CNT;

namespace tHerdBackend.Core.Interfaces.CNT
{
	/// <summary>
	/// CNT 前台查詢用 Repository（跨專案資料存取層）
	/// 僅負責「資料取得與組裝」，不承擔商業規則。
	/// </summary>
	public interface ICntQueryRepository
	{
		/// <summary>
		/// 取得所有文章分類與文章數量
		/// </summary>
		Task<IEnumerable<ArticleCategoryDto>> GetArticleCategoriesAsync(CancellationToken ct = default);


		/// <summary>
		/// 文章清單（支援分類、關鍵字、分頁）
		/// </summary>
		Task<(IReadOnlyList<ArticleListDto> Items, int Total)> GetArticleListAsync(
			int? pageTypeId, string? keyword, int page, int pageSize, CancellationToken ct = default);

		/// <summary>
		/// 文章詳細（含 SEO 與 Tags；Blocks 視付費狀態決定是否撈取）
		/// </summary>
		Task<ArticleDetailDto?> GetArticleDetailAsync(
			int pageId, int? userNumberId, CancellationToken ct = default);
	}
}
