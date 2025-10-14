using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums
{
	public enum PageStatus
	{
		Draft = 0,      // 草稿
		Published = 1,  // 已發布
		Archived = 2,   // 下架/封存
		Deleted = 9     // 刪除
	}

	public enum ActionType
	{
		[Display(Name = "精選文章")]
		Featured = 0,

		[Display(Name = "發布文章")]
		PublishPage = 1,

		[Display(Name = "下架文章")]
		UnpublishPage = 2,

		[Display(Name = "取消精選")]
		Unfeatured = 3,

		[Display(Name = "發布優惠券")]
		PublishCoupon = 4,

		[Display(Name = "清空所有排程")]
		ClearAllSchedules = 99
	}

	public enum ScheduleStatus
	{
		[Display(Name = "待執行")]
		Pending = 0,

		[Display(Name = "執行中")]
		Processing = 1,

		[Display(Name = "完成")]
		Done = 2,

		[Display(Name = "失敗")]
		Failed = 9
	}
}
