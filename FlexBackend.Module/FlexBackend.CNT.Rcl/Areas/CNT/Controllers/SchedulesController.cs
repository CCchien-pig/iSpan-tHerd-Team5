using FlexBackend.CNT.Rcl.Areas.CNT.Services;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
			string? scheduleStatus,
			string? actionType,
			int pageSize = 10,
			int? pageTypeId = null,
			string? pageStatus = null,
			string sortField = "ScheduledDate",
			string sortOrder = "asc"
		)
		{
			int pageNumber = Math.Max(page ?? 1, 1);
			pageSize = pageSize > 0 ? pageSize : 10;

			var query = _db.CntSchedules
				.Include(s => s.Page)
				.ThenInclude(p => p.PageType)
				.AsQueryable();

			// ======================
			// 篩選條件
			// ======================
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				var kw = keyword.Trim();
				query = query.Where(s => s.Page.Title.Contains(kw));
			}
			if (pageTypeId.HasValue && pageTypeId.Value > 0)
			{
				query = query.Where(s => s.Page.PageTypeId == pageTypeId.Value);
			}
			if (!string.IsNullOrWhiteSpace(pageStatus))
			{
				query = query.Where(s => s.Page.Status == pageStatus);
			}
			if (!string.IsNullOrWhiteSpace(scheduleStatus))
			{
				query = query.Where(s => s.Status == scheduleStatus);
			}
			if (!string.IsNullOrWhiteSpace(actionType))
			{
				query = query.Where(s => s.ActionType == actionType);
			}

			// ======================
			// 排序
			// ======================
			query = sortField switch
			{
				"PageTitle" => (sortOrder == "asc" ? query.OrderBy(s => s.Page.Title) : query.OrderByDescending(s => s.Page.Title)),
				"PageTypeName" => (sortOrder == "asc" ? query.OrderBy(s => s.Page.PageType.TypeName) : query.OrderByDescending(s => s.Page.PageType.TypeName)),
				"PageStatus" => (sortOrder == "asc" ? query.OrderBy(s => s.Page.Status) : query.OrderByDescending(s => s.Page.Status)),
				"ActionType" => (sortOrder == "asc" ? query.OrderBy(s => s.ActionType) : query.OrderByDescending(s => s.ActionType)),
				"ScheduledDate" => (sortOrder == "asc" ? query.OrderBy(s => s.ScheduledDate) : query.OrderByDescending(s => s.ScheduledDate)),
				"Status" => (sortOrder == "asc" ? query.OrderBy(s => s.Status) : query.OrderByDescending(s => s.Status)),
				_ => query.OrderBy(s => s.ScheduledDate)
			};

			// ======================
			// 分頁 + 投影
			// ======================
			var projected = query.Select(s => new
			{
				s.ScheduleId,
				s.PageId,
				PageTitle = s.Page.Title,
				PageTypeName = s.Page.PageType.TypeName,
				PageStatus = s.Page.Status,
				s.ActionType,
				s.ScheduledDate,
				s.Status
			});

			var pageRaw = projected.ToPagedList(pageNumber, pageSize);

			var currentItems = pageRaw.Select(s => new ScheduleListVM
			{
				ScheduleId = s.ScheduleId,
				PageId = s.PageId,
				PageTitle = s.PageTitle,
				PageTypeName = s.PageTypeName,
				PageStatus = int.TryParse(s.PageStatus, out var psInt) ? psInt : 0,
				ActionType = int.TryParse(s.ActionType, out var atInt) ? (ActionType)atInt : ActionType.PublishPage,
				ScheduledDate = s.ScheduledDate,
				Status = int.TryParse(s.Status, out var stInt) ? (ScheduleStatus)stInt : ScheduleStatus.Pending
			}).ToList();

			var model = new StaticPagedList<ScheduleListVM>(
				currentItems, pageRaw.PageNumber, pageRaw.PageSize, pageRaw.TotalItemCount);

			// ======================
			// 下拉清單
			// ======================
			ViewBag.PageSizeList = new SelectList(new[] { 5, 10, 20, 50, 100 }, pageSize);

			var pageTypes = _db.CntPageTypes
				.OrderBy(pt => pt.TypeName)
				.Select(pt => new { pt.PageTypeId, pt.TypeName })
				.ToList();
			pageTypes.Insert(0, new { PageTypeId = 0, TypeName = "全部分類" });
			ViewBag.PageTypeList = new SelectList(pageTypes, "PageTypeId", "TypeName", pageTypeId ?? 0);

			var pageStatusList = new List<SelectListItem>
				{
					new() { Value = "", Text = "全部選項" },
					new() { Value = "0", Text = "草稿" },
					new() { Value = "1", Text = "已發布" },
					new() { Value = "2", Text = "封存" },
					new() { Value = "9", Text = "刪除" }
				};
			ViewBag.PageStatusList = new SelectList(pageStatusList, "Value", "Text", pageStatus);

			var scheduleStatusItems = Enum.GetValues(typeof(ScheduleStatus))
				.Cast<ScheduleStatus>()
				.Select(s => new SelectListItem
				{
					Value = ((int)s).ToString(),
					Text = s.GetDisplayName()
				}).ToList();
			scheduleStatusItems.Insert(0, new SelectListItem { Value = "", Text = "全部選項" });
			ViewBag.ScheduleStatusList = new SelectList(scheduleStatusItems, "Value", "Text", scheduleStatus);

			var actionItems = Enum.GetValues(typeof(ActionType))
				.Cast<ActionType>()
				.Where(a => a != ActionType.ClearAllSchedules)
				.Select(a => new SelectListItem
				{
					Value = ((int)a).ToString(),
					Text = a.GetDisplayName()
				}).ToList();
			actionItems.Insert(0, new SelectListItem { Value = "", Text = "全部選項" });
			ViewBag.ActionTypeList = new SelectList(actionItems, "Value", "Text", actionType);

			// ======================
			// 篩選條件列 (Badge)
			// ======================
			var activeFilters = new List<(string Label, string Value, string RemoveUrl)>();

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				activeFilters.Add((
					"關鍵字", keyword,
					Url.Action("Index", new { pageSize, pageTypeId, pageStatus, actionType, scheduleStatus, sortField, sortOrder })
				));
			}
			if (pageTypeId.HasValue && pageTypeId.Value > 0)
			{
				var typeName = _db.CntPageTypes.FirstOrDefault(pt => pt.PageTypeId == pageTypeId.Value)?.TypeName ?? "-";
				activeFilters.Add((
					"分類", typeName,
					Url.Action("Index", new { pageSize, keyword, pageStatus, actionType, scheduleStatus, sortField, sortOrder })
				));
			}
			if (!string.IsNullOrWhiteSpace(pageStatus))
			{
				string statusText = pageStatus switch
				{
					"0" => "草稿",
					"1" => "已發布",
					"2" => "封存",
					"9" => "刪除",
					_ => pageStatus
				};
				activeFilters.Add((
					"狀態", statusText,
					Url.Action("Index", new { pageSize, keyword, pageTypeId, actionType, scheduleStatus, sortField, sortOrder })
				));
			}
			if (!string.IsNullOrWhiteSpace(actionType) && int.TryParse(actionType, out var atInt))
			{
				var at = (ActionType)atInt;
				activeFilters.Add((
					"預約動作", at.GetDisplayName(),
					Url.Action("Index", new { pageSize, keyword, pageTypeId, pageStatus, scheduleStatus, sortField, sortOrder })
				));
			}
			if (!string.IsNullOrWhiteSpace(scheduleStatus) && int.TryParse(scheduleStatus, out var stInt))
			{
				var st = (ScheduleStatus)stInt;
				activeFilters.Add((
					"排程執行狀態", st.GetDisplayName(),
					Url.Action("Index", new { pageSize, keyword, pageTypeId, pageStatus, actionType, sortField, sortOrder })
				));
			}

			ViewBag.ActiveFilters = activeFilters;

			// ======================
			// 回填用
			// ======================
			ViewBag.Keyword = keyword;
			ViewBag.ScheduleStatus = scheduleStatus;
			ViewBag.PageTypeId = pageTypeId;
			ViewBag.ActionType = actionType;
			ViewBag.PageStatus = pageStatus;
			ViewBag.SortField = sortField;
			ViewBag.SortOrder = sortOrder;

			return View(model);
		}

		// ================================
		// AJAX 更新時間
		// ================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult UpdateTime([FromBody] UpdateTimeVM model)
		{
			var schedule = _db.CntSchedules.FirstOrDefault(s => s.ScheduleId == model.Id);
			if (schedule == null)
				return Json(new { success = false, message = "找不到排程" });

			// 包裝成 PageEditVM (因為驗證器吃 PageEditVM)
			var vm = new PageEditVM
			{
				PageId = schedule.PageId,
				ActionType = int.TryParse(schedule.ActionType, out var atInt)
								? (ActionType)atInt
								: ActionType.PublishPage,
				ScheduledDate = model.ScheduledDate
			};

			var validator = new ScheduleValidator(_db);
			if (!validator.ValidateSchedule(vm, out var errorMsg))
			{
				return Json(new { success = false, message = errorMsg });
			}

			// 通過驗證 → 更新時間
			schedule.ScheduledDate = model.ScheduledDate;
			_db.SaveChanges();

			return Json(new { success = true });
		}


		// ================================
		// 編輯 (GET)
		// ================================
		public IActionResult Edit(int id,
			int? page,
			int pageSize = 10,
			string? keyword = null,
			string? scheduleStatus = null,
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
			ViewBag.ScheduleStatus = scheduleStatus;
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
				string? scheduleStatus = null,
				string? actionType = null,
				int? pageTypeId = null)
		{
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

			var service = new ScheduleService(_db);
			if (!service.TryUpsert(new PageEditVM
			{
				PageId = model.PageId,
				ActionType = model.ActionType,
				ScheduledDate = model.ScheduledDate
			}, out var errorMsg))
			{
				TempData["Error"] = errorMsg;
				return RedirectToAction(nameof(Edit), new { id = model.ScheduleId, page, pageSize, keyword, scheduleStatus, actionType, pageTypeId });
			}

			TempData["Msg"] = "排程已更新";
			return RedirectToAction(nameof(Index), new { page, pageSize, keyword, scheduleStatus, actionType, pageTypeId });
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
			var service = new ScheduleService(_db);
			if (!service.DeleteSchedule(id, out var errorMsg))
			{
				TempData["Error"] = errorMsg;
				return RedirectToAction(nameof(Index));
			}

			TempData["Msg"] = "排程已刪除";
			return RedirectToAction(nameof(Index));
		}
	}
}
