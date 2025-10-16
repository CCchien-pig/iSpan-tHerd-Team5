using System.Collections.Generic;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Share.DTOs.CNT;

namespace tHerdBackend.Services.CNT
{
	/// <summary>
	/// CNT 內容服務：薄服務，委派給 Repository，保留商規擴充點
	/// </summary>
	public class ContentService : IContentService
	{
		private readonly ICntQueryRepository _repo;

		public ContentService(ICntQueryRepository repo)
		{
			_repo = repo;
		}

		public Task<(IReadOnlyList<ArticleListDto> Items, int Total)> GetArticleListAsync(
			int? pageTypeId, string? keyword, int page, int pageSize)
			=> _repo.GetArticleListAsync(pageTypeId, keyword, page, pageSize);

		public Task<ArticleDetailDto?> GetArticleDetailAsync(int pageId, int? userNumberId)
			=> _repo.GetArticleDetailAsync(pageId, userNumberId);
	}
}
