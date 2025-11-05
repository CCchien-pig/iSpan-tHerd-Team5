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

    /// <summary>取得全部客服工單清單（限後台登入者）</summary>
    [HttpGet("list")]
    [Authorize(Roles = "Admin,CustomerService")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TicketsDto>>>> GetAllAsync()
    {
        var data = await _service.GetAllAsync();
        return ApiResponse<IEnumerable<TicketsDto>>.Ok(data);
    }

    /// <summary>取得「我的工單」清單（前台會員使用）</summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<TicketsDto>>>> GetMyTickets()
    {
        var userNumClaim = User.Claims.FirstOrDefault(c => c.Type == "user_number_id");
        if (userNumClaim == null || !int.TryParse(userNumClaim.Value, out var userNumberId))
            return BadRequest(ApiResponse<string>.Fail("Token 無效或遺失 user_number_id"));

        var data = await _service.GetByUserIdAsync(userNumberId);
        return ApiResponse<IEnumerable<TicketsDto>>.Ok(data);
    }

    /// <summary>查詢單筆工單（含分類名稱與留言）</summary>
    [HttpGet("{ticketId:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TicketOut>>> GetById(int ticketId)
    {
        var ticket = await _service.GetTicketByIdAsync(ticketId);
        if (ticket == null)
            return NotFound(ApiResponse<string>.Fail("找不到該工單"));

        return ApiResponse<TicketOut>.Ok(ticket);
    }

    /// <summary>建立新客服工單（前台客戶可匿名，上傳 1 張附件圖片）</summary>
    [HttpPost("create")]
    [AllowAnonymous]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ApiResponse<object>>> CreateAsync([FromForm] TicketIn dto, IFormFile? image)
    {
        var id = await _service.CreateAsync(dto, image);
        var data = await _service.GetTicketByIdAsync(id);
        return ApiResponse<object>.Ok(data, "建立成功");
    }

    /// <summary>新增一筆客服回覆 (後台客服人員使用，並觸發 Email 通知)</summary>
    [HttpPost("{ticketId:int}/reply")]
    [Authorize(Roles = "Admin,CustomerService")]
    public async Task<ActionResult<ApiResponse<object>>> AddReplyAsync(int ticketId, [FromBody] ReplyIn dto)
    {
        await _service.AddReplyAsync(ticketId, dto.Content);
        return ApiResponse<object>.Ok(null, "回覆成功並已寄出通知");
    }

    /// <summary>取得 FAQ 問題分類（供工單下拉選單用）</summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<FaqCategoryDto>>>> GetCategoriesAsync()
    {
        var data = await _faqService.GetActiveCategoriesAsync();
        return ApiResponse<IEnumerable<FaqCategoryDto>>.Ok(data);
    }

    //[Authorize]
    //[HttpPost("create")]
    //public async Task<IActionResult> CreateAsync([FromBody] TicketIn dto)
    //{
    //    // 取得登入者 Id
    //    var userId = _userMgr.GetUserId(User);
    //    if (string.IsNullOrEmpty(userId))
    //        return Unauthorized(new { error = "未登入" });
    //
    //    // 查出會員 Email + UserNumberId
    //    var u = await _appDb.Users
    //        .AsNoTracking()
    //        .Where(x => x.Id == userId)
    //        .Select(x => new { x.UserNumberId, x.Email })
    //        .FirstOrDefaultAsync();
    //
    //    if (u == null)
    //        return NotFound(new { error = "找不到會員資料" });
    //
    //    // 把會員資料寫進 DTO
    //    dto.UserId = u.UserNumberId;
    //    dto.Email = u.Email;
    //
    //    // 建立工單
    //    var ticketId = await _service.CreateAsync(dto);
    //    return Ok(new { ok = true, ticketId, message = "工單建立成功" });
    //}
}

/// <summary>接收回覆內容的 DTO</summary>
public class ReplyIn
{
    public required string Content { get; set; }
}