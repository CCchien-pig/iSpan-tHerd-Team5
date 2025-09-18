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
		/// 驗證排程邏輯：
		/// 1. 不允許同動作有多筆（除非 Controller 是 Upsert 覆蓋）
		/// 2. 僅檢查「上架 < 精選 < 取消精選 < 下架」的時間鏈是否成立
		/// 3. 精選/取消精選：必須已存在上架時間
		/// 4. ClearAllSchedules：永遠通過
		/// 5. 狀態檢查：依文章當前狀態判斷是否允許排程
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
			var actCode = ((int)incomingAct).ToString();

			// 撈取 Page 狀態
			var page = _db.CntPages.FirstOrDefault(p => p.PageId == pageId);
			if (page == null)
			{
				errorMsg = "文章不存在";
				return false;
			}

			// 文章狀態是 string，要轉成 int
			if (!int.TryParse(page.Status, out var pageStatus))
			{
				errorMsg = "文章狀態資料錯誤";
				return false;
			}
			// 0=草稿, 1=已發布, 2=下架, 9=刪除

			// === 🛑 狀態檢查 ===
			switch (incomingAct)
			{
				case ActionType.PublishPage:
					if (pageStatus == 1)
					{
						errorMsg = "文章已經是發布狀態，不能再排程發布";
						return false;
					}
					if (pageStatus == 9)
					{
						errorMsg = "文章已刪除，無法排程發布";
						return false;
					}
					break;

				case ActionType.UnpublishPage:
					if (pageStatus == 0)
					{
						errorMsg = "草稿文章不能下架";
						return false;
					}
					if (pageStatus == 2)
					{
						errorMsg = "文章已經下架，不能再排程下架";
						return false;
					}
					if (pageStatus == 9)
					{
						errorMsg = "文章已刪除，不能排程下架";
						return false;
					}
					break;

				case ActionType.Featured:
				case ActionType.Unfeatured:
					if (pageStatus != 1)
					{
						errorMsg = "只有已發布的文章才能精選/取消精選";
						return false;
					}
					break;
			}

			// 撈取目前已存在的排程（排除失敗的）
			var schedules = _db.CntSchedules
				.Where(s => s.PageId == pageId && s.Status != ((int)ScheduleStatus.Failed).ToString())
				.ToList();

			// 🛑 檢查：同動作最多一筆（除非 Controller 是 Upsert 覆蓋）
			var duplicates = schedules.Count(s => s.ActionType == actCode);
			if (duplicates > 1)
			{
				errorMsg = $"{incomingAct} 動作已存在多筆，請修改或刪除舊排程";
				return false;
			}

			// 取各動作現有的時間
			var publish = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.PublishPage).ToString());
			var featured = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.Featured).ToString());
			var unfeatured = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.Unfeatured).ToString());
			var unpublish = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.UnpublishPage).ToString());

			// 形成「候選」時間（把本次的設定套進去）
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

			// 依序檢查鏈條：上架 < 精選 < 取消精選 < 下架
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
			if (pubAt.HasValue && unpubAt.HasValue && !(pubAt.Value < unpubAt.Value))
			{
				errorMsg = "下架時間必須晚於上架時間";
				return false;
			}

			return true;
		}
	}
}
