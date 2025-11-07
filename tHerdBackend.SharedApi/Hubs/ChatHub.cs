using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace tHerdBackend.SharedApi.Hubs
{
	[Authorize] // ✅ 只有登入會員可使用 SignalR
	public class ChatHub : Hub
	{
		private static readonly ConcurrentDictionary<string, string> _connections = new();

		public override async Task OnConnectedAsync()
		{
			// ✅ 從 JWT Claims 取會員名稱
			var userName = Context.User?.Identity?.Name
						   ?? Context.User?.FindFirst("UserName")?.Value
						   ?? "匿名";

			_connections[Context.ConnectionId] = userName;

			await Clients.Caller.SendAsync("ReceiveMessage", "系統", $"{userName} 已連線");
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			if (_connections.TryRemove(Context.ConnectionId, out var user))
				await Clients.All.SendAsync("ReceiveMessage", "系統", $"{user} 已離線");

			await base.OnDisconnectedAsync(exception);
		}

		public async Task JoinChat(string chatId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
			var user = _connections[Context.ConnectionId];
			await Clients.Group(chatId).SendAsync("ReceiveMessage", "系統", $"{user} 已加入聊天室");
		}

        public async Task SendMessage(string chatId, string message)
        {
            try
            {
                var user = _connections.ContainsKey(Context.ConnectionId)
                    ? _connections[Context.ConnectionId]
                    : Context.User?.Identity?.Name ?? "匿名";

                await Clients.Group(chatId).SendAsync("ReceiveMessage", user, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SendMessage Error: {ex.Message}");
                throw; // 保留給前端顯示「server error」
            }
        }


    }
}
