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

		/// <summary>
		/// 多食材 × 多營養素 比較查詢（給前端圖表使用）
		/// </summary>
		/// <param name="sampleIds">以逗號分隔的 SampleId 清單，例如 "1001,1002,1003"</param>
		/// <param name="analyteIds">以逗號分隔的 AnalyteId 清單，例如 "1105,1107"</param>
		/// <param name="ct">取消 Token</param>
		/// <returns>每個營養素的各食材數值（供 Chart 使用）</returns>
		Task<object> CompareAsync(string sampleIds, string analyteIds, CancellationToken ct = default);

		Task<List<NutritionListDto>> GetAllSamplesAsync(
				string? keyword,
				int? categoryId,
				string? sort,
				CancellationToken ct = default);

		//營養素選單
		Task<IReadOnlyList<object>> GetAnalyteListAsync(bool isPopular, CancellationToken ct = default);


	}
}
