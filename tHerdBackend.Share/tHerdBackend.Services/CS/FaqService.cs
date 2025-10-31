using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.CS;

namespace tHerdBackend.Services.CS
{
	public sealed class FaqService : IFaqService
	{
		private readonly IFaqRepository _repo;
		public FaqService(IFaqRepository repo) => _repo = repo;

		public Task<List<CategoryWithFaqsDto>> GetListAsync()
			=> _repo.GetListAsync();

		public Task<List<FaqSearchDto>> SearchAsync(string keyword)
		{
			keyword = keyword?.Trim() ?? "";
			if (string.IsNullOrWhiteSpace(keyword))
				return Task.FromResult(new List<FaqSearchDto>());
			return _repo.SearchAsync(keyword);
		}

		public async Task AddFeedbackAsync(FaqFeedbackIn input)
		{
			if (input is null) throw new ArgumentNullException(nameof(input));
			var affected = await _repo.AddFeedbackAsync(input);
			if (affected <= 0) throw new InvalidOperationException("回饋寫入失敗");
		}
        public Task<IEnumerable<FaqSuggestDto>> SuggestAsync(string q, int limit)
          => _repo.SuggestAsync(q, limit);
        public Task<FaqDetailDto?> GetDetailAsync(int id) => _repo.GetDetailAsync(id);
		//工單faqcategory下拉選單
		public async Task<IEnumerable<FaqCategoryDto>> GetActiveCategoriesAsync()
		{
			return await _repo.GetActiveCategoriesAsync();
		}

	}
}
