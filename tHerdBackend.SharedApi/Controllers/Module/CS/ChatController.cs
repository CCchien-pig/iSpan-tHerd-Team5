using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.Chat;
using tHerdBackend.Core.Interfaces.CS;

namespace tHerdBackend.SharedApi.Controllers.CS
{
	[ApiController]
	[Route("api/cs/[controller]")]
	public class ChatController : ControllerBase
	{
		private readonly IChatService _chat;

		public ChatController(IChatService chat)
		{
			_chat = chat;
		}

		[HttpPost("ask")]
		public async Task<IActionResult> Ask([FromBody] ChatInput dto)
		{
			var result = await _chat.GetSmartReplyAsync(dto.Message);
			return Ok(result);
		}
	}
}
