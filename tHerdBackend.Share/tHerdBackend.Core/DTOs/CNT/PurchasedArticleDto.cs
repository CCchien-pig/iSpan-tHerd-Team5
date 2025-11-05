using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.CNT
{
	/// <summary>會員中心「我買過的文章」列表 DTO</summary>
	public class PurchasedArticleDto
	{
		public int PurchaseId { get; set; }
		public int PageId { get; set; }

		public string Title { get; set; } = "";
		public decimal UnitPrice { get; set; }

		public DateTime PurchasedDate { get; set; }
		public DateTime? PublishedDate { get; set; }

		public string CategoryName { get; set; } = "";
	}
}
