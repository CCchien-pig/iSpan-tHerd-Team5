using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Models;             // for tHerdDBContext & CNT_Page entity
using tHerdBackend.Infra.Repository.CNT;     // for CntQueryRepository (cast for recommended)
using tHerdBackend.Share.DTOs.CNT;


namespace tHerdBackend.Services.CNT
{
	/// <summary>
	/// CNT 內容服務：薄服務 + 商業邏輯整合點
	/// - 維持既有方法（清單 / 詳細）
	/// - 新增整合方法：詳細 + 推薦，一次回傳給前端
	/// </summary>
	public class ContentService : IContentService
	{
		private readonly ICntQueryRepository _repo;
		private readonly tHerdDBContext _db;

		public ContentService(ICntQueryRepository repo, tHerdDBContext db)
		{
			_repo = repo;
			_db = db;
		}

		/// <summary>
		/// 文章清單（支援分類、關鍵字、分頁）
		/// </summary>
		public Task<(IReadOnlyList<ArticleListDto> Items, int Total)> GetArticleListAsync(
			int? pageTypeId, string? keyword, int page, int pageSize)
			=> _repo.GetArticleListAsync(pageTypeId, keyword, page, pageSize);

		/// <summary>
		/// 文章詳細（含 SEO、Blocks、Tags、付費預覽判斷）
		/// </summary>
		public Task<ArticleDetailDto?> GetArticleDetailAsync(int id, int? userNumberId)
			=> _repo.GetArticleDetailAsync(id, userNumberId);

		/// <summary>
		/// 一次取得「文章詳情 + 同分類推薦清單」
		/// 回傳格式可直接對應前端需求：
		/// { canViewFullContent, data, recommended }
		/// </summary>
		public async Task<(ArticleDetailDto? Data, IReadOnlyList<ArticleListDto> Recommended)>
			GetArticleDetailWithRecommendedAsync(int id, int? userNumberId)
		{
			// 1) 取文章詳情（含付費判斷 / SEO / Tags / Blocks[視權限]）
			var dto = await _repo.GetArticleDetailAsync(id, userNumberId);
			if (dto == null)
				return (null, Array.Empty<ArticleListDto>());

			// 2) 取得該文章的 PageTypeId（用於同分類推薦）
			//    - 使用 EF Core 從 DB 讀（不額外改 DTO）
			var pageTypeId = await _db.Set<CntPage>()
				.Where(p => p.PageId == id)
				.Select(p => p.PageTypeId)
				.FirstOrDefaultAsync();

			// 若查無 PageTypeId（理論上不會），則不給推薦清單
			if (pageTypeId <= 0)
				return (dto, Array.Empty<ArticleListDto>());

			// 3) 取得推薦清單（同分類，排除自己；最新發佈優先）
			//    - 直接呼叫 Repository 的公用方法（在 CntQueryRepository 中實作）
			//    - 以 cast 方式取得（避免改動 ICntQueryRepository 介面）
			var concreteRepo = _repo as CntQueryRepository;
			IReadOnlyList<ArticleListDto> recommended;

			if (concreteRepo != null)
			{
				recommended = await concreteRepo.GetRecommendedByCategoryAsync(
					currentPageId: id,
					pageTypeId: pageTypeId,
					topN: 6,
					ct: default);
			}
			else
			{
				// 理論上不會走到這裡；保險起見給空清單
				recommended = Array.Empty<ArticleListDto>();
			}

			return (dto, recommended);
		}
	}
}
