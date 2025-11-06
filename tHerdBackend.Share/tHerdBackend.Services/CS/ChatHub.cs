using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace tHerdBackend.Services.CS
{
	public class ChatHub : Hub
	{
		// 當前端要傳訊息給同一群組（聊天室）
		public async Task SendMessage(string chatId, string sender, string message)
		{
			await Clients.Group(chatId).SendAsync("ReceiveMessage", sender, message);
		}

		// 使用者加入聊天室群組
		public async Task JoinChat(string chatId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
			await Clients.Group(chatId).SendAsync("ReceiveMessage", "system", "使用者已加入聊天室");
		}
	}
}
