using tHerdBackend.Core.DTOs.CS;

namespace tHerdBackend.Core.Interfaces.CS
{
	/// <summary>
	/// FAQ 服務層介面（供 Controller 呼叫）
	/// </summary>
	public interface IFaqService
	{
		Task<List<CategoryWithFaqsDto>> GetListAsync();
		Task<List<FaqSearchDto>> SearchAsync(string keyword);
		Task AddFeedbackAsync(FaqFeedbackIn input);

		// ⬇新增：給前端自動完成用的建議
		Task<IEnumerable<FaqSuggestDto>> SuggestAsync(string q, int limit);
		// 新增：單筆明細
		Task<FaqDetailDto?> GetDetailAsync(int id);

		// 給工單模組使用的 FAQ 分類下拉清單
		Task<IEnumerable<FaqCategoryDto>> GetActiveCategoriesAsync();
	}
}
