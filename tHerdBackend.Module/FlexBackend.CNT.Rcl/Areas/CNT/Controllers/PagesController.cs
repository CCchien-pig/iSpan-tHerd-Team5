using FlexBackend.CNT.Rcl.Areas.CNT.Services;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.CNT.Rcl.Helpers;// ⭐ 引用共用下拉 Helper//尚未實作修改程式碼引用
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using X.PagedList;
using X.PagedList.Extensions;
using System.Globalization;

namespace FlexBackend.CNT.Rcl.Areas.CNT.Controllers
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
			_pageDeletionService = new PageDeletionService(_db); // ⭐ 直接 new
		}

		// ================================
		// 共用方法：產生 PageType 下拉清單
		// ================================
		private SelectList GetPageTypeSelectList(int pageTypeId)
		{
			if (pageTypeId == HomePageTypeId)
			{
				// 如果是首頁 → 只顯示首頁
				return new SelectList(
					new[] { new { PageTypeId = HomePageTypeId, TypeName = "首頁" } },
					"PageTypeId", "TypeName", HomePageTypeId
				);
			}
			else
			{
				// 如果不是首頁 → 顯示其他類別（排除首頁）
				return new SelectList(
					_db.CntPageTypes.Where(pt => pt.PageTypeId != HomePageTypeId),
					"PageTypeId", "TypeName", pageTypeId
				);
			}
		}

		// ================================
		// 共用方法：狀態下拉選單
		// ================================
		private IEnumerable<SelectListItem> GetStatusSelectList(PageStatus? selected = null, bool includeAll = false, bool includeDeleted = false)
		{
			var items = Enum.GetValues(typeof(PageStatus))
				.Cast<PageStatus>()
				 .Where(s => includeDeleted || s != PageStatus.Deleted) // ⭐ 排除刪除
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
			{
				items.Insert(0, new SelectListItem("全部狀態", "", selected == null));
			}
			return items;
		}
		// ================================
		// 共用方法：狀態下拉選單
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
				})
				.ToList();

			// ⭐ 如果要包含「全部分類」
			if (includeAll)
			{
				items.Insert(0, new SelectListItem("全部分類", "", !selected.HasValue));
			}

			return items;
		}

		// ================================
		// 共用方法：讀 QueryString 列表狀態（含分類 pageTypeId）
		// ================================
		private (int? page, int pageSize, string? keyword, string? status, int? pageTypeId)
			GetListState(int defaultPageSize = 10)
		{
			var q = Request?.Query;

			// 頁碼
			int? page = null;
			if (int.TryParse(q?["page"], out var pageParsed) && pageParsed > 0)
				page = pageParsed;

			// 每頁筆數
			var pageSize = defaultPageSize;
			if (int.TryParse(q?["pageSize"], out var sizeParsed) && sizeParsed > 0)
				pageSize = sizeParsed;

			// 關鍵字
			var keyword = q?["keyword"].ToString();

			// 狀態
			var status = q?["status"].ToString();

			// 分類
			int? pageTypeId = null;
			if (int.TryParse(q?["pageTypeId"], out var typeParsed) && typeParsed > 0)
				pageTypeId = typeParsed;

			return (page, pageSize, keyword, status, pageTypeId);
		}

		// ================================
		// 文章列表 (Index)
		// ================================
		public IActionResult Index(int? page, string keyword, string status, int pageSize = 10, int? pageTypeId = null)
		{
			int pageNumber = page ?? 1;
			pageSize = (pageSize <= 0) ? 8 : pageSize;

			var query = _db.CntPages.Where(p => p.Status != ((int)PageStatus.Deleted).ToString());

			// 🔍 關鍵字搜尋
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				if (int.TryParse(keyword, out int idValue))
					query = query.Where(p => p.PageId == idValue || p.Title.Contains(keyword));
				else
					query = query.Where(p => p.Title.Contains(keyword));
			}

			// 📌 狀態篩選
			if (!string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(p => p.Status == status);
			}

			// 📂 分類篩選
			if (pageTypeId.HasValue && pageTypeId.Value > 0)
			{
				query = query.Where(p => p.PageTypeId == pageTypeId.Value);
			}

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

			// ================================
			// 下拉選單資料
			// ================================
			ViewBag.StatusList = new SelectList(
				GetStatusSelectList(null, includeAll: true, includeDeleted: false),
				"Value", "Text", status);

			ViewBag.PageTypeList = new SelectList(
				GetPageTypeSelectList(pageTypeId, includeAll: true),
				"Value", "Text", pageTypeId);

			ViewBag.PageSizeList = new SelectList(new[] { 5, 10, 20, 50, 100 }, pageSize);

			// ================================
			// 顯示用：目前篩選條件（中文）
			// ================================

			// 狀態中文
			if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int statusInt))
			{
				var statusEnum = (PageStatus)statusInt;
				ViewBag.StatusName = statusEnum switch
				{
					PageStatus.Draft => "草稿",
					PageStatus.Published => "已發佈",
					PageStatus.Archived => "封存",
					PageStatus.Deleted => "刪除",
					_ => "未知"
				};
			}
			else
			{
				ViewBag.StatusName = null;
			}

			// 分類中文
			if (pageTypeId.HasValue && pageTypeId.Value > 0)
			{
				ViewBag.PageTypeName = _db.CntPageTypes
					.Where(pt => pt.PageTypeId == pageTypeId.Value)
					.Select(pt => pt.TypeName)
					.FirstOrDefault();
			}
			else
			{
				ViewBag.PageTypeName = null;
			}

			// 關鍵字
			ViewBag.Keyword = keyword;

			return View(pages.ToPagedList(pageNumber, pageSize));
		}



		// ================================
		// 新增 (Create)
		// ================================
		public IActionResult Create()
		{
			var vm = new PageEditVM
			{
				Status = PageStatus.Draft,
				StatusList = GetStatusSelectList(PageStatus.Draft),
				// ⭐ 提供所有可選標籤（剛新增所以沒有已選）
				TagOptions = new MultiSelectList(
				_db.CntTags.Where(t => t.IsActive).ToList(),
				"TagId", "TagName"
				),
				Blocks = new List<CntPageBlock>()
			};
			// ✅ 改成呼叫共用方法，避免重複
			PreparePageEditVM(vm);

			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(PageEditVM model, int? page, int pageSize = 10)
		{
			if (!ModelState.IsValid)
			{
				// 🔄 驗證失敗 → 重建 ViewModel 所需資料
				PreparePageEditVM(model);
				return View(model);
			}

			// ✅ 建立 Page 實體
			var pageEntity = new CntPage
			{
				Title = model.Title,
				Status = ((int)model.Status).ToString(),
				PageTypeId = model.PageTypeId,
				CreatedDate = DateTime.Now,
				RevisedDate = null
			};

			_db.CntPages.Add(pageEntity);
			_db.SaveChanges();

			// ==== 處理排程 ====
			var validator = new ScheduleValidator(_db);

			// 「一次取消所有排程」
			if (model.ActionType == ActionType.ClearAllSchedules)
			{
				var all = _db.CntSchedules.Where(s => s.PageId == pageEntity.PageId);
				_db.CntSchedules.RemoveRange(all);
				_db.SaveChanges();
			}
			else if (model.ActionType.HasValue && model.ScheduledDate.HasValue)
			{
				if (!validator.ValidateSchedule(model, out string error))
				{
					ModelState.AddModelError("", error);
					// 還原表單所需資料
					PreparePageEditVM(model);
					return View(model);
				}

				// Upsert：同動作有就更新，沒有就新增
				var actCode = ((int)model.ActionType.Value).ToString();
				var existing = _db.CntSchedules.FirstOrDefault(s => s.PageId == pageEntity.PageId && s.ActionType == actCode);

				if (existing != null)
				{
					existing.ScheduledDate = model.ScheduledDate.Value;
					existing.Status = ((int)ScheduleStatus.Pending).ToString();
				}
				else
				{
					_db.CntSchedules.Add(new CntSchedule
					{
						PageId = pageEntity.PageId,
						ActionType = actCode,
						ScheduledDate = model.ScheduledDate.Value,
						Status = ((int)ScheduleStatus.Pending).ToString()
					});
				}
				_db.SaveChanges();
			}

			// ✅ 建立關聯的 Tags
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
				_db.SaveChanges();
			}

			TempData["Msg"] = "文章已建立";

			// 讀取目前 QueryString 的列表狀態
			var (qPage, qSize, qKeyword, qStatus, qPageTypeId) = GetListState();

			return RedirectToAction(nameof(Index), new
			{
				// 以 model 為主，否則回退 QueryString，再回退預設
				page = model.Page ?? qPage,
				pageSize = (model.PageSize > 0 ? model.PageSize : qSize),
				keyword = string.IsNullOrWhiteSpace(model.Keyword) ? qKeyword : model.Keyword,
				status = string.IsNullOrWhiteSpace(model.StatusFilter) ? qStatus : model.StatusFilter
			});
		}

		// ================================
		// 編輯 (Edit)
		// ================================
		public IActionResult Edit(int id)
		{
			var page = _db.CntPages
				.Include(p => p.CntPageBlocks)
				.FirstOrDefault(p => p.PageId == id && p.Status != "9");

			if (page == null) return NotFound();

			// 查找該 Page 已有的 TagId
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
				PageTypeId = page.PageTypeId   // ⭐ 加上這行
			};

			// ✅ 使用共用方法
			PreparePageEditVM(vm);
			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(PageEditVM model, int? page, int pageSize = 10)
		{
			// ===============================
			// 標籤驗證邏輯
			// ===============================
			if (model.PageTypeId == HomePageTypeId)
			{
				model.SelectedTagIds ??= new List<int>();
				ModelState.Remove(nameof(model.SelectedTagIds));
			}
			else if (model.SelectedTagIds == null || !model.SelectedTagIds.Any())
			{
				ModelState.AddModelError(nameof(model.SelectedTagIds), "請至少選一個標籤");
			}

			// ===============================
			// 驗證失敗 → 回傳原本畫面
			// ===============================
			if (!ModelState.IsValid)
			{
				PreparePageEditVM(model);
				return View(model);
			}

			// ===============================
			// 更新資料庫
			// ===============================
			var pageEntity = _db.CntPages.FirstOrDefault(p => p.PageId == model.PageId);
			if (pageEntity == null) return NotFound();

			// ===============================
			// 更新 Page 資料
			// ===============================
			pageEntity.Title = model.Title;
			pageEntity.Status = ((int)model.Status).ToString(CultureInfo.InvariantCulture);
			pageEntity.RevisedDate = DateTime.Now;

			// ===============================
			// PageType 更新
			// ===============================
			if (model.PageTypeId == HomePageTypeId)
			{
				// 強制保持首頁類別
				pageEntity.PageTypeId = HomePageTypeId;
			}
			else
			{
				// ✅ 非首頁才允許修改分類
				pageEntity.PageTypeId = model.PageTypeId;
			}

			// ======= 這裡不再「全部清空」舊排程 =======
			var validator = new ScheduleValidator(_db);

			// 「一次取消所有排程」
			if (model.ActionType == ActionType.ClearAllSchedules)
			{
				var all = _db.CntSchedules.Where(s => s.PageId == pageEntity.PageId);
				_db.CntSchedules.RemoveRange(all);
				_db.SaveChanges();
			}
			else if (model.ActionType.HasValue && model.ScheduledDate.HasValue)
			{
				// 檢查「上架 < 精選 < 取消精選 < 下架」鏈條
				if (!validator.ValidateSchedule(model, out string error))
				{
					ModelState.AddModelError("", error);
					PreparePageEditVM(model);
					return View(model);
				}

				// Upsert：同動作有就更新，沒有就新增
				var actCode = ((int)model.ActionType.Value).ToString();
				var existing = _db.CntSchedules.FirstOrDefault(s => s.PageId == pageEntity.PageId && s.ActionType == actCode);

				if (existing != null)
				{
					existing.ScheduledDate = model.ScheduledDate.Value;
					existing.Status = ((int)ScheduleStatus.Pending).ToString();
				}
				else
				{
					_db.CntSchedules.Add(new CntSchedule
					{
						PageId = pageEntity.PageId,
						ActionType = actCode,
						ScheduledDate = model.ScheduledDate.Value,
						Status = ((int)ScheduleStatus.Pending).ToString()
					});
				}
				_db.SaveChanges();
			}

			// 先清舊標籤 → 再重建
			var oldTags = _db.CntPageTags.Where(pt => pt.PageId == pageEntity.PageId);
			_db.CntPageTags.RemoveRange(oldTags);
			if (model.SelectedTagIds != null && model.SelectedTagIds.Any())
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

		// ================================
		// 共用方法：重建下拉 & 標籤 & Blocks
		// ================================
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

			// ✅ 使用共用方法
			ViewBag.PageTypeList = GetPageTypeSelectList(model.PageTypeId);
		}

		// ================================
		// 詳細頁面 (Details)
		// ================================
		public IActionResult Details(int id, int? page, int pageSize = 10, string? keyword = null, string? status = null)
			{
			var pageEntity = _db.CntPages
				.Include(p => p.CntPageBlocks) // 撈文章區塊
				.FirstOrDefault(p => p.PageId == id && p.Status != "9"); // ⭐ 排除已刪除

			if (pageEntity == null) return NotFound();

			// ⭐ 撈取標籤
			var tagNames = (from pt in _db.CntPageTags
							join t in _db.CntTags on pt.TagId equals t.TagId
							where pt.PageId == id
							select t.TagName).ToList();

			// ⭐ 撈取排程
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


		// ================================
		// 刪除 (軟刪除 → 回收桶)
		// ================================
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
					PageTypeName = p.PageType.TypeName // ⚡ 如果有外鍵關聯 CNT_PageType
				})
				.FirstOrDefault();

			if (page == null) return NotFound();

			if (page.PageTypeId == HomePageTypeId)
			{
				return BadRequest("首頁不能刪除");
			}

			var vm = new PageEditVM
			{
				PageId = page.PageId,
				Title = page.Title,
				Status = (PageStatus)int.Parse(page.Status),
				RevisedDate = page.RevisedDate,
				PageTypeId = page.PageTypeId,
				PageTypeName = page.PageTypeName ?? "未分類" // ⚡ 加上名稱
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
			{
				return BadRequest("首頁不能刪除");
			}

			pageEntity.Status = ((int)PageStatus.Deleted).ToString();
			pageEntity.RevisedDate = DateTime.Now;

			_db.SaveChanges();
			TempData["Msg"] = "文章已移到回收桶";
			var (qPage, qSize, qKeyword, qStatus, qPageTypeId) = GetListState();

			return RedirectToAction(nameof(Index), new
			{
				page = model.Page ?? qPage,
				pageSize = (model.PageSize > 0 ? model.PageSize : qSize),
				keyword = string.IsNullOrWhiteSpace(model.Keyword) ? qKeyword : model.Keyword,
				status = string.IsNullOrWhiteSpace(model.StatusFilter) ? qStatus : model.StatusFilter
			});
		}
		// ================================
		// 回收桶列表 (RecycleBin)
		// ================================
		public IActionResult RecycleBin(
			int? page,
			string? keyword,
			int pageSize = 10,
			string? status = null,
			int? pageTypeId = null)
		{
			int pageNumber = Math.Max(page ?? 1, 1);
			pageSize = pageSize > 0 ? pageSize : 10;

			// ✅ 固定只顯示已刪除文章
			var query = _db.CntPages
				.Where(p => p.Status == ((int)PageStatus.Deleted).ToString());

			// 🔍 關鍵字搜尋
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				keyword = keyword.Trim();
				if (int.TryParse(keyword, out int idValue))
					query = query.Where(p => p.PageId == idValue || p.Title.Contains(keyword));
				else
					query = query.Where(p => p.Title.Contains(keyword));
			}

			// 🔍 分類 (PageType) 篩選
			if (pageTypeId.HasValue && pageTypeId > 0)
			{
				query = query.Where(p => p.PageTypeId == pageTypeId.Value);
			}

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

			// ✅ 保留條件到 ViewBag，方便 UI 回填
			ViewBag.Keyword = keyword;
			ViewBag.Status = status;
			ViewBag.PageTypeId = pageTypeId;

			// 頁數選單
			ViewBag.PageSizeList = new SelectList(new[] { 5, 10, 20, 50, 100 }, pageSize);

			// 分類下拉（加「全部分類」選項）
			var pageTypeOptions = _db.CntPageTypes
				.Select(pt => new { pt.PageTypeId, pt.TypeName })
				.ToList();

			pageTypeOptions.Insert(0, new { PageTypeId = 0, TypeName = "全部分類" });

			ViewBag.PageTypeList = new SelectList(pageTypeOptions, "PageTypeId", "TypeName", pageTypeId ?? 0);

			// ✅ 狀態下拉：提供「全部」和「已刪除」
			ViewBag.StatusList = new SelectList(new[]
			{
		new { Value = "", Text = "全部" },
		new { Value = "deleted", Text = "已刪除" }
	}, "Value", "Text", status);

			return View(deletedPages.ToPagedList(pageNumber, pageSize));
		}

		// ================================
		// 復原 (Restore)
		// ================================
		public IActionResult Restore(int id, int? page, int pageSize = 10, string? keyword = null, string? status = null)
		{
			var pageEntity = _db.CntPages.Find(id);
			if (pageEntity == null) return NotFound();

			pageEntity.Status = ((int)PageStatus.Draft).ToString();
			pageEntity.RevisedDate = DateTime.Now;

			_db.SaveChanges();
			TempData["Msg"] = "文章已復原";

			// 讀取 QueryString 狀態
			var (qPage, qSize, qKeyword, qStatus, qPageTypeId) = GetListState();

			return RedirectToAction(nameof(RecycleBin), new
			{
				page = page ?? qPage,
				pageSize = qSize,
				keyword = string.IsNullOrWhiteSpace(keyword) ? qKeyword : keyword,
				status = string.IsNullOrWhiteSpace(status) ? qStatus : status
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Destroy(int id, int? page, int pageSize = 10, string? keyword = null, string? status = null)
		{
			// 先取得目前 QueryString 狀態
			var (qPage, qSize, qKeyword, qStatus, qPageTypeId) = GetListState();

			if (!_pageDeletionService.PermanentlyDeletePage(id, out var error))
			{
				// ❌ 有錯誤：放進 TempData → 回列表頁顯示
				TempData["Error"] = error;
				return RedirectToAction(nameof(RecycleBin), new
				{
					page = page ?? qPage,
					pageSize = qSize,
					keyword = string.IsNullOrWhiteSpace(keyword) ? qKeyword : keyword,
					status = string.IsNullOrWhiteSpace(status) ? qStatus : status
				});
			}

			// ✅ 改這裡：成功刪除後 → 回 RecycleBin，而不是 Index
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
