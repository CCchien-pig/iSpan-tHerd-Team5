using tHerdBackend.Core.DTOs.CS;

namespace tHerdBackend.Core.Interfaces.CS
{
	public interface IFaqService
	{
		Task<List<CategoryWithFaqsDto>> GetListAsync();
		Task<List<FaqSearchDto>> SearchAsync(string keyword);
		Task AddFeedbackAsync(FaqFeedbackIn input);
       
		// ⬇新增：給前端自動完成用的建議
        Task<IEnumerable<FaqSuggestDto>> SuggestAsync(string q, int limit);
        // 新增：單筆明細
        Task<FaqDetailDto?> GetDetailAsync(int id);
    }
}
