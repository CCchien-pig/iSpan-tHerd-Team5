using tHerdBackend.Core.DTOs.CS;

namespace tHerdBackend.Core.Interfaces.CS
{
	public interface IFaqService
	{
		Task<List<CategoryWithFaqsDto>> GetListAsync();
		Task<List<FaqSearchDto>> SearchAsync(string keyword);
		Task AddFeedbackAsync(FaqFeedbackIn input);
	}
}
