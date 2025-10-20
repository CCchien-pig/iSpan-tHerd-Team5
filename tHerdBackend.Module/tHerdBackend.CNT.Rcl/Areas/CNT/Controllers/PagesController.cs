using tHerdBackend.CNT.Rcl.Areas.CNT.Services;
using tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels;
using tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using tHerdBackend.CNT.Rcl.Helpers;
using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using X.PagedList;
using X.PagedList.Extensions;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[Area("CNT")]
	public class PagesController : Controller
	{
		private readonly tHerdDBContext _db;
		private readonly PageDeletionService _pageDeletionService;
		private const int HomePageTypeId = 1000;

		public PagesController(tHerdDBContext db)
		{
			_db = db;
			_pageDeletionService = new PageDeletionService(_db);
		}

		// ======================================
		// ✅ 共用：處理 PublishedDate (發佈 / 草稿)
		// ======================================
		private void HandlePublishedDate(CntPage pageEntity, PageEditVM model)
		{
			// 如果設定為「已發佈」，且之前未發佈 → 設為現在
			if (model.Status == PageStatus.Published)
			{
				if (pageEntity.PublishedDate == null)
					pageEntity.PublishedDate = DateTime.Now;
			}
			// 如果改為「草稿」 → 清空 PublishedDate (依你選擇)
			else if (model.Status == PageStatus.Draft)
			{
				pageEntity.PublishedDate = null;
			}
		}

		// ================================
		// 共用方法：狀態下拉選單
		// ================================
		private IEnumerable<SelectListItem> GetStatusSelectList(PageStatus? selected = null, bool includeAll = false, bool includeDeleted = false)
		{
			var items = Enum.GetValues(typeof(PageStatus))
				.Cast<PageStatus>()
				.Where(s => includeDeleted || s != PageStatus.Deleted)
				.Select(s => new SelectListItem
				{
					Text = s switch
					{
						PageStatus.Draft => "草稿",
						PageStatus.Published => "已發佈",
						PageStatus.Archived => "封存",
						PageStatus.Deleted => "刪除",
						_ => "未知"
					},
					Value = ((int)s).ToString(),
					Selected = selected.HasValue && s == selected.Value
				}).ToList();

			if (includeAll)
				items.Insert(0, new SelectListItem("全部狀態", "", selected == null));

			return items;
		}

		// ================================
		// 共用方法：分類下拉選單
		// ================================
		private IEnumerable<SelectListItem> GetPageTypeSelectList(int? selected = null, bool includeAll = false)
		{
			var items = _db.CntPageTypes
				.OrderBy(pt => pt.TypeName)
				.Select(pt => new SelectListItem
				{
					Text = pt.TypeName,
					Value = pt.PageTypeId.ToString(),
					Selected = selected.HasValue && pt.PageTypeId == selected.Value
				}).ToList();

			if (includeAll)
				items.Insert(0, new SelectListItem("全部分類", "", !selected.HasValue));

			return items;
		}

		// ================================
		// 共用方法：保留 QueryString 狀態
		// ================================
		private (int? page, int pageSize, string? keyword, string? status, int? pageTypeId)
			GetListState(int defaultPageSize = 10)
		{
			var q = Request?.Query;
			int? page = null;

			if (int.TryParse(q?["page"], out var pageParsed) && pageParsed > 0)
				page = pageParsed;

			var pageSize = defaultPageSize;
			if (int.TryParse(q?["pageSize"], out var sizeParsed) && sizeParsed > 0)
				pageSize = sizeParsed;

			var keyword = q?["keyword"].ToString();
			var status = q?["status"].ToString();

			int? pageTypeId = null;
			if (int.TryParse(q?["pageTypeId"], out var typeParsed) && typeParsed > 0)
				pageTypeId = typeParsed;

			return (page, pageSize, keyword, status, pageTypeId);
		}

		// ======================================
		// 文章列表 (Index)
		// ======================================
		public IActionResult Index(int? page, string keyword, string status, int pageSize = 10, int? pageTypeId = null)
		{
			int pageNumber = page ?? 1;
			pageSize = (pageSize <= 0) ? 8 : pageSize;

			var query = _db.CntPages.Where(p => p.Status != ((int)PageStatus.Deleted).ToString());

			// 🔍 關鍵字
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				if (int.TryParse(keyword, out int idValue))
					query = query.Where(p => p.PageId == idValue || p.Title.Contains(keyword));
				else
					query = query.Where(p => p.Title.Contains(keyword));
			}

			// 🎯 狀態
			if (!string.IsNullOrWhiteSpace(status))
				query = query.Where(p => p.Status == status);

			// 🗂 分類
			if (pageTypeId.HasValue && pageTypeId.Value > 0)
				query = query.Where(p => p.PageTypeId == pageTypeId.Value);

			var pages = query
				.OrderByDescending(p => p.CreatedDate)
				.Select(p => new PageListVM
				{
					PageId = p.PageId,
					Title = p.Title,
					Status = (PageStatus)int.Parse(p.Status),
					CreatedDate = p.CreatedDate,
					RevisedDate = p.RevisedDate,
					PageTypeName = _db.CntPageTypes
								.Where(pt => pt.PageTypeId == p.PageTypeId)
								.Select(pt => pt.TypeName)
								.FirstOrDefault() ?? "未知類別"
				});

			// ViewBag for 篩選 UI
			ViewBag.StatusList = new SelectList(GetStatusSelectList(null, includeAll: true), "Value", "Text", status);
			ViewBag.PageTypeList = new SelectList(GetPageTypeSelectList(pageTypeId, includeAll: true), "Value", "Text", pageTypeId);
			ViewBag.PageSizeList = new SelectList(new[] { 5, 10, 20, 50, 100 }, pageSize);

			return View(pages.ToPagedList(pageNumber, pageSize));
		}
		// ======================================
		// 新增 (Create) GET
		// ======================================
		public IActionResult Create()
		{
			var vm = new PageEditVM
			{
				Status = PageStatus.Draft,
				StatusList = GetStatusSelectList(PageStatus.Draft),
				TagOptions = new MultiSelectList(
					_db.CntTags.Where(t => t.IsActive).ToList(),
					"TagId", "TagName"
				),
				Blocks = new List<CntPageBlock>(),
				HasSchedule = false
			};

			PreparePageEditVM(vm);
			return View(vm);
		}

		// ======================================
		// 新增 (Create) POST
		// ======================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(PageEditVM model, int? page, int pageSize = 10)
		{
			if (model.IsHomePage)
			{
				ModelState.Remove(nameof(model.SelectedTagIds));
			}

			// 驗證排程
			if (model.HasSchedule)
			{
				if (!model.ActionType.HasValue)
					ModelState.AddModelError(nameof(model.ActionType), "請選擇排程動作");

				if (!model.ScheduledDate.HasValue)
					ModelState.AddModelError(nameof(model.ScheduledDate), "請輸入排程時間");
			}

			if (!ModelState.IsValid)
			{
				PreparePageEditVM(model);
				return View(model);
			}

			using var tx = _db.Database.BeginTransaction();
			try
			{
				var pageEntity = new CntPage
				{
					Title = model.Title,
					Status = ((int)model.Status).ToString(),
					PageTypeId = model.PageTypeId,
					CreatedDate = DateTime.Now,
					RevisedDate = null
				};

				// ✅ 發佈日期處理
				HandlePublishedDate(pageEntity, model);

				_db.CntPages.Add(pageEntity);
				_db.SaveChanges();

				// 處理排程 (Schedule)
				if (model.HasSchedule && model.ActionType.HasValue && model.ScheduledDate.HasValue)
				{
					var scheduleService = new ScheduleService(_db);
					if (!scheduleService.TryUpsert(new PageEditVM
					{
						PageId = pageEntity.PageId,
						ActionType = model.ActionType,
						ScheduledDate = model.ScheduledDate
					}, out var error))
					{
						throw new InvalidOperationException(error);
					}
				}
				else
				{
					new ScheduleService(_db).ClearAll(pageEntity.PageId);
				}

				// 處理 Tags
				if (model.SelectedTagIds?.Any() == true)
				{
					foreach (var tagId in model.SelectedTagIds)
					{
						_db.CntPageTags.Add(new CntPageTag
						{
							PageId = pageEntity.PageId,
							TagId = tagId,
							CreatedDate = DateTime.Now
						});
					}
				}

				_db.SaveChanges();
				tx.Commit();

				TempData["Msg"] = "文章已建立";

				var (qPage, qSize, qKeyword, qStatus, qPageTypeId) = GetListState();
				return RedirectToAction(nameof(Index), new
				{
					page = model.Page ?? qPage,
					pageSize = (model.PageSize > 0 ? model.PageSize : qSize),
					keyword = string.IsNullOrWhiteSpace(model.Keyword) ? qKeyword : model.Keyword,
					status = string.IsNullOrWhiteSpace(model.StatusFilter) ? qStatus : model.StatusFilter
				});
			}
			catch (Exception ex)
			{
				tx.Rollback();
				ModelState.AddModelError("", $"建立失敗：{ex.Message}");
				PreparePageEditVM(model);
				return View(model);
			}
		}

		// ======================================
		// 編輯 (Edit) GET
		// ======================================
		public IActionResult Edit(int id)
		{
			var page = _db.CntPages
				.Include(p => p.CntPageBlocks)
				.FirstOrDefault(p => p.PageId == id && p.Status != "9");

			if (page == null) return NotFound();

			var selectedTagIds = _db.CntPageTags
				.Where(pt => pt.PageId == id)
				.Select(pt => pt.TagId)
				.ToList();

			var vm = new PageEditVM
			{
				PageId = page.PageId,
				Title = page.Title,
				Status = (PageStatus)int.Parse(page.Status),
				RevisedDate = page.RevisedDate,
				StatusList = GetStatusSelectList((PageStatus)int.Parse(page.Status)),
				SelectedTagIds = selectedTagIds,
				TagOptions = new MultiSelectList(
					_db.CntTags.Where(t => t.IsActive),
					"TagId", "TagName", selectedTagIds
				),
				Blocks = page.CntPageBlocks.OrderBy(b => b.OrderSeq).ToList(),
				PageTypeId = page.PageTypeId
			};

			var existingSchedule = _db.CntSchedules
				.Where(s => s.PageId == id)
				.OrderByDescending(s => s.ScheduledDate)
				.FirstOrDefault();

			if (existingSchedule != null)
			{
				vm.HasSchedule = true;
				vm.ActionType = int.TryParse(existingSchedule.ActionType, out var atInt)
					? (ActionType)atInt
					: null;
				vm.ScheduledDate = existingSchedule.ScheduledDate;
			}
			else
			{
				vm.HasSchedule = false;
			}

			PreparePageEditVM(vm);
			return View(vm);
		}

		// ======================================
		// 編輯 (Edit) POST
		// ======================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(PageEditVM model, int? page, int pageSize = 10)
		{
			if (model.IsHomePage)
			{
				ModelState.Remove(nameof(model.SelectedTagIds));
			}

			if (model.HasSchedule)
			{
				if (!model.ActionType.HasValue)
					ModelState.AddModelError(nameof(model.ActionType), "請選擇排程動作");

				if (!model.ScheduledDate.HasValue)
					ModelState.AddModelError(nameof(model.ScheduledDate), "請輸入排程時間");
			}

			if (!ModelState.IsValid)
			{
				PreparePageEditVM(model);
				return View(model);
			}

			using var tx = _db.Database.BeginTransaction();
			try
			{
				var pageEntity = _db.CntPages.FirstOrDefault(p => p.PageId == model.PageId);
				if (pageEntity == null) return NotFound();

				pageEntity.Title = model.Title;
				pageEntity.Status = ((int)model.Status).ToString();
				pageEntity.PageTypeId = model.PageTypeId == HomePageTypeId ? HomePageTypeId : model.PageTypeId;
				pageEntity.RevisedDate = DateTime.Now;

				// ✅ 處理 PublishedDate
				HandlePublishedDate(pageEntity, model);

				// 更新排程
				if (model.HasSchedule && model.ActionType.HasValue && model.ScheduledDate.HasValue)
				{
					var scheduleService = new ScheduleService(_db);
					if (!scheduleService.TryUpsert(model, out var error))
						throw new InvalidOperationException(error);
				}
				else
				{
					new ScheduleService(_db).ClearAll(pageEntity.PageId);
				}

				// 更新 Tags
				var oldTags = _db.CntPageTags.Where(pt => pt.PageId == pageEntity.PageId);
				_db.CntPageTags.RemoveRange(oldTags);

				if (model.SelectedTagIds?.Any() == true)
				{
					foreach (var tagId in model.SelectedTagIds)
					{
						_db.CntPageTags.Add(new CntPageTag
						{
							PageId = pageEntity.PageId,
							TagId = tagId,
							CreatedDate = DateTime.Now
						});
					}
				}

				_db.SaveChanges();
				tx.Commit();

				TempData["Msg"] = "文章修改成功";

				var (qPage, qSize, qKeyword, qStatus, qPageTypeId) = GetListState();
				return RedirectToAction(nameof(Details), new
				{
					id = model.PageId,
					page = model.Page ?? qPage,
					pageSize = (model.PageSize > 0 ? model.PageSize : qSize),
					keyword = string.IsNullOrWhiteSpace(model.Keyword) ? qKeyword : model.Keyword,
					status = string.IsNullOrWhiteSpace(model.StatusFilter) ? qStatus : model.StatusFilter
				});
			}
			catch (Exception ex)
			{
				tx.Rollback();
				ModelState.AddModelError("", $"修改失敗：{ex.Message}");
				PreparePageEditVM(model);
				return View(model);
			}
		}

		// ======================================
		// 重建下拉選單 & 區塊
		// ======================================
		private void PreparePageEditVM(PageEditVM model)
		{
			model.StatusList = GetStatusSelectList(model.Status);

			model.TagOptions = new MultiSelectList(
				_db.CntTags.Where(t => t.IsActive).ToList(),
				"TagId", "TagName", model.SelectedTagIds
			);

			model.Blocks ??= _db.CntPageBlocks
				.Where(b => b.PageId == model.PageId)
				.OrderBy(b => b.OrderSeq).ToList();

			ViewBag.PageTypeList = new SelectList(
										GetPageTypeSelectList(model.PageTypeId),
										"Value",
										"Text",
										model.PageTypeId
									);


			ViewBag.ActionTypeList = new SelectList(
				Enum.GetValues(typeof(ActionType))
					.Cast<ActionType>()
					.Select(a => new { Value = (int)a, Text = a.ToDisplayName() }),
				"Value", "Text", model.ActionType
			);
		}

		// ======================================
		// 詳細頁 Details
		// ======================================
		public IActionResult Details(int id, int? page, int pageSize = 10, string? keyword = null, string? status = null)
		{
			var pageEntity = _db.CntPages
				.Include(p => p.CntPageBlocks)
				.FirstOrDefault(p => p.PageId == id && p.Status != "9");

			if (pageEntity == null) return NotFound();

			var tagNames = (from pt in _db.CntPageTags
							join t in _db.CntTags on pt.TagId equals t.TagId
							where pt.PageId == id
							select t.TagName).ToList();

			var schedules = _db.CntSchedules
				.Where(s => s.PageId == id)
				.OrderBy(s => s.ScheduledDate)
				.Select(s => new ScheduleVM
				{
					ScheduleId = s.ScheduleId,
					ScheduledDate = s.ScheduledDate,
					ActionTypeRaw = s.ActionType,
					StatusRaw = s.Status
				})
				.ToList();

			var vm = new PageDetailVM
			{
				PageId = pageEntity.PageId,
				Title = pageEntity.Title,
				Status = (PageStatus)int.Parse(pageEntity.Status),
				CreatedDate = pageEntity.CreatedDate,
				RevisedDate = pageEntity.RevisedDate,
				PageTypeId = pageEntity.PageTypeId,
				PageTypeName = _db.CntPageTypes
					 .Where(pt => pt.PageTypeId == pageEntity.PageTypeId)
					 .Select(pt => pt.TypeName)
					 .FirstOrDefault() ?? "未知類別",
				TagNames = tagNames,
				Blocks = pageEntity.CntPageBlocks.OrderBy(b => b.OrderSeq).ToList(),
				Schedules = schedules,
				Page = page,
				PageSize = pageSize,
				Keyword = keyword,
				StatusFilter = status
			};

			return View(vm);
		}

		// ======================================
		// 軟刪除 (Delete)
		// ======================================
		public IActionResult Delete(int id)
		{
			var page = _db.CntPages
				.Where(p => p.PageId == id)
				.Select(p => new
				{
					p.PageId,
					p.Title,
					p.Status,
					p.RevisedDate,
					p.PageTypeId,
					PageTypeName = p.PageType.TypeName
				})
				.FirstOrDefault();

			if (page == null) return NotFound();

			if (page.PageTypeId == HomePageTypeId)
				return BadRequest("首頁不能刪除");

			var vm = new PageEditVM
			{
				PageId = page.PageId,
				Title = page.Title,
				Status = (PageStatus)int.Parse(page.Status),
				RevisedDate = page.RevisedDate,
				PageTypeId = page.PageTypeId,
				PageTypeName = page.PageTypeName ?? "未分類"
			};

			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Delete(PageEditVM model, int? page, int pageSize = 10)
		{
			var pageEntity = _db.CntPages.Find(model.PageId);
			if (pageEntity == null) return NotFound();

			if (pageEntity.PageTypeId == HomePageTypeId)
				return BadRequest("首頁不能刪除");

			pageEntity.Status = ((int)PageStatus.Deleted).ToString();
			pageEntity.RevisedDate = DateTime.Now;

			new ScheduleService(_db).ClearAll(pageEntity.PageId);
			_db.SaveChanges();

			TempData["Msg"] = "文章已移到回收桶";

			var (qPage, qSize, qKeyword, qStatus, qPageTypeId) = GetListState();

			return RedirectToAction(nameof(Index), new
			{
				page = model.Page ?? qPage,
				pageSize = (model.PageSize > 0 ? model.PageSize : qSize),
				keyword = string.IsNullOrWhiteSpace(model.Keyword) ? qKeyword : model.Keyword,
				status = string.IsNullOrWhiteSpace(model.StatusFilter) ? qStatus : model.StatusFilter,
				pageTypeId = qPageTypeId
			});
		}

		// ======================================
		// 回收桶 (RecycleBin)
		// ======================================
		public IActionResult RecycleBin(
			int? page,
			string? keyword,
			int pageSize = 10,
			string? status = null,
			int? pageTypeId = null)
		{
			int pageNumber = Math.Max(page ?? 1, 1);
			pageSize = pageSize > 0 ? pageSize : 10;

			var query = _db.CntPages
				.Where(p => p.Status == ((int)PageStatus.Deleted).ToString());

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				keyword = keyword.Trim();
				if (int.TryParse(keyword, out int idValue))
					query = query.Where(p => p.PageId == idValue || p.Title.Contains(keyword));
				else
					query = query.Where(p => p.Title.Contains(keyword));
			}

			if (pageTypeId.HasValue && pageTypeId > 0)
				query = query.Where(p => p.PageTypeId == pageTypeId.Value);

			var deletedPages = query
				.OrderByDescending(p => p.RevisedDate ?? p.CreatedDate)
				.Select(p => new PageListVM
				{
					PageId = p.PageId,
					Title = p.Title,
					Status = (PageStatus)int.Parse(p.Status),
					CreatedDate = p.CreatedDate,
					RevisedDate = p.RevisedDate,
					PageTypeName = p.PageType.TypeName
				});

			ViewBag.Keyword = keyword;
			ViewBag.Status = status;
			ViewBag.PageTypeId = pageTypeId;
			ViewBag.PageSizeList = new SelectList(new[] { 5, 10, 20, 50, 100 }, pageSize);

			var pageTypeOptions = _db.CntPageTypes
				.Select(pt => new { pt.PageTypeId, pt.TypeName })
				.ToList();

			pageTypeOptions.Insert(0, new { PageTypeId = 0, TypeName = "全部分類" });

			ViewBag.PageTypeList = new SelectList(pageTypeOptions, "PageTypeId", "TypeName", pageTypeId ?? 0);

			ViewBag.StatusList = new SelectList(new[]
			{
				new { Value = "", Text = "全部" },
				new { Value = "deleted", Text = "已刪除" }
			}, "Value", "Text", status);

			return View(deletedPages.ToPagedList(pageNumber, pageSize));
		}

		// ======================================
		// 復原 (Restore)
		// ======================================
		public IActionResult Restore(int id, int? page, int pageSize = 10, string? keyword = null, string? status = null)
		{
			var pageEntity = _db.CntPages.Find(id);
			if (pageEntity == null) return NotFound();

			pageEntity.Status = ((int)PageStatus.Draft).ToString();
			pageEntity.RevisedDate = DateTime.Now;
			pageEntity.PublishedDate = null; // ✅ 草稿 → 清空日期

			_db.SaveChanges();
			TempData["Msg"] = "文章已復原";

			var (qPage, qSize, qKeyword, qStatus, qPageTypeId) = GetListState();

			return RedirectToAction(nameof(RecycleBin), new
			{
				page = page ?? qPage,
				pageSize = qSize,
				keyword = string.IsNullOrWhiteSpace(keyword) ? qKeyword : keyword,
				status = string.IsNullOrWhiteSpace(status) ? qStatus : status
			});
		}

		// ======================================
		// 永久刪除 (Destroy)
		// ======================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Destroy(int id, int? page, int pageSize = 10, string? keyword = null, string? status = null)
		{
			var (qPage, qSize, qKeyword, qStatus, qPageTypeId) = GetListState();

			if (!_pageDeletionService.PermanentlyDeletePage(id, out var error))
			{
				TempData["Error"] = error;
				return RedirectToAction(nameof(RecycleBin), new
				{
					page = page ?? qPage,
					pageSize = qSize,
					keyword = string.IsNullOrWhiteSpace(keyword) ? qKeyword : keyword,
					status = string.IsNullOrWhiteSpace(status) ? qStatus : status
				});
			}

			return RedirectToAction(nameof(RecycleBin), new
			{
				page = page ?? qPage,
				pageSize = qSize,
				keyword = string.IsNullOrWhiteSpace(keyword) ? qKeyword : keyword,
				status = string.IsNullOrWhiteSpace(status) ? qStatus : status
			});
		}
	}
}

