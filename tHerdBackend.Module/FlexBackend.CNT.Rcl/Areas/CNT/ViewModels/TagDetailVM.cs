using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	
		public class TagDetailVM
		{
			public int TagId { get; set; }
			public string TagName { get; set; } = string.Empty;
			public bool IsActive { get; set; }
			public string? Revisor { get; set; }
			public DateTime? RevisedDate { get; set; }
			public List<string> BoundPages { get; set; } = new();
		}
	

}
