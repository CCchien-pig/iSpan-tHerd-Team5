using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.PROD
{
	[ApiController]
	[Route("api/[controller]")]   // 統一為 /api/Products
	public class ProductsController : ControllerBase
	{
		private readonly IProductListForApiService _service;  // 服務注入

		// 建構子注入 Service
		public ProductsController(IProductListForApiService service)
		{
			_service = service;
		}

        /// <summary>
        /// 前台：查詢產品清單 (支援關鍵字、分類、價格區間、分頁)
        /// </summary>
		[AllowAnonymous]  // 不用 JWT，前台也能看
        [HttpGet("search")]
		public async Task<IActionResult> SearchProducts(
			[FromQuery] ProductFilterQueryDto query,
			CancellationToken ct = default)
		{
            try
            {
                var result = await _service.GetFrontProductListAsync(query, ct);

                return Ok(ApiResponse<PagedResult<ProdProductDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                // 捕捉異常（例如 SQL 錯誤、參數錯誤）
                return StatusCode(500, ApiResponse<string>.Fail($"伺服器錯誤：{ex.Message}"));
            }
        }
    }
}
