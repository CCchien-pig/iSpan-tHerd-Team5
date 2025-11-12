using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Claims;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;
using tHerdBackend.Services.Common;

//namespace tHerdBackend.SharedApi.Hubs
//{
//	//[Authorize] // ✅ 僅允許登入會員或管理員使用
//	public class ChatHub : Hub
//	{
//		private static readonly ConcurrentDictionary<string, ChatUserInfo> _connections = new();
//		private readonly UserManager<ApplicationUser> _userMgr;
//		private readonly ApplicationDbContext _appDb;

//		public ChatHub(UserManager<ApplicationUser> userMgr, ApplicationDbContext appDb)
//		{
//			_userMgr = userMgr;
//			_appDb = appDb;
//		}

//		// ✅ 儲存使用者連線資訊（前台會員 / 後台客服皆可）
//		public class ChatUserInfo
//		{
//			public string ConnectionId { get; set; } = "";
//			public string UserId { get; set; } = "";
//			public int UserNumberId { get; set; }
//			public string UserName { get; set; } = "";
//			public string Email { get; set; } = "";
//			public string FullName { get; set; } = "";
//			public bool IsAdmin { get; set; } = false;
//		}

//		public override async Task OnConnectedAsync()
//		{
//			try
//			{
//				// 嘗試從 Claims 抓出各種來源的使用者資訊
//				var userName =
//	Context.User?.FindFirst("name")?.Value ??
//	Context.User?.FindFirst("email")?.Value ??
//	Context.User?.FindFirst("sub")?.Value ??
//	"未登入使用者";

//				var isAdmin = Context.User?.IsInRole("SuperAdmin") ?? false;

//				// 若完全取不到 → 表示這次連線沒有帶 token / cookie
//				if (string.IsNullOrEmpty(userName))
//				{
//					await Clients.Caller.SendAsync("ReceiveMessage", "系統", "⚠️ 尚未登入，因此不會顯示使用者名稱");
//					userName = "未登入使用者";
//				}

//				var roleLabel = isAdmin ? "(後台客服)" : "(前台會員)";
//				Console.WriteLine($"👤 {userName} {roleLabel} connected ({Context.ConnectionId})");

//				await Clients.Caller.SendAsync("ReceiveMessage", "系統", $"{userName} {roleLabel} 已連線");
//				await base.OnConnectedAsync();
//			}
//			catch (Exception ex)
//			{
//				Console.WriteLine($"❌ OnConnected Error: {ex.Message}");
//				await Clients.Caller.SendAsync("ReceiveMessage", "系統", "連線時發生錯誤。");
//			}
//		}

//		public override async Task OnDisconnectedAsync(Exception? exception)
//		{
//			if (_connections.TryRemove(Context.ConnectionId, out var user))
//			{
//				var roleLabel = user.IsAdmin ? "(後台客服)" : "(前台會員)";
//				await Clients.All.SendAsync("ReceiveMessage", "系統", $"{user.FullName} {roleLabel} 已離線");
//				Console.WriteLine($"⚠️ {user.FullName} disconnected ({Context.ConnectionId})");
//			}

//			await base.OnDisconnectedAsync(exception);
//		}

//		public async Task JoinChat(string chatId)
//		{
//			await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
//			if (_connections.TryGetValue(Context.ConnectionId, out var user))
//			{
//				var roleLabel = user.IsAdmin ? "(後台客服)" : "(前台會員)";
//				await Clients.Group(chatId).SendAsync("ReceiveMessage", "系統", $"{user.FullName} {roleLabel} 已加入聊天室");
//			}
//		}

//		public async Task SendMessage(string chatId, string message)
//		{
//			try
//			{
//				if (!_connections.TryGetValue(Context.ConnectionId, out var user))
//				{
//					var userName = Context.User?.Identity?.Name ?? "匿名";
//					await Clients.Group(chatId).SendAsync("ReceiveMessage", userName, message);
//					return;
//				}

//				var roleLabel = user.IsAdmin ? "(後台客服)" : "";
//				await Clients.Group(chatId).SendAsync("ReceiveMessage", $"{user.FullName}{roleLabel}", message);

//				Console.WriteLine($"💬 [{chatId}] {user.FullName}{roleLabel}: {message}");
//			}
//			catch (Exception ex)
//			{
//				Console.WriteLine($"❌ SendMessage Error: {ex.Message}");
//				throw; // 給前端捕捉 "server error"
//			}
//		}
//	}
//}

namespace tHerdBackend.SharedApi.Hubs
{
	public class ChatHub : Hub
	{
		private static readonly ConcurrentDictionary<string, ChatUserInfo> _connections = new();

		public class ChatUserInfo
		{
			public string ConnectionId { get; set; } = "";
			public string UserName { get; set; } = "";
			public bool IsAdmin { get; set; } = false;
		}

		// ✅ 統一的取名方法（from=admin → 直接寫死「超級管理員」）
		private (string userName, bool isAdmin) ResolveUserInfo(HubCallerContext ctx)
		{
			var http = ctx.GetHttpContext();
			var from = http?.Request.Query["from"].ToString();

			// 後台：明確宣告 from=admin，就固定當作超級管理員
			if (!string.IsNullOrEmpty(from) && from.Equals("admin", StringComparison.OrdinalIgnoreCase))
			{
				return ("超級管理員", true);
			}

			// 前台：從 JWT claims 拿 name/email/sub
			var userName =
				ctx.User?.FindFirst("name")?.Value ??
				ctx.User?.FindFirst("UserName")?.Value ??
				ctx.User?.FindFirst("email")?.Value ??
				ctx.User?.FindFirst("sub")?.Value ??
				"未登入使用者";

			// 不再做角色判斷（固定視為前台）
			var isAdmin = false;

			return (userName, isAdmin);
		}

		private static MeProfileVm GetProfileFromClaims(HubCallerContext ctx)
		{
			var claims = ctx.User;
			return new MeProfileVm
			{
				Id = claims?.FindFirst("sub")?.Value ?? "",
				UserNumberId = int.TryParse(claims?.FindFirst("user_number_id")?.Value, out var n) ? n : 0,
				FirstName = claims?.FindFirst("name")?.Value ?? "",
				Email = claims?.FindFirst("email")?.Value ?? "",
				IsActive = true
			};
		}

		public override async Task OnConnectedAsync()
		{
			try
			{
				var (userName, isAdmin) = ResolveUserInfo(Context);

				_connections[Context.ConnectionId] = new ChatUserInfo
				{
					ConnectionId = Context.ConnectionId,
					UserName = userName,
					IsAdmin = isAdmin
				};

				var roleLabel = isAdmin ? "(後台客服)" : "(前台會員)";
				Console.WriteLine($"👤 {userName} {roleLabel} connected ({Context.ConnectionId})");

				await Clients.Caller.SendAsync("ReceiveMessage", "系統", $"{userName} {roleLabel} 已連線");
				await base.OnConnectedAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ OnConnected Error: {ex.Message}");
				await Clients.Caller.SendAsync("ReceiveMessage", "系統", "連線時發生錯誤。");
			}
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			try
			{
				if (_connections.TryRemove(Context.ConnectionId, out var user))
				{
					var roleLabel = user.IsAdmin ? "(後台客服)" : "(前台會員)";
					await Clients.All.SendAsync("ReceiveMessage", "系統", $"{user.UserName} {roleLabel} 已離線");
					Console.WriteLine($"⚠️ {user.UserName} disconnected ({Context.ConnectionId})");
				}
				else
				{
					// 沒有暫存就即時解析（避免漏刪）
					var (userName, isAdmin) = ResolveUserInfo(Context);
					var roleLabel = isAdmin ? "(後台客服)" : "(前台會員)";
					await Clients.All.SendAsync("ReceiveMessage", "系統", $"{userName} {roleLabel} 已離線");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ OnDisconnected Error: {ex.Message}");
			}

			await base.OnDisconnectedAsync(exception);
		}

		public async Task JoinChat(string chatId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, chatId);

			var (userName, isAdmin) = ResolveUserInfo(Context);
			var roleLabel = isAdmin ? "(後台客服)" : "(前台會員)";

			await Clients.Group(chatId).SendAsync("ReceiveMessage", "系統", $"{userName} {roleLabel} 已加入聊天室");
			Console.WriteLine($"👥 [{chatId}] {userName} {roleLabel} joined");
		}

		public async Task SendMessage(string chatId, string message)
		{
			try
			{
				var (userName, isAdmin) = ResolveUserInfo(Context);
				var roleLabel = isAdmin ? "(後台客服)" : "";

				await Clients.Group(chatId).SendAsync("ReceiveMessage", $"{userName}{roleLabel}", message);
				Console.WriteLine($"💬 [{chatId}] {userName}{roleLabel}: {message}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ SendMessage Error: {ex.Message}");
				await Clients.Caller.SendAsync("ReceiveMessage", "系統", "訊息傳送失敗。");
			}
		}
	}
}
