using tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using tHerdBackend.CNT.Rcl.Helpers;
using System;
using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class ScheduleVM
	{
		public int ScheduleId { get; set; }
		public DateTime ScheduledDate { get; set; }

		// 原始值（DB varchar）
		public string ActionTypeRaw { get; set; } = string.Empty;
		public string StatusRaw { get; set; } = string.Empty;

		// Nullable Enum（安全轉換）
		public ActionType? ActionTypeEnum =>
			int.TryParse(ActionTypeRaw, out int v) ? (ActionType?)v : null;

		public ScheduleStatus? StatusEnum =>
			int.TryParse(StatusRaw, out int v) ? (ScheduleStatus?)v : null;

		// 給 Razor 用的文字顯示
		public string ActionTypeText =>
			ActionTypeEnum.HasValue ? EnumHelper.GetDisplayName(ActionTypeEnum.Value) : "未知";

		public string StatusText =>
			StatusEnum.HasValue ? EnumHelper.GetDisplayName(StatusEnum.Value) : "未知";
	}

}
