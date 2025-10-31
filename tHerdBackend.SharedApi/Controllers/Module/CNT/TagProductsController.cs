using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;

namespace tHerdBackend.SharedApi.Controllers.Module.CNT
{
	// GET /api/cnt/tags/{tagId}/products?page=1&pageSize=24
	[ApiController]
	[Route("api/cnt/tags/{tagId:int}/products")]
	public class TagProductsController : ControllerBase
	{
		private readonly tHerdDBContext _db;

		public TagProductsController(tHerdDBContext db)
		{
			_db = db;
		}

		[HttpGet]
		public async Task<ActionResult> GetByTagId(
			[FromRoute] int tagId,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 24
		)
		{
			// 防呆，避免 page=0 / pageSize=0
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = 24;

			// 先組查詢（注意：這裡的欄位要和你前端需要的東西一致）
			var baseQuery =
				from pt in _db.CntProductTags
				where pt.TagId == tagId
					  && pt.IsVisible == true
					  && pt.IsDeleted == false
					  && pt.Product.IsPublished == true
				orderby
					pt.IsPrimary descending,
					pt.DisplayOrder ascending,
					(pt.Product.RevisedDate ?? pt.Product.CreatedDate) descending
				select new ProductBriefDto
				{
					ProductId = pt.Product.ProductId,
					ProductName = pt.Product.ProductName,
					ShortDesc = pt.Product.ShortDesc,
					Badge = pt.Product.Badge,
					MainSkuId = pt.Product.MainSkuId,
				};

			// 總筆數（這個 tag 總共有幾個可顯示商品）
			var total = await baseQuery.CountAsync();

			// 這一頁要回的資料
			var items = await baseQuery
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// 回傳物件，讓前端知道總共有幾筆、這頁有哪些項目
			return Ok(new
			{
				total,
				items
			});
		}
	}
}
