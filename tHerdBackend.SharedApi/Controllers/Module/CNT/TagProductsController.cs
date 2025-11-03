using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.CNT;   // ITagProductQueryService, PagedResult<T>
using tHerdBackend.Core.DTOs.PROD;       // ProdProductDto

namespace tHerdBackend.SharedApi.Controllers.Module.CNT
{
	// GET /api/cnt/tags/{tagId}/products?page=1&pageSize=24
	[ApiController]
	[Route("api/cnt/tags")]
	public class TagProductsController : ControllerBase
	{
		private readonly ITagProductQueryService _tagProductQueryService;

		public TagProductsController(ITagProductQueryService tagProductQueryService)
		{
			_tagProductQueryService = tagProductQueryService;
		}

		/// <summary>
		/// 取得某個標籤底下的商品清單（分頁、排序，並且包含主圖URL、價格、星等等資訊）
		/// 前端「標籤導過來的商品列表頁」直接打這支。
		/// e.g. GET /api/cnt/tags/1002/products?page=1&pageSize=24
		/// </summary>
		[HttpGet("{tagId:int}/products")]
		public async Task<ActionResult<PagedResult<ProdProductDto>>> GetProductsByTag(
			[FromRoute] int tagId,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 24)
		{
			if (tagId <= 0)
			{
				return BadRequest("tagId 必須是正整數");
			}

			// 呼叫 service，而不是直接碰 DbContext
			var result = await _tagProductQueryService
				.GetProductsByTagAsync(tagId, page, pageSize);

			// result 內容長這樣：
			// {
			//   Total = 42,
			//   Items = [
			//     {
			//       ProductId,
			//       ProductName,
			//       Badge,
			//       ImageUrl,
			//       SalePrice,
			//       ListPrice,
			//       AvgRating,
			//       ReviewCount,
			//       ...
			//     }, ...
			//   ]
			// }
			return Ok(result);
		}
	}
}
