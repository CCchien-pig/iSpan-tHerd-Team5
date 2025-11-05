using tHerdBackend.Core.DTOs.CS;

namespace tHerdBackend.Core.Interfaces.CS
{
	public interface IFaqRepository
	{
		Task<List<CategoryWithFaqsDto>> GetListAsync();
		Task<List<FaqSearchDto>> SearchAsync(string keyword);
		Task<int> AddFeedbackAsync(FaqFeedbackIn input);
        Task<IEnumerable<FaqSuggestDto>> SuggestAsync(string q, int limit);
        Task<FaqDetailDto?> GetDetailAsync(int id);
		// 給工單使用的 FAQ 分類清單
		Task<IEnumerable<FaqCategoryDto>> GetActiveCategoriesAsync();
	}
}
