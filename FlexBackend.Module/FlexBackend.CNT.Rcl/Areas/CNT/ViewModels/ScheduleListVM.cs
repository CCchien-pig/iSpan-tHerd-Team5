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

		// ✅ 文章狀態 (數字)
		public int PageStatus { get; set; }

		// ✅ 文章狀態文字
		public string PageStatusText => PageStatus switch
		{
			0 => "草稿",
			1 => "已發布",
			2 => "封存",
			9 => "刪除",
			_ => "未知"
		};

		// ✅ 文章狀態顏色 (badge class)
		public string PageStatusBadgeClass => PageStatus switch
		{
			0 => "bg-secondary",         // 草稿 → 灰色
			1 => "bg-success",           // 已發布 → 綠色
			2 => "bg-warning text-dark", // 封存 → 黃色
			9 => "bg-danger",            // 刪除 → 紅色
			_ => "bg-dark"
		};

		// 顯示文字 / 樣式
		public string ActionTypeText => ActionType switch
		{
			ActionType.Featured => "精選文章",
			ActionType.PublishPage => "發布文章",
			ActionType.UnpublishPage => "下架文章",
			ActionType.Unfeatured => "取消精選",
			ActionType.PublishCoupon => "發布優惠券",
			ActionType.ClearAllSchedules => "清空所有排程",
			_ => "其他"
		};

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
