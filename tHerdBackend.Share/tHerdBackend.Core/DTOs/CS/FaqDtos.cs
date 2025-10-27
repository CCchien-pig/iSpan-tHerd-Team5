namespace tHerdBackend.Core.DTOs.CS
{
	public sealed class FaqBriefDto
	{
		public int FaqId { get; set; }
		public string Title { get; set; } = "";
		public string AnswerHtml { get; set; } = "";
	}

	public sealed class CategoryWithFaqsDto
	{
		public int CategoryId { get; set; }
		public string CategoryName { get; set; } = "";
		public List<FaqBriefDto> Faqs { get; set; } = new();
	}

	public sealed class FaqSearchDto
	{
		public int FaqId { get; set; }
		public string Title { get; set; } = "";
		public string AnswerHtml { get; set; } = "";
		public int CategoryId { get; set; }
		public string CategoryName { get; set; } = "";
	}

	public sealed class FaqFeedbackIn
	{
		public int FaqId { get; set; }
		public bool IsHelpful { get; set; }
		public int? UserId { get; set; }
		public string? ClientSessionKey { get; set; }
	}
}
