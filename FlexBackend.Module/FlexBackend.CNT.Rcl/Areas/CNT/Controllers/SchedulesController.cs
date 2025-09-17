using FlexBackend.CNT.Rcl.Areas.CNT.Services;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using X.PagedList;
using X.PagedList.Extensions;

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
			string? actionType,   // 動作篩選 (varchar 存數字字串)
			int pageSize = 10,
			int? pageTypeId = null)
		{
			int pageNumber = Math.Max(page ?? 1, 1);
			pageSize = pageSize > 0 ? pageSize : 10;

			var query = _db.CntSchedules
				.Include(s => s.Page)
				.ThenInclude(p => p.PageType)
				.AsQueryable();

			// 關鍵字（標題）
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				var kw = keyword.Trim();
				query = query.Where(s => s.Page.Title.Contains(kw));
			}

			// 分類
			if (pageTypeId.HasValue && pageTypeId.Value > 0)
			{
				query = query.Where(s => s.Page.PageTypeId == pageTypeId.Value);
			}

			// 狀態（只比對數字字串）
			if (!string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(s => s.Status == status);
			}

			// 動作（只比對數字字串）
			if (!string.IsNullOrWhiteSpace(actionType))
			{
				query = query.Where(s => s.ActionType == actionType);
			}

			// ===== 第一次投影（讓 EF 做分頁）=====
			var projected = query
				.OrderBy(s => s.ScheduledDate)
				.Select(s => new
				{
					s.ScheduleId,
					s.PageId,
					PageTitle = s.Page.Title,
					PageTypeName = s.Page.PageType.TypeName,
					s.ActionType,
					s.ScheduledDate,
					s.Status
				});

			var pageRaw = projected.ToPagedList(pageNumber, pageSize);

			// ===== 第二次投影（轉 Enum 顯示）=====
			var currentItems = pageRaw.Select(s => new ScheduleListVM
			{
				ScheduleId = s.ScheduleId,
				PageId = s.PageId,
				PageTitle = s.PageTitle,
				PageTypeName = s.PageTypeName,
				ActionType = int.TryParse(s.ActionType, out var atInt)
								? (ActionType)atInt
								: ActionType.PublishPage, // 預設
				ScheduledDate = s.ScheduledDate,
				Status = int.TryParse(s.Status, out var stInt)
								? (ScheduleStatus)stInt
								: ScheduleStatus.Pending
			}).ToList();

			var model = new StaticPagedList<ScheduleListVM>(
				currentItems, pageRaw.PageNumber, pageRaw.PageSize, pageRaw.TotalItemCount);

			// 下拉：每頁筆數
			ViewBag.PageSizeList = new SelectList(new[] { 5, 10, 20, 50, 100 }, pageSize);

			// 下拉：分類
			var pageTypes = _db.CntPageTypes
				.OrderBy(pt => pt.TypeName)
				.Select(pt => new { pt.PageTypeId, pt.TypeName })
				.ToList();
			pageTypes.Insert(0, new { PageTypeId = 0, TypeName = "全部分類" });
			ViewBag.PageTypeList = new SelectList(pageTypes, "PageTypeId", "TypeName", pageTypeId ?? 0);

			// 下拉：狀態
			var statusItems = Enum.GetValues(typeof(ScheduleStatus))
				.Cast<ScheduleStatus>()
				.Select(s => new SelectListItem
				{
					Value = ((int)s).ToString(),
					Text = s.GetDisplayName()
				}).ToList();

			statusItems.Insert(0, new SelectListItem { Value = "", Text = "全部狀態" });
			ViewBag.StatusList = new SelectList(statusItems, "Value", "Text", status);
			// 下拉：動作（排除 ClearAllSchedules）
			var actionItems = Enum.GetValues(typeof(ActionType))
				.Cast<ActionType>()
				.Where(a => a != ActionType.ClearAllSchedules) // 🚫 排除
				.Select(a => new SelectListItem
				{
					Value = ((int)a).ToString(),
					Text = a.GetDisplayName()
				}).ToList();

			actionItems.Insert(0, new SelectListItem { Value = "", Text = "全部動作" });
			ViewBag.ActionTypeList = new SelectList(actionItems, "Value", "Text", actionType);

			// 回填篩選
			ViewBag.Keyword = keyword;
			ViewBag.Status = status;
			ViewBag.PageTypeId = pageTypeId;
			ViewBag.ActionType = actionType;

			return View(model);
		}

		// ================================
		// 編輯 (GET) → 顯示表單
		// ================================
		public IActionResult Edit(int id,
			int? page,
			int pageSize = 10,
			string? keyword = null,
			string? status = null,
			string? actionType = null,
			int? pageTypeId = null)
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
				PageTypeName = schedule.Page.PageType?.TypeName ?? "-",
				ActionType = int.TryParse(schedule.ActionType, out var atInt)
								? (ActionType)atInt
								: ActionType.PublishPage,
				ScheduledDate = schedule.ScheduledDate,
				Status = int.TryParse(schedule.Status, out var stInt)
								? (ScheduleStatus)stInt
								: ScheduleStatus.Pending
			};

			// 下拉：排除 ClearAllSchedules
			ViewBag.ActionTypeList = Enum.GetValues(typeof(ActionType))
				.Cast<ActionType>()
				.Where(a => a != ActionType.ClearAllSchedules)
				.Select(a => new SelectListItem
				{
					Value = ((int)a).ToString(),
					Text = a.GetDisplayName()
				}).ToList();

			// 保留回列表的篩選條件
			ViewBag.Page = page;
			ViewBag.PageSize = pageSize;
			ViewBag.Keyword = keyword;
			ViewBag.Status = status;
			ViewBag.ActionType = actionType;
			ViewBag.PageTypeId = pageTypeId;

			return View(vm);
		}

		// ================================
		// 編輯 (POST)
		// ================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(ScheduleEditVM model,
	int? page, int pageSize = 10,
	string? keyword = null,
	string? status = null,
	string? actionType = null,
	int? pageTypeId = null)
		{
			// 不參與驗證/繫結的欄位，先移出 ModelState（雙保險）
			ModelState.Remove(nameof(ScheduleEditVM.PageTitle));
			ModelState.Remove(nameof(ScheduleEditVM.PageTypeName));
			ModelState.Remove(nameof(ScheduleEditVM.Status));
			ModelState.Remove(nameof(ScheduleEditVM.StatusText));
			ModelState.Remove(nameof(ScheduleEditVM.ActionTypeText));
			ModelState.Remove(nameof(ScheduleEditVM.ActionBadgeClass));
			ModelState.Remove(nameof(ScheduleEditVM.StatusBadgeClass));

			if (!ModelState.IsValid)
			{
				ViewBag.ActionTypeList = Enum.GetValues(typeof(ActionType))
					.Cast<ActionType>()
					.Where(a => a != ActionType.ClearAllSchedules)
					.Select(a => new SelectListItem { Value = ((int)a).ToString(), Text = a.GetDisplayName() })
					.ToList();

				return View(model);
			}

			// ScheduleValidator 驗證
			var validator = new ScheduleValidator(_db);
			if (!validator.ValidateSchedule(new PageEditVM
			{
				PageId = model.PageId,
				ActionType = model.ActionType,
				ScheduledDate = model.ScheduledDate
			}, out var errorMsg))
			{
				TempData["Error"] = errorMsg;
				return RedirectToAction(nameof(Edit), new { id = model.ScheduleId, page, pageSize, keyword, status, actionType, pageTypeId });
			}

			var schedule = _db.CntSchedules.Find(model.ScheduleId);
			if (schedule == null) return NotFound();

			schedule.ActionType = ((int)model.ActionType).ToString(CultureInfo.InvariantCulture);
			schedule.ScheduledDate = model.ScheduledDate;
			_db.SaveChanges();

			TempData["Msg"] = "排程已更新";
			return RedirectToAction(nameof(Index), new { page, pageSize, keyword, status, actionType, pageTypeId });
		}

		// ================================
		// 詳細（Details）
		// ================================
		public IActionResult Details(
			int id,
			int? page,
			int pageSize = 10,
			string? keyword = null,
			string? status = null,
			string? actionType = null,
			int? pageTypeId = null)
		{
			var s = _db.CntSchedules
				.Include(x => x.Page)
				.ThenInclude(p => p.PageType)
				.Where(x => x.ScheduleId == id)
				.Select(x => new
				{
					x.ScheduleId,
					x.PageId,
					PageTitle = x.Page.Title,
					PageTypeName = x.Page.PageType.TypeName,
					x.ActionType,
					x.ScheduledDate,
					x.Status
				})
				.FirstOrDefault();

			if (s == null) return NotFound();

			var vm = new ScheduleListVM
			{
				ScheduleId = s.ScheduleId,
				PageId = s.PageId,
				PageTitle = s.PageTitle,
				PageTypeName = s.PageTypeName,
				ActionType = int.TryParse(s.ActionType, out var atInt)
								  ? (ActionType)atInt
								  : ActionType.PublishPage,
				ScheduledDate = s.ScheduledDate,
				Status = int.TryParse(s.Status, out var stInt)
								  ? (ScheduleStatus)stInt
								  : ScheduleStatus.Pending
			};

			// 回列表要用到的 QueryString
			ViewBag.Page = page;
			ViewBag.PageSize = pageSize;
			ViewBag.Keyword = keyword;
			ViewBag.Status = status;
			ViewBag.ActionType = actionType;
			ViewBag.PageTypeId = pageTypeId;

			return View(vm);
		}
		// ================================
		// 刪除 (GET) → 顯示確認頁面
		// ================================
		public IActionResult Delete(int id)
		{
			var schedule = _db.CntSchedules
				.Include(s => s.Page)
				.ThenInclude(p => p.PageType)
				.FirstOrDefault(s => s.ScheduleId == id);

			if (schedule == null) return NotFound();

			var vm = new ScheduleListVM
			{
				ScheduleId = schedule.ScheduleId,
				PageId = schedule.PageId,
				PageTitle = schedule.Page.Title,
				PageTypeName = schedule.Page.PageType?.TypeName ?? "-",
				ScheduledDate = schedule.ScheduledDate,
				ActionType = int.TryParse(schedule.ActionType, out var atInt)
								? (ActionType)atInt
								: ActionType.PublishPage,
				Status = int.TryParse(schedule.Status, out var stInt)
								? (ScheduleStatus)stInt
								: ScheduleStatus.Pending
			};

			return View(vm);
		}

		// ================================
		// 刪除 (POST) → 真正刪掉
		// ================================
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public IActionResult DeleteConfirmed(int id)
		{
			var schedule = _db.CntSchedules.Find(id);
			if (schedule == null) return NotFound();

			_db.CntSchedules.Remove(schedule);
			_db.SaveChanges();

			TempData["Msg"] = "排程已刪除";
			return RedirectToAction(nameof(Index));
		}

	}
}
