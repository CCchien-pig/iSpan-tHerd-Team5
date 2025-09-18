using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;

// ================================
// Services/ScheduleService.cs
// ================================
namespace FlexBackend.CNT.Rcl.Areas.CNT.Services
{
	public class ScheduleService
	{
		private readonly tHerdDBContext _db;
		private readonly ScheduleValidator _validator;

		public ScheduleService(tHerdDBContext db)
		{
			_db = db;
			_validator = new ScheduleValidator(db);
		}

		/// <summary>
		/// 新增或更新排程（Upsert）
		/// </summary>
		public bool TryUpsert(PageEditVM model, out string errorMsg)
		{
			errorMsg = string.Empty;

			// 1. 驗證
			if (!_validator.ValidateSchedule(model, out errorMsg))
				return false;

			// 2. ClearAll 特例：清空排程
			if (model.ActionType == ActionType.ClearAllSchedules)
			{
				var all = _db.CntSchedules.Where(s => s.PageId == model.PageId);
				_db.CntSchedules.RemoveRange(all);
				_db.SaveChanges();
				return true;
			}

			// 3. 一般 Upsert
			var actCode = ((int)model.ActionType!.Value).ToString();
			var existing = _db.CntSchedules
							  .FirstOrDefault(s => s.PageId == model.PageId && s.ActionType == actCode);

			if (existing != null)
			{
				existing.ScheduledDate = model.ScheduledDate!.Value;
				existing.Status = ((int)ScheduleStatus.Pending).ToString();
			}
			else
			{
				_db.CntSchedules.Add(new CntSchedule
				{
					PageId = model.PageId,
					ActionType = actCode,
					ScheduledDate = model.ScheduledDate!.Value,
					Status = ((int)ScheduleStatus.Pending).ToString()
				});
			}

			_db.SaveChanges();
			return true;
		}

		/// <summary>
		/// 清空指定文章的所有排程
		/// （文章軟刪除時可直接呼叫）
		/// </summary>
		public void ClearAll(int pageId)
		{
			var all = _db.CntSchedules.Where(s => s.PageId == pageId);
			_db.CntSchedules.RemoveRange(all);
			_db.SaveChanges();
		}

		/// <summary>
		/// 永久刪除單一排程
		/// </summary>
		public bool DeleteSchedule(int scheduleId, out string errorMsg)
		{
			errorMsg = string.Empty;

			var schedule = _db.CntSchedules.Find(scheduleId);
			if (schedule == null)
			{
				errorMsg = "找不到指定的排程。";
				return false;
			}

			_db.CntSchedules.Remove(schedule);
			_db.SaveChanges();
			return true;
		}
	}
}
