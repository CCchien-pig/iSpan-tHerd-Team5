using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
	}
}
