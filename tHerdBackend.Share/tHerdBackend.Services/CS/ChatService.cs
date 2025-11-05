using System.Text.RegularExpressions;
using tHerdBackend.Core.DTOs.Chat;
using tHerdBackend.Core.Interfaces.CS;

namespace tHerdBackend.Services.CS
{
	/// <summary>AI + FAQ 智慧客服邏輯（依據 CS_Faq 資料表）</summary>
	public class ChatService : IChatService
	{
		private readonly IFaqRepository _faqRepo;

		public ChatService(IFaqRepository faqRepo)
		{
			_faqRepo = faqRepo;
		}

		public async Task<ChatResponse> GetSmartReplyAsync(string msg)
		{
			msg = msg.Trim();

			// 🔹 1️⃣ 嘗試從 FAQ 找出關鍵字匹配
			var faq = await _faqRepo.SearchByKeywordAsync(msg);
			if (faq != null)
			{
				return new ChatResponse
				{
					Type = "faq",
					Title = faq.Title,
					Content = faq.AnswerHtml
				};
			}

			// 🔹 2️⃣ 若包含「人工客服」
			if (Regex.IsMatch(msg, "(人工|真人|聯絡客服)"))
			{
				return new ChatResponse
				{
					Type = "ticket",
					Content = "我已將您的需求轉交客服人員，請點擊下方前往聯絡我們。",
					Link = "/cs/ticket"
				};
			}

			// 🔹 3️⃣ 預設回覆
			return new ChatResponse
			{
				Type = "text",
				Content = "感謝您的提問，目前我無法找到相關資訊，您可以試著輸入其他關鍵字或前往客服表單。"
			};
		}
	}
}
