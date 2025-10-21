using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace tHerdBackend.Core.DTOs.Nutrition
{
	/// <summary>
	/// 食材詳細（整合基本資料 + 營養素列表）
	/// </summary>
	public class NutritionDetailDto
	{
		public int SampleId { get; set; }
		public string SampleName { get; set; } = string.Empty;
		public string CategoryName { get; set; } = string.Empty;
		public string? Description { get; set; }   // ContentDesc / 样品描述

		public List<NutrientDto> Nutrients { get; set; } = new();
	}
}

