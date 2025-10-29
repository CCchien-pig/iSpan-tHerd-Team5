using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Core.ValueObjects;
using tHerdBackend.Services.CS;

namespace tHerdBackend.SharedApi.Controllers.Module.CS
{
	[ApiController]
	[Route("api/cs/[controller]")]
	public sealed class TicketsController : ControllerBase
	{
		private readonly CsTicketService _svc;
		public TicketsController(CsTicketService svc) => _svc = svc;

		/// <summary>建立客服工單（匿名可送）</summary>
		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Create([FromBody] TicketIn dto)
		{
			var result = await _svc.CreateAsync(dto);
			return Ok(ApiResponse<TicketOut>.Ok(result, "建立成功"));
		}
		[HttpPost("create")]
		[AllowAnonymous]
		public async Task<IActionResult> CreateTicket([FromBody] TicketIn dto)
		{
			try
			{
				var result = await _svc.CreateAsync(dto);
				return Ok(ApiResponse<TicketOut>.Ok(result, "工單已建立"));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse<string>.Fail(ex.Message));
			}
		}

	}

}
