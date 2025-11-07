namespace tHerdBackend.Core.DTOs.Chat
{
	public class ChatInput
	{
		public string Message { get; set; } = "";
	}

	public class ChatResponse
	{
		public string Type { get; set; } = "text"; // text / product / faq / ticket
		public string? Content { get; set; }
		public string? Title { get; set; }
		public string? Image { get; set; }
		public string? Link { get; set; }
	}
}
