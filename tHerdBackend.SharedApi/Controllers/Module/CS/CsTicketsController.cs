using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Core.ValueObjects;
using System.Security.Claims;


[ApiController]
[AllowAnonymous] 
[Route("api/cs/[controller]")]
public class CsTicketsController : ControllerBase
{
	private readonly ICsTicketService _service;
	private readonly IFaqService _faqService;
	public CsTicketsController(ICsTicketService service, IFaqService faqService)
	{
		_service = service;
		_faqService = faqService;
	}

    ///// <summary>取得全部客服工單清單（限後台登入者）</summary>
    //[HttpGet("list")]
    //[Authorize(Roles = "Admin,CustomerService")] // 👈 只有後台能看
    //public async Task<IActionResult> GetAllAsync()
    //{
    //	try
    //	{
    //		var data = await _service.GetAllAsync();
    //		return Ok(ApiResponse<IEnumerable<TicketsDto>>.Ok(data));
    //	}
    //	catch (Exception ex)
    //	{
    //		return BadRequest(ApiResponse<string>.Fail(ex.Message));
    //	}
    //}

    /// <summary>取得「我的工單」清單（前台會員使用）</summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyTickets()
    {
        try
        {
            // 從 Token 取出 user_number_id（你的 token payload 裡就有）
            var userNumClaim = User.Claims.FirstOrDefault(c => c.Type == "user_number_id");
            if (userNumClaim == null)
                return BadRequest(ApiResponse<string>.Fail("Token 無 user_number_id"));

            int userNumberId = int.Parse(userNumClaim.Value);

            // 呼叫 service 層
            var data = await _service.GetByUserIdAsync(userNumberId);

            return Ok(ApiResponse<IEnumerable<TicketsDto>>.Ok(data));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>查詢單筆工單（含分類名稱與留言）</summary>
    [HttpGet("{ticketId}")]
    [Authorize] // 必須登入
    public async Task<IActionResult> GetById(int ticketId)
    {
        var ticket = await _service.GetTicketByIdAsync(ticketId);
        if (ticket == null)
            return NotFound(ApiResponse<string>.Fail("找不到該工單"));

        return Ok(ApiResponse<TicketOut>.Ok(ticket));
    }




    /// <summary>建立新客服工單（前台客戶可匿名，上傳 1 張附件圖片）</summary>
    [HttpPost("create")]
	[AllowAnonymous]
	[RequestSizeLimit(10_000_000)] // 限制上傳大小（10MB）
	public async Task<IActionResult> CreateAsync([FromForm] TicketIn dto, IFormFile? image)
	{
		try
		{
			// ✅ 呼叫 Service，傳入工單資料與圖片
			var id = await _service.CreateAsync(dto, image);

			var data = await _service.GetTicketByIdAsync(id);
			return Ok(ApiResponse<object>.Ok(data, "建立成功"));
		}
		catch (Exception ex)
		{
			return BadRequest(ApiResponse<string>.Fail(ex.Message));
		}
	}

	/// <summary>取得 FAQ 問題分類（供工單下拉選單用）</summary>
	[HttpGet("categories")]
	[AllowAnonymous]
	public async Task<IActionResult> GetCategoriesAsync()
	{
		try
		{
			// 從 FAQ Service 抓啟用的分類
			var data = await _faqService.GetActiveCategoriesAsync();
			return Ok(ApiResponse<IEnumerable<FaqCategoryDto>>.Ok(data));
		}
		catch (Exception ex)
		{
			return BadRequest(ApiResponse<string>.Fail(ex.Message));
		}

	}

}
