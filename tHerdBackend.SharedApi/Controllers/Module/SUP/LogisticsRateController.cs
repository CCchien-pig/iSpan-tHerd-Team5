using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	[ApiController]
	[Route("api/[folder]/[controller]")]
	public class LogisticsRateController : ControllerBase
	{
		private readonly ILogisticsRateService _service;
		private readonly IAntiforgery _antiforgery;

		public LogisticsRateController(
			ILogisticsRateService service,
			IAntiforgery antiforgery)
		{
			_service = service;
			_antiforgery = antiforgery;
		}

		//GET /api/sup/LogisticsRate/token
		[HttpGet("token")]
		public IActionResult GetToken()
		{
			var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
			return Ok(new { token = tokens.RequestToken });
		}

		//GET /api/sup/LogisticsRate/bylogistics/{logisticsId}
		[HttpGet("bylogistics/{logisticsId}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetByLogisticsId(int logisticsId)
		{
			try
			{
				var rates = await _service.GetByLogisticsIdAsync(logisticsId);
				return Ok(rates);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
			}
		}
	}
}