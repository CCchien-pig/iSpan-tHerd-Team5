using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.CS
{
	[ApiController]
	[Route("api/cs/[controller]")]
	public sealed class TicketsController : ControllerBase
	{
		private readonly ITicketService _svc;
		public TicketsController(ITicketService svc) => _svc = svc;

		/// <summary>建立客服工單（匿名可送）</summary>
		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Create([FromBody] TicketIn dto)
		{
			var result = await _svc.CreateAsync(dto);
			return Ok(ApiResponse<TicketOut>.Ok(result, "建立成功"));
		}
	}

}
