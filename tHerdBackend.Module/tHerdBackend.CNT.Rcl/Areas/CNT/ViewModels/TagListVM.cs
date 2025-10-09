using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class TagListVM
	{
		public int TagId { get; set; }
		public string TagName { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public string? Revisor { get; set; }
		public DateTime? RevisedDate { get; set; }
	}
}

