using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.Nutrition;

namespace tHerdBackend.SharedApi.Controllers.Module.CNT
{
	/// <summary>
	/// 營養分析 API
	/// 路徑：/api/cnt/nutrition/...
	/// 支援：列表、詳細查詢
	/// </summary>
	[Route("api/cnt/nutrition")]
	[ApiController]
	public class NutritionController : ControllerBase
	{
		private readonly INutritionService _service;

		public NutritionController(INutritionService service)
		{
			_service = service;
		}

		// =========================================================================
		// GET: /api/cnt/nutrition/list
		// 參數：keyword, categoryId, sort, page, pageSize
		// 回傳：{ items, total, page, pageSize }
		// =========================================================================
		[HttpGet("list")]
		public async Task<IActionResult> GetList(
			[FromQuery] string? keyword,
			[FromQuery] int? categoryId,
			[FromQuery] string? sort,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 12,
			CancellationToken ct = default)
		{
			var (items, total) = await _service.GetSampleListAsync(
				keyword,
				categoryId,
				sort,
				page,
				pageSize,
				ct
			);

			return Ok(new
			{
				items,
				total,
				page,
				pageSize
			});
		}

		// =========================================================================
		// GET: /api/cnt/nutrition/{id}
		// 取得單一食材完整營養資料
		// 回傳：{ sample, nutrients }
		// =========================================================================
		[HttpGet("{id:int}")]
		public async Task<IActionResult> GetDetail(int id, CancellationToken ct = default)
		{
			var dto = await _service.GetSampleDetailAsync(id, ct);
			if (dto == null)
			{
				return NotFound(new { message = $"Sample not found: {id}" });
			}

			return Ok(new
			{
				sample = new
				{
					dto.SampleId,
					dto.SampleName,
					dto.CategoryName,
					dto.Description
				},
				nutrients = dto.Nutrients
			});
		}
	}
}
