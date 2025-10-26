using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.Nutrition
{
	/// <summary>
	/// 營養素資料 DTO（每一行：維生素、礦物質、脂肪酸...）
	/// </summary>
	public class NutrientDto
	{
		public string Category { get; set; } = string.Empty;   // 如：維生素B群
		public string Name { get; set; } = string.Empty;       // 如：維生素B1
		public string Unit { get; set; } = string.Empty;       // mg, µg
		public decimal ValuePer100g { get; set; }              // 每100g含量
	}
}

