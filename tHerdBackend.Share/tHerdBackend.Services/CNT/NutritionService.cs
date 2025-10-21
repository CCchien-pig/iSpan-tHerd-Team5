using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tHerdBackend.Core.Dtos;
using tHerdBackend.Core.DTOs.Nutrition;
using tHerdBackend.Core.Interfaces.Nutrition;

namespace tHerdBackend.Services.CNT
{
	/// <summary>
	/// 營養模組 Service
	/// 處理 Repository 回傳資料 → DTO 映射
	/// 修正版：使用 ContentDesc 作為 Description，且使用 GetSampleByIdAsync 查單筆
	/// </summary>
	public class NutritionService : INutritionService
	{
		private readonly INutritionRepository _repo;

		public NutritionService(INutritionRepository repo)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
		}

		// =========================================================================
		// 取得食材列表（支援搜尋 / 分類 / 排序 / 分頁）
		// =========================================================================
		public async Task<(IReadOnlyList<NutritionListDto> Items, int TotalCount)> GetSampleListAsync(
			string? keyword,
			int? categoryId,
			string? sort,
			int page,
			int pageSize,
			CancellationToken ct = default)
		{
			var (rows, total) = await _repo.GetSamplesAsync(keyword, categoryId, sort, page, pageSize, ct);

			var items = rows.Select(r => new NutritionListDto
			{
				SampleId = r.SampleId,
				SampleName = SafeString(r.DisplayName),
				AliasName = SafeString(r.AliasName),
				CategoryName = SafeString(r.CategoryName)
			}).ToList();

			return (items, total);
		}

		// =========================================================================
		// 取得單一食材詳細（Sample + Nutrients）
		// =========================================================================
		public async Task<NutritionDetailDto?> GetSampleDetailAsync(
			int sampleId,
			CancellationToken ct = default)
		{
			// 🆕 精準查 Sample 基本資料（取代原本從列表搜尋）
			var sampleRow = await _repo.GetSampleByIdAsync(sampleId, ct);
			if (sampleRow == null)
			{
				// 若 DB 中無此 Sample，代表真正不存在
				return null;
			}

			// 查 Nutrients
			var nutrientsRaw = await _repo.GetNutrientsBySampleIdAsync(sampleId, ct);

			// 映射 DTO（Description 使用 ContentDesc）
			var dto = new NutritionDetailDto
			{
				SampleId = sampleRow.SampleId,
				SampleName = SafeString(CombineName(sampleRow.SampleName, sampleRow.SampleNameEn)),
				CategoryName = SafeString(sampleRow.CategoryName),
				Description = SafeString(sampleRow.ContentDesc), // ✅ 使用 ContentDesc
				Nutrients = nutrientsRaw.Select(n => new NutrientDto
				{
					Category = SafeString(n.Category),
					Name = SafeString(n.Name),
					Unit = SafeString(n.Unit),
					ValuePer100g = ParseDecimal(n.ValuePer100g)
				}).ToList()
			};

			return dto;
		}

		// =========================================================================
		// Helper：名稱格式化（SampleName + SampleNameEn）
		// =========================================================================
		private static string CombineName(object? nameCh, object? nameEn)
		{
			string ch = nameCh?.ToString()?.Trim() ?? "";
			string en = nameEn?.ToString()?.Trim() ?? "";
			if (!string.IsNullOrEmpty(en))
				return $"{ch} ({en})";
			return ch;
		}

		// =========================================================================
		// Helper：Null → "-" / Trim 字串
		// =========================================================================
		private static string SafeString(object? value)
		{
			var s = value?.ToString()?.Trim();
			return string.IsNullOrEmpty(s) ? "-" : s;
		}

		private static decimal ParseDecimal(object? value)
		{
			if (value == null) return 0;
			if (decimal.TryParse(value.ToString(), out var d)) return d;
			return 0;
		}

		// 既有：GetSampleListAsync / GetSampleDetailAsync ...

		public Task<IReadOnlyList<FoodCategoryDto>> GetFoodCategoriesAsync(CancellationToken ct = default)
			=> _repo.GetFoodCategoriesAsync(ct);

		public async Task<object> CompareAsync(string sampleIds, string analyteIds, CancellationToken ct = default)
		{
			// -------------------------------
			// 1️⃣ 解析輸入參數
			// -------------------------------
			if (string.IsNullOrWhiteSpace(sampleIds) || string.IsNullOrWhiteSpace(analyteIds))
				throw new ArgumentException("必須提供 sampleIds 與 analyteIds");

			var sIds = sampleIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
								.Select(id => int.TryParse(id, out var i) ? i : 0)
								.Where(i => i > 0)
								.ToList();

			var aIds = analyteIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
								 .Select(id => int.TryParse(id, out var i) ? i : 0)
								 .Where(i => i > 0)
								 .ToList();

			// -------------------------------
			// 2️⃣ 食材數量限制檢查
			// -------------------------------
			if (sIds.Count < 2 || sIds.Count > 6)
				throw new ArgumentException("食材數量必須介於 2 至 6 之間。");

			// -------------------------------
			// 3️⃣ 呼叫資料庫查詢
			// -------------------------------
			var data = await _repo.CompareNutritionAsync(sIds, aIds, ct);

			// -------------------------------
			// 4️⃣ 缺值處理 + 單位分組
			// -------------------------------
			var groupedByUnit = data
				.GroupBy(x => (string)(x.Unit ?? "未知單位"))
				.Select(unitGroup => new
				{
					unit = unitGroup.Key,
					analytes = unitGroup
						.GroupBy(x => (int)x.AnalyteId)
						.Select(analyteGroup => new
						{
							analyteId = analyteGroup.Key,
							analyteName = analyteGroup.First().AnalyteName,
							unit = analyteGroup.First().Unit ?? "",
							values = sIds.Select(sampleId =>
							{
								var row = analyteGroup.FirstOrDefault(r => (int)r.SampleId == sampleId);
								return new
								{
									sampleId,
									sampleName = row?.SampleName ?? $"Sample {sampleId}",
									value = row != null ? (decimal)row.ValuePer100g : 0M,
									hasData = row != null
								};
							}).ToList()
						})
						.ToList()
				})
				.ToList();

			// -------------------------------
			// 5️⃣ 結果結構
			// -------------------------------
			return new
			{
				sampleCount = sIds.Count,
				analyteCount = aIds.Count,
				groups = groupedByUnit
			};
		}


	}
}
