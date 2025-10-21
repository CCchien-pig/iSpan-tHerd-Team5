using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tHerdBackend.Core.Dtos;

namespace tHerdBackend.Core.Interfaces.Nutrition
{
	public interface INutritionRepository
	{
		/// <summary>
		/// 取得食材列表（支援搜尋 / 分類 / 排序 / 分頁）
		/// </summary>
		Task<(IReadOnlyList<dynamic> Items, int TotalCount)> GetSamplesAsync(
			string? keyword,
			int? categoryId,
			string? sort,
			int page,
			int pageSize,
			CancellationToken ct = default);

		/// <summary>
		/// 取得單一 Sample 的基本資料（SampleName, CategoryName, ContentDesc）
		/// 用於 /nutrition/{id} 詳細頁
		/// </summary>
		Task<dynamic?> GetSampleByIdAsync(
			int sampleId,
			CancellationToken ct = default);

		/// <summary>
		/// 取得指定 SampleId 的營養素明細
		/// </summary>
		Task<IReadOnlyList<dynamic>> GetNutrientsBySampleIdAsync(
			int sampleId,
			CancellationToken ct = default);

		// 既有：GetSamplesAsync / GetSampleByIdAsync / GetNutrientsBySampleIdAsync ...
		Task<IReadOnlyList<FoodCategoryDto>> GetFoodCategoriesAsync(CancellationToken ct = default);

		/// <summary>
		/// 多食材 × 多營養素 比較查詢（Compare）
		/// </summary>
		Task<IReadOnlyList<dynamic>> CompareNutritionAsync(
			IEnumerable<int> sampleIds,
			IEnumerable<int> analyteIds,
			CancellationToken ct = default);

	}
}
