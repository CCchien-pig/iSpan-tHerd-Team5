using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.Nutrition;
using tHerdBackend.Services.CNT;

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
		private readonly INutritionService _nutritionService;

		public NutritionController(INutritionService nutritionService)
		{
			_nutritionService = nutritionService;
		}

		// =========================================================================
		// GET: /api/cnt/nutrition/list
		// 參數：keyword, categoryId, sort, page, pageSize, all
		// 回傳：{ items, total, page, pageSize }
		// =========================================================================
		[HttpGet("list")]
		public async Task<IActionResult> GetList(
			[FromQuery] string? keyword,
			[FromQuery] int? categoryId,
			[FromQuery] string? sort,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 12,
			[FromQuery] bool all = false,             // ⬅️ 新增
			CancellationToken ct = default)
		{
			try
			{
				// ⬅️ 新增：一次回傳全部（給前端下拉）
				if (all)
				{
					var allItems = await _nutritionService.GetAllSamplesAsync(keyword, categoryId, sort, ct);
					return Ok(new { items = allItems, total = allItems.Count });
				}

				var (items, total) = await _nutritionService.GetSampleListAsync(
					keyword, categoryId, sort, page, pageSize, ct);

				return Ok(new { items, total, page, pageSize });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = "伺服器內部錯誤", detail = ex.Message });
			}
		}


		// =========================================================================
		// GET: /api/cnt/nutrition/{id}
		// 取得單一食材完整營養資料
		// 回傳：{ sample, nutrients }
		// =========================================================================
		[HttpGet("{id:int}")]
		public async Task<IActionResult> GetDetail(int id, CancellationToken ct = default)
		{
			var dto = await _nutritionService.GetSampleDetailAsync(id, ct);
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

		/// <summary>
		/// 取得食物分類清單（for 前端下拉）
		/// </summary>
		[HttpGet("foodcategories")]
		public async Task<IActionResult> GetFoodCategories(CancellationToken ct)
		{
			var data = await _nutritionService.GetFoodCategoriesAsync(ct);
			return Ok(data); // [{ id, name }, ...]
		}

		[HttpGet("compare")]
		public async Task<IActionResult> Compare([FromQuery] string sampleIds, [FromQuery] string analyteIds, CancellationToken ct)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(sampleIds) || string.IsNullOrWhiteSpace(analyteIds))
					return BadRequest(new { error = "必須提供 sampleIds 與 analyteIds。" });

				var result = await _nutritionService.CompareAsync(sampleIds, analyteIds, ct);
				return Ok(result);
			}
			catch (ArgumentException ex)
			{
				// ⚠️ 捕捉你在 Service 中丟出的錯誤（例如「食材數量必須介於 2 至 6 之間」）
				return BadRequest(new { error = ex.Message });
			}
			catch (Exception ex)
			{
				// 其他未預期錯誤，仍用 500 但不回傳堆疊
				return StatusCode(500, new { error = "伺服器內部錯誤", detail = ex.Message });
			}
		}


		[HttpGet("analytes")]
		public async Task<IActionResult> GetAnalytes([FromQuery] bool isPopular = false, CancellationToken ct = default)
		{
			try
			{
				var list = await _nutritionService.GetAnalyteListAsync(isPopular, ct);
				return Ok(new { items = list });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = "載入營養素清單失敗", detail = ex.Message });
			}
		}


	}
}
