using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.Nutrition
{
	/// <summary>
	/// 食材列表 DTO（給前端列表頁使用）
	/// </summary>
	public class NutritionListDto
	{
		public int SampleId { get; set; }
		public string SampleName { get; set; } = string.Empty;
		public string? AliasName { get; set; }
		public string CategoryName { get; set; } = string.Empty;
	}
}

