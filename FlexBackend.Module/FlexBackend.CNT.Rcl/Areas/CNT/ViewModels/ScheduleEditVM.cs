using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class ScheduleEditVM
	{
		public int ScheduleId { get; set; }
		public int PageId { get; set; }

		[BindNever, ValidateNever]
		public string? PageTitle { get; set; }

		[BindNever, ValidateNever]
		public string? PageTypeName { get; set; }

		[Display(Name = "文章動作")]
		[Required(ErrorMessage = "{0} 必選")]
		public ActionType ActionType { get; set; }

		[Display(Name = "預定時間")]
		[Required(ErrorMessage = "{0} 必填")]
		public DateTime ScheduledDate { get; set; }

		[BindNever, ValidateNever]
		public ScheduleStatus Status { get; set; }

		// 顯示用
		[ValidateNever]
		public string StatusText => Status.GetDisplayName();

		[ValidateNever]
		public string ActionTypeText => ActionType.GetDisplayName();

		// badge 樣式
		[ValidateNever]
		public string ActionBadgeClass => ActionType switch
		{
			ActionType.PublishPage => "bg-success",
			ActionType.UnpublishPage => "bg-secondary",
			ActionType.Featured => "bg-purple",
			ActionType.Unfeatured => "bg-warning text-dark",
			ActionType.PublishCoupon => "bg-pink",
			_ => "bg-dark"
		};

		[ValidateNever]
		public string StatusBadgeClass => Status switch
		{
			ScheduleStatus.Pending => "bg-warning text-dark",
			ScheduleStatus.Processing => "bg-primary",
			ScheduleStatus.Done => "bg-success",
			ScheduleStatus.Failed => "bg-danger",
			_ => "bg-secondary"
		};
	}
}
