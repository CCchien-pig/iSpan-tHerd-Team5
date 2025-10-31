using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;

namespace tHerdBackend.SharedApi.Controllers.Module.CNT
{
	//用標籤ID查
	//用標籤ID查
	//用標籤ID查
	// GET /api/cnt/tags/{tagId}/products
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
		public async Task<ActionResult<IEnumerable<ProductBriefDto>>> GetByTagId(
			[FromRoute] int tagId,
			[FromQuery] int take = 24)
		{
			// 從 CNT_ProductTag(CntProductTag) 出發
			// 注意：CntProductTag 已經有 Product 導覽屬性，指向商品 ProdProduct，包含 ProductName / IsPublished 等欄位。:contentReference[oaicite:1]{index=1}

			var query =
				from pt in _db.CntProductTags
				where pt.TagId == tagId
					  && pt.IsVisible == true
					  && pt.IsDeleted == false
					  && pt.Product.IsPublished == true
				orderby
					pt.IsPrimary descending,                          // 主要標籤優先顯示
					pt.DisplayOrder ascending,                        // 手動排序
					(pt.Product.RevisedDate ?? pt.Product.CreatedDate) descending
				select new ProductBriefDto
				{
					ProductId = pt.Product.ProductId,
					ProductName = pt.Product.ProductName,
					ShortDesc = pt.Product.ShortDesc,
					Badge = pt.Product.Badge,
					MainSkuId = pt.Product.MainSkuId
				};

			var list = await query
				.Take(take)
				.ToListAsync();

			return Ok(list);
		}
	}
}
