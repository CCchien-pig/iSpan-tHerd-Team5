using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using X.PagedList;
using X.PagedList.Extensions;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;

namespace FlexBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[Area("CNT")]
	public class SchedulesController : Controller
	{
		private readonly tHerdDBContext _db;

		public SchedulesController(tHerdDBContext db)
		{
			_db = db;
		}

		// ================================
		// 排程列表 (Index)
		// ================================
		public IActionResult Index(
			int? page,
			string? keyword,
			string? status,
			int pageSize = 10,
			int? pageTypeId = null)
		{
			int pageNumber = Math.Max(page ?? 1, 1);
			pageSize = pageSize > 0 ? pageSize : 10;

			var query = _db.CntSchedules
				.Include(s => s.Page)
				.ThenInclude(p => p.PageType)
				.AsQueryable();

			// 🔍 關鍵字搜尋 (文章標題)
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				query = query.Where(s => s.Page.Title.Contains(keyword));
			}

			// 📌 狀態篩選
			if (!string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(s => s.Status == status);
			}

			// 📂 分類篩選
			if (pageTypeId.HasValue && pageTypeId.Value > 0)
			{
				query = query.Where(s => s.Page.PageTypeId == pageTypeId.Value);
			}

			var schedules = query
				.OrderBy(s => s.ScheduledDate)
				.Select(s => new ScheduleListVM
				{
					ScheduleId = s.ScheduleId,
					PageId = s.PageId,
					PageTitle = s.Page.Title,
					PageTypeName = s.Page.PageType.TypeName,
					ActionType = (ActionType)int.Parse(s.ActionType),
					ScheduledDate = s.ScheduledDate,
					Status = (ScheduleStatus)int.Parse(s.Status)
				});

			// 篩選下拉選單
			ViewBag.PageSizeList = new SelectList(new[] { 5, 10, 20, 50, 100 }, pageSize);

			ViewBag.PageTypeList = new SelectList(
				_db.CntPageTypes.OrderBy(pt => pt.TypeName)
					.Select(pt => new { pt.PageTypeId, pt.TypeName }),
				"PageTypeId", "TypeName", pageTypeId);

			ViewBag.StatusList = new SelectList(
				Enum.GetValues(typeof(ScheduleStatus))
					.Cast<ScheduleStatus>()
					.Select(s => new SelectListItem
					{
						Value = ((int)s).ToString(),
						Text = s switch
						{
							ScheduleStatus.Pending => "待執行",
							ScheduleStatus.Processing => "處理中",
							ScheduleStatus.Done => "完成",
							ScheduleStatus.Failed => "失敗",
							_ => "未知"
						}
					}),
				"Value", "Text", status);

			// 顯示用：目前篩選條件
			ViewBag.Keyword = keyword;
			ViewBag.Status = status;
			ViewBag.PageTypeId = pageTypeId;

			return View(schedules.ToPagedList(pageNumber, pageSize));
		}

		// ================================
		// 編輯 (Edit)
		// ================================
		public IActionResult Edit(int id)
		{
			var schedule = _db.CntSchedules
				.Include(s => s.Page)
				.ThenInclude(p => p.PageType)
				.FirstOrDefault(s => s.ScheduleId == id);

			if (schedule == null) return NotFound();

			var vm = new ScheduleEditVM
			{
				ScheduleId = schedule.ScheduleId,
				PageId = schedule.PageId,
				PageTitle = schedule.Page.Title,
				PageTypeName = schedule.Page.PageType.TypeName,
				ActionType = (ActionType)int.Parse(schedule.ActionType),
				ScheduledDate = schedule.ScheduledDate,
				Status = (ScheduleStatus)int.Parse(schedule.Status)
			};

			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(ScheduleEditVM model, int? page, int pageSize = 10, string? keyword = null, string? status = null, int? pageTypeId = null)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var schedule = _db.CntSchedules.Find(model.ScheduleId);
			if (schedule == null) return NotFound();

			schedule.ActionType = ((int)model.ActionType).ToString(CultureInfo.InvariantCulture);
			schedule.ScheduledDate = model.ScheduledDate;
			schedule.Status = ((int)model.Status).ToString(CultureInfo.InvariantCulture);

			_db.SaveChanges();

			TempData["Msg"] = "排程已更新";

			return RedirectToAction(nameof(Index), new
			{
				page,
				pageSize,
				keyword,
				status,
				pageTypeId
			});
		}
	}
}
