using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Repository.CNT;   // 為了使用推薦功能的 Repository 方法
using tHerdBackend.Share.DTOs.CNT;

namespace tHerdBackend.Services.CNT
{
	/// <summary>
	/// CNT 內容服務（前台用）
	/// 採用純 Repository 模式，不直接接觸 DbContext
	/// - 提供文章清單、文章詳情
	/// - 支援付費判斷與推薦文章
	/// </summary>
	public class ContentService : IContentService
	{
		private readonly ICntQueryRepository _repo;

		public ContentService(ICntQueryRepository repo)
		{
			_repo = repo;
		}

		/// <summary>
		/// 取得文章清單
		/// </summary>
		public Task<(IReadOnlyList<ArticleListDto> Items, int Total)> GetArticleListAsync(
			int? pageTypeId, string? keyword, int page, int pageSize)
			=> _repo.GetArticleListAsync(pageTypeId, keyword, page, pageSize);

		/// <summary>
		/// 取得單篇文章（含 Blocks、付費判斷、SEO）
		/// </summary>
		public Task<ArticleDetailDto?> GetArticleDetailAsync(int id, int? userNumberId)
			=> _repo.GetArticleDetailAsync(id, userNumberId);

		/// <summary>
		/// 取得文章詳細 + 同分類推薦
		/// 使用 ArticleDetailDto.PageTypeId 取得推薦（不再查 DB）
		/// </summary>
		public async Task<(ArticleDetailDto? Data, IReadOnlyList<ArticleListDto> Recommended)>
			GetArticleDetailWithRecommendedAsync(int id, int? userNumberId)
		{
			// 1) 取得文章詳細資料（含 SEO / Blocks / 付費）
			var dto = await _repo.GetArticleDetailAsync(id, userNumberId);
			if (dto == null)
				return (null, Array.Empty<ArticleListDto>());

			// 2) 預設：沒有推薦文章
			IReadOnlyList<ArticleListDto> recommended = Array.Empty<ArticleListDto>();

			// 3) 若有分類 PageTypeId，則取得同分類推薦文章
			//    注意：必須從 Repository Cast 成具體類別 CntQueryRepository
			var concreteRepo = _repo as CntQueryRepository;
			if (concreteRepo != null && dto.PageTypeId > 0)
			{
				recommended = await concreteRepo.GetRecommendedByCategoryAsync(
					currentPageId: id,
					pageTypeId: dto.PageTypeId,
					topN: 6
				);
			}

			// 4) 回傳 (詳細內容, 推薦清單)
			return (dto, recommended);
		}
	}
}
