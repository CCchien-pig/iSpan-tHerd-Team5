using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.MKT;
using tHerdBackend.Core.Interfaces.MKT;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.MKT
{
    [ApiController]
    [Route("api/promotion")]
    public class PromotionController : ControllerBase
    {
        private readonly IMktPromotionService _service;

        public PromotionController(IMktPromotionService service)
        {
            _service = service;
        }

        /// <summary>
        /// 計算優惠券折扣金額
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] PromotionCalculateRequestDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(ApiResponse<string>.Fail("請提供正確的請求內容"));

                var result = await _service.CalculatePromotionAsync(dto);

                // ✅ 根據服務回傳的 success 狀態來決定 ApiResponse
                if (result.Success)
                    return Ok(ApiResponse<object>.Ok(result.Data, result.Message));

                return Ok(ApiResponse<object>.Fail(result.Message));
            }
            catch (Exception ex)
            {
                // ✅ 捕捉所有未預期錯誤
                return StatusCode(500, ApiResponse<string>.Fail($"伺服器發生錯誤：{ex.Message}"));
            }
        }
    }
}
