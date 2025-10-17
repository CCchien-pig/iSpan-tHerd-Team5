using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	/// <summary>
	/// 物流商API
	/// </summary>
	[ApiController]
	[Route("api/sup/[controller]")]
	//[Authorize] // 預設授權
	[AllowAnonymous]
	public class LogisticsController : ControllerBase
	{
		private readonly ILogisticsService _service;

		public LogisticsController(ILogisticsService service)
		{
			_service = service;
		}

		/// <summary>
		/// 取得所有物流商
		/// GET /api/sup/logistics
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var list = await _service.GetAllAsync();
				return Ok(ApiResponse<object>.Ok(list, "查詢成功"));
			}
			catch (Exception ex)
			{
				return Ok(ApiResponse<object>.Fail("系統錯誤：" + ex.Message));
			}
		}

		/// <summary>
		/// 取得所有啟用物流商
		/// GET /api/sup/logistics/active
		/// </summary>
		[HttpGet("active")]
		public async Task<IActionResult> GetActiveLogistics()
		{
			try
			{
				var list = await _service.GetActiveAsync();
				return Ok(ApiResponse<object>.Ok(list, "查詢成功"));
			}
			catch (Exception ex)
			{
				return Ok(ApiResponse<object>.Fail("系統錯誤：" + ex.Message));
			}
		}

		/// <summary>
		/// 取得單一物流商
		/// GET /api/sup/logistics/{id}
		/// </summary>
		[HttpGet("{id}")]
		public async Task<IActionResult> Get(int id)
		{
			try
			{
				var log = await _service.GetByIdAsync(id);
				if (log == null)
					return Ok(ApiResponse<object>.Fail("找不到該物流商"));
				return Ok(ApiResponse<object>.Ok(log, "查詢成功"));
			}
			catch (Exception ex)
			{
				return Ok(ApiResponse<object>.Fail("系統錯誤：" + ex.Message));
			}
		}
	}
}
