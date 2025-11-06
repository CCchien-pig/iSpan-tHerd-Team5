using System.Text.RegularExpressions;
using tHerdBackend.Core.DTOs.Chat;
using tHerdBackend.Core.Interfaces.CS;

namespace tHerdBackend.Services.CS
{
	/// <summary>
	/// 智慧客服邏輯（加強版：清除語助詞 + 多關鍵字搜尋）
	/// </summary>
	public class ChatService : IChatService
	{
		private readonly IFaqRepository _faqRepo;

		public ChatService(IFaqRepository faqRepo)
		{
			_faqRepo = faqRepo;
		}

		public async Task<ChatResponse> GetSmartReplyAsync(string msg)
		{
			if (string.IsNullOrWhiteSpace(msg))
			{
				return new ChatResponse
				{
					Type = "text",
					Content = "請輸入您想詢問的問題喔 😊"
				};
			}

			// ✅ Step 1. 預處理輸入（清除語助詞、符號）
			msg = msg.Trim();
			msg = Regex.Replace(msg, "[呢嗎啊呀的喔哦～!！,.。？?]", ""); // 移除語助詞與標點
			msg = Regex.Replace(msg, @"\s+", " "); // 移除多餘空白

			// ✅ Step 2. 切割多關鍵字（空白或常見連接詞）
			var keywords = msg.Split(new[] { " ", "、", "和", "與", "還有", "想", "請問", "我想" },
									 StringSplitOptions.RemoveEmptyEntries);
			// ✅ Step 3. 用每個關鍵字依序查詢（命中即回傳）
			foreach (var kw in keywords)
			{
				var faq = await _faqRepo.SearchByKeywordAsync(kw);
				if (faq != null)
				{
					return new ChatResponse
					{
						Type = "faq",
						Title = faq.Title,
						Content = faq.AnswerHtml
					};
				}
			}

			// ✅ Step 4. 若包含人工關鍵詞 → 引導轉客服
			if (Regex.IsMatch(msg, "(人工|真人|聯絡客服)"))
			{
				return new ChatResponse
				{
					Type = "ticket",
					Content = "我已將您的需求轉交客服人員，請點擊下方前往聯絡我們。",
					Link = "/cs/ticket"
				};
			}

			// ✅ Step 5. 若都找不到 → 提示使用者
			return new ChatResponse
			{
				Type = "text",
				Content = "目前找不到相關的常見問題 😢 您可以試著換個說法，或直接聯絡客服人員喔。"
			};
		}
	}
}
