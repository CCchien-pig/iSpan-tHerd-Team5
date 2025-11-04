using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Infra.Models;   // 這裡換成你自己的命名空間

namespace tHerdBackend.SharedApi.Controllers.Module.CNT
{
	[ApiController]
	[Route("api/cnt/tags")]
	public class TagsController : ControllerBase
	{
		private readonly tHerdDBContext _db;

		public TagsController(tHerdDBContext db)
		{
			_db = db;
		}

		// GET /api/cnt/tags/1000
		[HttpGet("{tagId:int}")]
		public async Task<ActionResult<TagInfoDto>> GetTag(int tagId)
		{
			if (tagId <= 0)
			{
				return BadRequest("tagId 必須是正整數");
			}

			var dto = await _db.CntTags   // 這裡的 CntTags 換成你實際的 DbSet 名稱
				.Where(t => t.TagId == tagId && t.IsActive == true)
				.Select(t => new TagInfoDto
				{
					TagId = t.TagId,
					TagName = t.TagName,

					// ⚠ DB 現在沒有這兩欄，先給固定值 or null
					TagTypeName = "商品標籤",   // 小圓角 pill 要顯示的文字（你也可以寫「健康中心標籤」）
					Description = null        // 目前沒有描述欄位，就先留空
				})
				.FirstOrDefaultAsync();

			if (dto == null)
			{
				return NotFound();
			}

			return Ok(dto);
		}
	}
}
