using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class ScheduleListVM
	{
		public int ScheduleId { get; set; }
		public int PageId { get; set; }
		public string PageTitle { get; set; } = "";
		public string PageTypeName { get; set; } = "";
		public ActionType ActionType { get; set; }
		public DateTime ScheduledDate { get; set; }
		public ScheduleStatus Status { get; set; }
		// ✅ 新增文章狀態欄位
		public int PageStatus { get; set; }
		public string PageStatusText { get; set; }
		public string PageStatusBadgeClass { get; set; }

		// 顯示文字 / 樣式
		public string ActionTypeText => ActionType switch
		{
			ActionType.PublishPage => "發布文章",
			ActionType.UnpublishPage => "下架文章",
			ActionType.PublishCoupon => "發布優惠券",
			_ => "其他"
		};

		// badge 樣式（與 Index 一致）
		public string ActionBadgeClass => ActionType switch
		{
			ActionType.PublishPage => "bg-success",
			ActionType.UnpublishPage => "bg-secondary",
			ActionType.Featured => "bg-purple",
			ActionType.Unfeatured => "bg-warning text-dark",
			ActionType.PublishCoupon => "bg-pink",
			_ => "bg-dark"
		};

		public string StatusText => Status switch
		{
			ScheduleStatus.Pending => "待執行",
			ScheduleStatus.Processing => "處理中",
			ScheduleStatus.Done => "完成",
			ScheduleStatus.Failed => "失敗",
			_ => "未知"
		};

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
