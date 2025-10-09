using tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels;
using tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using tHerdBackend.Infra.Models;
using System.Linq;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.Services
{
	public class ScheduleValidator
	{
		private readonly tHerdDBContext _db;
		public ScheduleValidator(tHerdDBContext db) => _db = db;

		public bool ValidateSchedule(PageEditVM model, out string errorMsg)
		{
			errorMsg = string.Empty;

			// 特例
			if (model.ActionType == ActionType.ClearAllSchedules)
				return true;

			// 必填
			if (!model.ScheduledDate.HasValue || !model.ActionType.HasValue)
			{
				errorMsg = "請選擇排程時間與動作";
				return false;
			}

			var pageId = model.PageId;
			var incomingAt = model.ScheduledDate!.Value;
			var incoming = model.ActionType!.Value;

			// 時間需大於現在(UTC，容許1分鐘誤差)
			if (incomingAt <= DateTime.UtcNow.AddMinutes(-1))
			{
				errorMsg = "排程時間必須大於現在時間";
				return false;
			}

			// 文章狀態
			var page = _db.CntPages.FirstOrDefault(p => p.PageId == pageId);
			if (page == null)
			{
				errorMsg = "文章不存在";
				return false;
			}
			if (!int.TryParse(page.Status, out var pageStatus))
			{
				errorMsg = "文章狀態資料錯誤";
				return false;
			}
			if (pageStatus == 9)
			{
				errorMsg = "文章已刪除，不能設定排程";
				return false;
			}
			if (pageStatus == 3)
			{
				errorMsg = "文章已封存，不能設定排程。請改回草稿後再操作";
				return false;
			}

			// 允許下架狀態排「非上架」動作，但必須先有上架排程且上架時間在本次動作之前
			if (pageStatus == 2 && incoming != ActionType.PublishPage)
			{
				DateTime? pub = _db.CntSchedules
					.Where(s => s.PageId == pageId
							 && s.Status != ((int)ScheduleStatus.Failed).ToString()
							 && s.ActionType == ((int)ActionType.PublishPage).ToString())
					.Select(s => (DateTime?)s.ScheduledDate)   // ✅ cast 成 nullable
					.FirstOrDefault();

				if (!pub.HasValue || !(pub.Value < incomingAt))
				{
					errorMsg = "下架文章需先安排上架，且本次動作時間必須晚於上架時間";
					return false;
				}
			}

			// 取現有排程（排除 Failed）
			var schedules = _db.CntSchedules
				.Where(s => s.PageId == pageId && s.Status != ((int)ScheduleStatus.Failed).ToString())
				.ToList();

			// 同動作僅可一筆（允許已存在1筆，視為更新；>1才擋）
			string incomingCode = ((int)incoming).ToString();
			int sameActionCount = schedules.Count(s => s.ActionType == incomingCode);
			if (sameActionCount > 1)
			{
				errorMsg = $"{incoming.GetDisplayName()} 已存在多筆排程，請先清理或合併後再設定";
				return false;
			}

			// 既有時間
			DateTime? pubAt = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.PublishPage).ToString())?.ScheduledDate;
			DateTime? featAt = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.Featured).ToString())?.ScheduledDate;
			DateTime? unfeatAt = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.Unfeatured).ToString())?.ScheduledDate;
			DateTime? unpubAt = schedules.FirstOrDefault(s => s.ActionType == ((int)ActionType.UnpublishPage).ToString())?.ScheduledDate;

			switch (incoming)
			{
				case ActionType.PublishPage:
					if (pageStatus == 1)
					{
						errorMsg = "文章已是發布狀態，不能再排程發布";
						return false;
					}
					// 若已有下架，發布時間要早於下架
					if (unpubAt.HasValue && !(incomingAt < unpubAt.Value))
					{
						errorMsg = "上架時間必須早於下架時間";
						return false;
					}
					pubAt = incomingAt;
					break;
				// TODO  有擴充下架狀態可以用
				case ActionType.UnpublishPage:
					if (pageStatus == 2)
					{
						errorMsg = "文章已經下架，不能再排程下架";
						return false;
					}
					// 草稿但沒有 Publish → 不允許
					if (pageStatus == 0 && !pubAt.HasValue)
					{
						errorMsg = "草稿文章若要下架，必須先有發布排程";
						return false;
					}
					// ✅ 有精選就必須「先取消精選」，且取消精選 < 下架
					if (featAt.HasValue && (!unfeatAt.HasValue || !(unfeatAt.Value < incomingAt)))
					{
						errorMsg = "已設定精選，請先安排取消精選，且取消精選時間必須早於下架時間";
						return false;
					}
					// 上架 < 下架
					if (pubAt.HasValue && !(pubAt.Value < incomingAt))
					{
						errorMsg = "下架時間必須晚於上架時間";
						return false;
					}
					unpubAt = incomingAt;
					break;

				case ActionType.Featured:
					// 必須已發布或已安排發布
					if (!pubAt.HasValue && pageStatus != 1)
					{
						errorMsg = "必須先設定上架時間或文章已發布，才能精選";
						return false;
					}
					// 若已有取消精選，精選時間要早於取消精選
					if (unfeatAt.HasValue && !(incomingAt < unfeatAt.Value))
					{
						errorMsg = "精選時間必須早於取消精選時間";
						return false;
					}
					// 若已有下架，精選要早於下架
					if (unpubAt.HasValue && !(incomingAt < unpubAt.Value))
					{
						errorMsg = "精選時間必須早於下架時間";
						return false;
					}
					featAt = incomingAt;
					break;

				case ActionType.Unfeatured:
					// ✅ 僅修這段：必須已有精選，且取消精選在精選之後、下架之前
					if (!featAt.HasValue)
					{
						errorMsg = "必須先設定精選時間，才能取消精選";
						return false;
					}
					if (!(featAt.Value < incomingAt))
					{
						errorMsg = "取消精選時間必須晚於精選時間";
						return false;
					}
					if (unpubAt.HasValue && !(incomingAt < unpubAt.Value))
					{
						errorMsg = "取消精選時間必須早於下架時間";
						return false;
					}
					unfeatAt = incomingAt;
					break;

				case ActionType.PublishCoupon:
					if (pageStatus == 3 || pageStatus == 9)
					{
						errorMsg = "封存或刪除的文章不能發布優惠券";
						return false;
					}
					break;
			}

			// ── 最後做一次基礎時序鏈檢查（保險絲） ──
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

			// ✅ 注意：這裡**移除了**原本那段「(featAt && unpubAt) && (!unfeatAt || unfeatAt >= unpubAt)」的全域檢查，
			// 以免在「排精選」時也被要求一定要同時有取消精選。

			return true;
		}
	}
}
