using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.ValueObjects;
using tHerdBackend.Services.CS;

namespace tHerdBackend.SharedApi.Controllers.CS
{
	/// <summary>
	/// 客服工單 API
	/// </summary>
	[ApiController]
	[Route("api/cs/tickets")]
	public class CsTicketsController : ControllerBase
	{
		private readonly CsTicketService _service;

		public CsTicketsController(CsTicketService service)
		{
			_service = service;
		}

		/// <summary>
		/// 取得所有工單（後台或測試用）
		/// </summary>
		[HttpGet("list")]
		[Authorize(Roles = "Admin,CS")]
		public async Task<IActionResult> GetTickets()
		{
			try
			{
				var data = await _service.GetTicketsAsync();
				return Ok(ApiResponse<IEnumerable<TicketsDto>>.Ok(data));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse<string>.Fail(ex.Message));
			}
		}
	}
}
