using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.Interfaces.PROD;

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
		[HttpGet("search")]
		public async Task<IActionResult> SearchProducts(
			[FromQuery] ProductFilterQueryDto query,
			CancellationToken ct = default)
		{
			var result = await _service.GetFrontProductListAsync(query, ct);
			return Ok(result);
		}
	}
}
