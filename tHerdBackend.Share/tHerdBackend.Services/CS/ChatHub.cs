using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace tHerdBackend.SharedApi.Hubs
{
	public class ChatHub : Hub
	{
		// 當使用者傳送訊息
		public async Task SendMessage(string chatId, string sender, string message)
		{
			// 傳給同一聊天室群組內的所有人
			await Clients.Group(chatId).SendAsync("ReceiveMessage", sender, message);
		}

		// 加入聊天室群組
		public async Task JoinChat(string chatId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
			await Clients.Group(chatId).SendAsync("ReceiveMessage", "system", $"{chatId} 已加入聊天室");
		}
	}
}
