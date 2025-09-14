using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class TagDeleteVM
	{
		public int TagId { get; set; }
		public string TagName { get; set; }
		public bool IsActive { get; set; }
		public List<string> BoundPages { get; set; } = new();
	}

}
