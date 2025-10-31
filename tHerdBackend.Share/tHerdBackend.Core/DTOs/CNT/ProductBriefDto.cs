using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.CNT
{
	public class ProductBriefDto
	{
		public int ProductId { get; set; }
		public string ProductName { get; set; } = "";
		public string ShortDesc { get; set; } = "";
		public string? Badge { get; set; }
		public int? MainSkuId { get; set; }
	}
}
