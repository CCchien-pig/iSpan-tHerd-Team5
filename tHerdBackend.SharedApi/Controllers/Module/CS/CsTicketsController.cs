using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Core.ValueObjects;

[ApiController]
[AllowAnonymous] 
[Route("api/cs/[controller]")]
public class CsTicketsController : ControllerBase
{
	private readonly ICsTicketService _service;
	public CsTicketsController(ICsTicketService service) => _service = service;

	/// <summary>取得全部客服工單清單（限後台登入者）</summary>
	[HttpGet("list")]
	[Authorize(Roles = "Admin,CustomerService")] // 👈 只有後台能看
	public async Task<IActionResult> GetAllAsync()
	{
		try
		{
			var data = await _service.GetAllAsync();
			return Ok(ApiResponse<IEnumerable<TicketsDto>>.Ok(data));
		}
		catch (Exception ex)
		{
			return BadRequest(ApiResponse<string>.Fail(ex.Message));
		}
	}

	/// <summary>建立新客服工單（前台客戶可匿名）</summary>
	[HttpPost("create")]
	[AllowAnonymous] // 👈 讓前台客戶能開單
	public async Task<IActionResult> CreateAsync([FromBody] TicketIn dto)
	{
		try
		{
			var id = await _service.CreateAsync(dto);
			var data = await _service.GetTicketByIdAsync(id);
			return Ok(ApiResponse<object>.Ok(data, "建立成功"));
		}
		catch (Exception ex)
		{
			return BadRequest(ApiResponse<string>.Fail(ex.Message));
		}
	}
}
