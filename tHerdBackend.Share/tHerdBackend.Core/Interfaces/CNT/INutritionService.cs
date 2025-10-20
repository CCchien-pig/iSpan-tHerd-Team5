using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tHerdBackend.Core.Dtos;
using tHerdBackend.Core.DTOs.Nutrition;

namespace tHerdBackend.Core.Interfaces.Nutrition
{
	public interface INutritionService
	{
		/// <summary>
		/// 取得食材列表（DTO 版本，給 API 使用）
		/// </summary>
		Task<(IReadOnlyList<NutritionListDto> Items, int TotalCount)> GetSampleListAsync(
			string? keyword,
			int? categoryId,
			string? sort,
			int page,
			int pageSize,
			CancellationToken ct = default);

		/// <summary>
		/// 取得單一食材營養明細（DTO 版本）
		/// </summary>
		Task<NutritionDetailDto?> GetSampleDetailAsync(
			int sampleId,
			CancellationToken ct = default);

		// 既有：GetSampleListAsync / GetSampleDetailAsync ...
		Task<IReadOnlyList<FoodCategoryDto>> GetFoodCategoriesAsync(CancellationToken ct = default);
	}
}
