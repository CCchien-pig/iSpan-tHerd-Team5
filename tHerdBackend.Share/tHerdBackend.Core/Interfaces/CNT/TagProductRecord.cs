using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.Interfaces.CNT
{
	/// <summary>
	/// Repo 層查出來「某個標籤底下的一筆商品基本資料」。
	/// 這是 Service 組成 ProdProductDto 的原料，不直接回前端。
	/// </summary>
	public class TagProductRecord
	{
		public int ProductId { get; set; }

		public string ProductName { get; set; } = string.Empty;

		public string Badge { get; set; } = string.Empty;

		public bool IsPublished { get; set; }

		public int? MainSkuIdRaw { get; set; }

		public string ProductCode { get; set; } = string.Empty;

		public int Creator { get; set; }
		public DateTime CreatedDate { get; set; }
		public int? Reviser { get; set; }
		public DateTime? RevisedDate { get; set; }

		// 這些欄位不是必然直接存在 ProdProduct，
		// 我們會用 left join / 導覽屬性投影進來，如果抓不到就給空
		public string BrandName { get; set; } = string.Empty;

		public string? ProductTypeName { get; set; } = string.Empty;
	}
}
