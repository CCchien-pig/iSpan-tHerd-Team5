using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using System.Linq;

namespace FlexBackend.CNT.Rcl.Areas.CNT.Services
{
	public class ScheduleValidator
	{
		private readonly tHerdDBContext _db;
		public ScheduleValidator(tHerdDBContext db) => _db = db;

		/// <summary>
		/// 僅檢查「上架 < 精選 < 取消精選 < 下架」的時間鏈是否成立。
		/// - 允許多動作並存
		/// - 允許重複設定（視為更新），這裡不做重複擋
		/// - 精選/取消精選：必須已存在上架時間，但不要求下架
		/// - ClearAllSchedules：永遠通過，交由 Controller 處理刪除
		/// </summary>
		public bool ValidateSchedule(PageEditVM model, out string errorMsg)
		{
			errorMsg = string.Empty;

			// ClearAll 不需要時間，直接放行
			if (model.ActionType == ActionType.ClearAllSchedules)
				return true;

			// 其他動作必須要有時間
			if (!model.ScheduledDate.HasValue || !model.ActionType.HasValue)
			{
				errorMsg = "請選擇排程時間與動作";
				return false;
			}

			var pageId = model.PageId;
			var incomingAt = model.ScheduledDate!.Value;
			var incomingAct = model.ActionType!.Value;

			// 取目前資料庫已存在的時間
			var schedules = _db.CntSchedules
				.Where(s => s.PageId == pageId && s.Status != ((int)ScheduleStatus.Failed).ToString())
				.ToList();

			var publish = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.PublishPage).ToString());
			var featured = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.Featured).ToString());
			var unfeatured = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.Unfeatured).ToString());
			var unpublish = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.UnpublishPage).ToString());

			// 形成「候選」時間（把本次的設定套進去後再檢查整體順序）
			DateTime? pubAt = incomingAct == ActionType.PublishPage ? incomingAt : publish?.ScheduledDate;
			DateTime? featAt = incomingAct == ActionType.Featured ? incomingAt : featured?.ScheduledDate;
			DateTime? unfeatAt = incomingAct == ActionType.Unfeatured ? incomingAt : unfeatured?.ScheduledDate;
			DateTime? unpubAt = incomingAct == ActionType.UnpublishPage ? incomingAt : unpublish?.ScheduledDate;

			// 規則：精選 / 取消精選 必須要有「上架」存在
			if ((incomingAct == ActionType.Featured || incomingAct == ActionType.Unfeatured) && !pubAt.HasValue)
			{
				errorMsg = "必須先設定上架時間，才能精選/取消精選";
				return false;
			}

			// 依序檢查鏈條：上架 < 精選 < 取消精選 < 下架（僅在兩端都存在時檢查）
			if (pubAt.HasValue && featAt.HasValue && !(pubAt.Value < featAt.Value))
			{
				errorMsg = "精選時間必須晚於上架時間";
				return false;
			}
			if (featAt.HasValue && unfeatAt.HasValue && !(featAt.Value < unfeatAt.Value))
			{
				errorMsg = "取消精選時間必須晚於精選時間";
				return false;
			}
			if (unfeatAt.HasValue && unpubAt.HasValue && !(unfeatAt.Value < unpubAt.Value))
			{
				errorMsg = "下架時間必須晚於取消精選時間";
				return false;
			}
			// 額外：上架 < 下架（若兩者都有）
			if (pubAt.HasValue && unpubAt.HasValue && !(pubAt.Value < unpubAt.Value))
			{
				errorMsg = "下架時間必須晚於上架時間";
				return false;
			}

			return true;
		}
	}
}
