using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	[ApiController]
	[Route("api/[folder]/[controller]")]
	[Authorize]
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
		/// <summary>
		/// 取得 AntiForgery Token （如前端需用，可保留此匿名）
		/// </summary>
		[HttpGet("token")]
		[AllowAnonymous]
		public IActionResult GetToken()
		{
			var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
			return Ok(ApiResponse<string>.Ok(tokens.RequestToken, "取得成功"));
		}

		//GET /api/sup/LogisticsRate/bylogistics/{logisticsId}
		/// <summary>
		/// 依物流商ID查詢所有運費率（可匿名）
		/// </summary>
		[HttpGet("bylogistics/{logisticsId}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetByLogisticsId(int logisticsId)
		{
			try
			{
				var rates = await _service.GetByLogisticsIdAsync(logisticsId);
				return Ok(ApiResponse<List<LogisticsRateDto>>.Ok(rates, "查詢成功"));
			}
			catch (Exception ex)
			{
				// 統一回傳失敗格式
				return Ok(ApiResponse<List<LogisticsRateDto>>.Fail("系統錯誤：" + ex.Message));
			}
		}

		//POST /api/sup/LogisticsRate/order-shipping-fee
		/// <summary>
		/// 訂單運費計算（需授權?），現在先用[AllowAnonymous]
		/// </summary>
		[HttpPost("order-shipping-fee")]
		[AllowAnonymous]
		public async Task<IActionResult> CalculateShippingFee(
			[FromBody] ShippingFeeDto.ShippingFeeRequestDto request)
		{
			try
			{
				var result = await _service.CalculateShippingFeeAsync(request);

				// 統一用 ApiResponse 格式回傳
				return Ok(ApiResponse<ShippingFeeDto.ShippingFeeResponseDto>.Ok(result, result.Message ?? "計算完成"));
			}
			catch (Exception ex)
			{
				return Ok(ApiResponse<ShippingFeeDto.ShippingFeeResponseDto>.Fail("系統錯誤：" + ex.Message));
			}
		}
	}
}
