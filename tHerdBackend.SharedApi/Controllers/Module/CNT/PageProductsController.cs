using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Core.Interfaces.CNT;

namespace tHerdBackend.SharedApi.Controllers.Module.CNT
{
	[ApiController]
	[Route("api/cnt/pages/{pageId:int}/products")]
	public class PageProductsController : ControllerBase
	{
		private readonly IContentProductService _svc;

		public PageProductsController(IContentProductService svc)
		{
			_svc = svc;
		}

		// GET /api/cnt/pages/123/products?take=24
		[HttpGet]
		public async Task<ActionResult<IEnumerable<ProductBriefDto>>> Get(
			[FromRoute] int pageId,
			[FromQuery] int take = 24)
		{
			var data = await _svc.GetRelatedProductsForPageAsync(pageId, take);
			return Ok(data);
		}
	}
}
