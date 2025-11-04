using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Core.ValueObjects;
using tHerdBackend.Infra.Models;


[ApiController]
[AllowAnonymous] 
[Route("api/cs/[controller]")]
public class CsTicketsController : ControllerBase
{
	private readonly ICsTicketService _service;
	private readonly IFaqService _faqService;
    private readonly UserManager<ApplicationUser> _userMgr;
    private readonly ApplicationDbContext _appDb;
    public CsTicketsController(
        ICsTicketService service,
        IFaqService faqService,
        UserManager<ApplicationUser> userMgr,
        ApplicationDbContext appDb
    )
    {
        _service = service;
        _faqService = faqService;
        _userMgr = userMgr;
        _appDb = appDb;
    }
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
	/// <summary>
	/// 建立新客服工單（需登入會員，可上傳圖片，Email 使用者自行輸入）
	/// </summary>
	[HttpPost("create")]
	[Authorize] // ✅ 需登入，才能抓 user_number_id
	[RequestSizeLimit(10_000_000)]
	public async Task<IActionResult> CreateAsync([FromForm] TicketIn dto, IFormFile? image)
	{
		try
		{
			// ✅ 從 JWT 取出 user_number_id
			var userNumClaim = User.Claims.FirstOrDefault(c => c.Type == "user_number_id");
			if (userNumClaim == null)
				return Unauthorized(ApiResponse<string>.Fail("Token 無效或未登入"));

			dto.UserId = int.Parse(userNumClaim.Value);

			// ✅ 呼叫 Service 建立工單
			var id = await _service.CreateAsync(dto, image);

			// ✅ 查詢建立後的完整資料
			var data = await _service.GetTicketByIdAsync(id);

			return Ok(ApiResponse<object>.Ok(data, "工單建立成功"));
		}
		catch (Exception ex)
		{
			return BadRequest(ApiResponse<string>.Fail(ex.Message));
		}
	}




	//   /// <summary>建立新客服工單（前台客戶可匿名，上傳 1 張附件圖片）</summary>
	//   [HttpPost("create")]
	//[AllowAnonymous]
	//[RequestSizeLimit(10_000_000)] // 限制上傳大小（10MB）
	//public async Task<IActionResult> CreateAsync([FromForm] TicketIn dto, IFormFile? image)
	//{
	//	try
	//	{
	//		// ✅ 呼叫 Service，傳入工單資料與圖片
	//		var id = await _service.CreateAsync(dto, image);

	//		var data = await _service.GetTicketByIdAsync(id);
	//		return Ok(ApiResponse<object>.Ok(data, "建立成功"));
	//	}
	//	catch (Exception ex)
	//	{
	//		return BadRequest(ApiResponse<string>.Fail(ex.Message));
	//	}
	//}

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
    //[Authorize]
    //[HttpPost("create")]
    //public async Task<IActionResult> CreateAsync([FromBody] TicketIn dto)
    //{
    //    // 取得登入者 Id
    //    var userId = _userMgr.GetUserId(User);
    //    if (string.IsNullOrEmpty(userId))
    //        return Unauthorized(new { error = "未登入" });

    //    // 查出會員 Email + UserNumberId
    //    var u = await _appDb.Users
    //        .AsNoTracking()
    //        .Where(x => x.Id == userId)
    //        .Select(x => new { x.UserNumberId, x.Email })
    //        .FirstOrDefaultAsync();

    //    if (u == null)
    //        return NotFound(new { error = "找不到會員資料" });

    //    // 把會員資料寫進 DTO
    //    dto.UserId = u.UserNumberId;
    //    dto.Email = u.Email;

    //    // 建立工單
    //    var ticketId = await _service.CreateAsync(dto);
    //    return Ok(new { ok = true, ticketId, message = "工單建立成功" });
    //}


}
