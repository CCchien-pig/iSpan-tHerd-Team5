using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Interfaces.SUP;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	[ApiController]
	[Route("api/[controller]")]
	public class LogisticsController : ControllerBase
	{
		private readonly ILogisticsService _service;

		public LogisticsController(ILogisticsService service)
		{
			_service = service;
		}

		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var list = await _service.GetAllAsync();
				return Ok(list);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> Get(int id)
		{
			try
			{
				var log = await _service.GetByIdAsync(id);
				if (log == null)
					return NotFound(new { success = false, message = "找不到該物流商" });
				return Ok(log);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}
	}

}
