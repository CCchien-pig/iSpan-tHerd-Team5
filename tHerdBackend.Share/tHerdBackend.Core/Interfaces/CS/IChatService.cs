using tHerdBackend.Core.DTOs.Chat;

namespace tHerdBackend.Core.Interfaces.CS
{
	public interface IChatService
	{
		Task<ChatResponse> GetSmartReplyAsync(string message);
	}
}
