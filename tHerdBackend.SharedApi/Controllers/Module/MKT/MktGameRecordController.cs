using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.MKT;
using tHerdBackend.Core.Interfaces.MKT;
using tHerdBackend.Services.MKT;

namespace tHerdBackend.SharedApi.Controllers.Module.MKT
{
    [ApiController]
    [Route("api/mkt/[controller]")]
    [Authorize]
    public class MktGameRecordController : ControllerBase
    {
        private readonly IMktGameRecordService _service;

        public MktGameRecordController(IMktGameRecordService service)
        {
            _service = service;
        }

        /// <summary>
        /// 取得今日遊戲紀錄（檢查是否已玩過）
        /// </summary>
        [HttpGet("{userNumberId}")]
        public async Task<IActionResult> GetTodayRecord(int userNumberId)
        {
            try
            {
                var record = await _service.GetTodayRecordAsync(userNumberId);

                if (record == null)
                {
                    return Ok(new
                    {
                        played = false,
                        message = "今日尚未遊玩",
                        record = (object?)null
                    });
                }

                return Ok(new
                {
                    played = true,
                    message = "今日已玩過遊戲",
                    record
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    played = false,
                    message = $"查詢失敗：{ex.Message}"
                });
            }
        }


        /// <summary>
        /// 新增一筆遊戲紀錄
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MktGameRecordDto dto)
        {
            try
            {
                var result = await _service.CreateRecordAsync(dto);

                return Ok(new
                {
                    success = true,
                    message = "遊戲紀錄已建立，恭喜獲得優惠券！",
                    data = result
                });
            }
            catch (Exception ex)
            {
                // 特殊業務邏輯：「今日已玩過遊戲」屬於正常情況，不該回 500
                if (ex.Message.Contains("今日已玩過遊戲"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = ex.Message,
                        data = (object?)null
                    });
                }

                // 其他未預期錯誤才用 500
                return StatusCode(500, new
                {
                    success = false,
                    message = "建立遊戲紀錄時發生錯誤：" + ex.Message
                });
            }
        }

    }
}
