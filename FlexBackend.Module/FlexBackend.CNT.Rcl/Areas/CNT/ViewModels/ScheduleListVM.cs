using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	// ================================
	// ViewModels
	// ================================
	public class ScheduleListVM
	{
		public int ScheduleId { get; set; }
		public int PageId { get; set; }
		public string PageTitle { get; set; }
		public string PageTypeName { get; set; }
		public ActionType ActionType { get; set; }
		public DateTime ScheduledDate { get; set; }
		public ScheduleStatus Status { get; set; }
	}
}
