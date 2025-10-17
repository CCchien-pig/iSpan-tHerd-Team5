using Microsoft.AspNetCore.Authorization;//匯入授權相關的標註與型別
using Microsoft.AspNetCore.Mvc;//匯入 ASP.NET Core MVC 相關的標註與型別
using tHerdBackend.Core.DTOs.CS;//
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.CS
{
	/// <summary>常見問題（FAQ）查詢、搜尋與回饋</summary>
	[ApiController]
	[Authorize]
	[Route("api/cs/[controller]")]
	public class FaqsController : ControllerBase
	{
		private readonly IFaqService _service;
		public FaqsController(IFaqService service) => _service = service;

		/// <summary>取得分類＋FAQ 列表（僅啟用）</summary>
		[HttpGet("list")]
		[AllowAnonymous] //表示不用登入，Vue 也能直接請求
		public async Task<IActionResult> GetListAsync()
		{
			try
			{
				var data = await _service.GetListAsync();
				return Ok(ApiResponse<List<CategoryWithFaqsDto>>.Ok(data));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse<string>.Fail(ex.Message));
			}
		}

		/// <summary>搜尋 FAQ（標題/內容/關鍵字）</summary>
		[HttpGet("search")]
		[AllowAnonymous]
		public async Task<IActionResult> SearchAsync([FromQuery] string q)
		{
			try
			{
				var data = await _service.SearchAsync(q);
				return Ok(ApiResponse<List<FaqSearchDto>>.Ok(data));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse<string>.Fail(ex.Message));
			}
		}

		/// <summary>回報 FAQ 是否有幫助</summary>
		[HttpPost("feedback")]
		public async Task<IActionResult> FeedbackAsync([FromBody] FaqFeedbackIn dto)
		{
			try
			{
				await _service.AddFeedbackAsync(dto);
				return Ok(ApiResponse<object>.Ok(new { ok = true }, "新增成功"));
			}
			catch (Exception ex)
			{
				return BadRequest(ApiResponse<string>.Fail(ex.Message));
			}
		}
	}
}
