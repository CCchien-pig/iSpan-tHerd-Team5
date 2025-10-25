using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.MKT;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.MKT
{
    /// <summary>
    /// 活動 API
    /// </summary>
    [ApiController]
    [Route("api/mkt/[controller]")]
    [AllowAnonymous] // 暫時允許匿名，若要權限可改為 [Authorize]
    public class CampaignController : ControllerBase
    {
        private readonly ICampaignService _service;

        public CampaignController(ICampaignService service)
        {
            _service = service;
        }

        /// <summary>
        /// 取得啟用中的活動資料
        /// GET /api/mkt/campaign/active
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCampaigns()
        {
            try
            {
                var campaigns = await _service.GetAllAsync();
                var activeCampaigns = campaigns.Where(c => c.IsActive == true).ToList();

                if (activeCampaigns == null || !activeCampaigns.Any())
                    return Ok(ApiResponse<object>.Fail("目前沒有啟用中的活動"));

                return Ok(ApiResponse<object>.Ok(activeCampaigns, "查詢成功"));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Fail("系統錯誤：" + ex.Message));
            }
        }
    }
}
