using FlexBackend.CNT.Rcl.Areas.CNT.Services;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using X.PagedList;
using X.PagedList.Extensions;

namespace FlexBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[Area("CNT")]
	public class PagesController : Controller
	{
		private readonly tHerdDBContext _db;
		private const int HomePageTypeId = 1000; // 統一首頁 PageTypeId

		public PagesController(tHerdDBContext db)
		{
			_db = db;
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
		// 文章列表 (Index)
		// ================================
		public IActionResult Index(int? page, string keyword, string status, int pageSize = 8)
		{
			int pageNumber = page ?? 1;
			pageSize = (pageSize <= 0) ? 8 : pageSize; // 避免傳 0

			var query = _db.CntPages.Where(p => p.Status != ((int)PageStatus.Deleted).ToString());

			// 關鍵字搜尋
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				if (int.TryParse(keyword, out int idValue))
					query = query.Where(p => p.PageId == idValue || p.Title.Contains(keyword));
				else
					query = query.Where(p => p.Title.Contains(keyword));
			}

			// 狀態篩選
			if (!string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(p => p.Status == status);
			}

			var pages = query
				.OrderByDescending(p => p.CreatedDate)
				.Select(p => new PageListVM
				{
					PageId = p.PageId,
					Title = p.Title,
					Status = (PageStatus)int.Parse(p.Status),   // varchar -> enum
					CreatedDate = p.CreatedDate,
					RevisedDate = p.RevisedDate
				});

			// 給 ViewBag
			ViewBag.Keyword = keyword;
			ViewBag.Status = status;
			ViewBag.StatusList = new SelectList(
				GetStatusSelectList(null, includeAll: true, includeDeleted: false),
				"Value", "Text", status);


			// 每頁筆數下拉
			ViewBag.PageSizeList = new SelectList(new[] { 5, 10, 20, 50, 100}, pageSize);

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
			// ✅ 使用共用方法
			ViewBag.PageTypeList = GetPageTypeSelectList(vm.PageTypeId);
			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(PageEditVM model, int? page, int pageSize = 8)
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

			// ✅ 保留查詢條件（返回列表）
			return RedirectToAction(nameof(Index), new
			{
				page = model.Page,
				pageSize = model.PageSize > 0 ? model.PageSize : pageSize,
				keyword = model.Keyword,
				status = model.StatusFilter
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
			ViewBag.PageTypeList = GetPageTypeSelectList(vm.PageTypeId);
			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(PageEditVM model, int? page, int pageSize = 8)
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
			pageEntity.Status = ((int)model.Status).ToString();
			pageEntity.RevisedDate = DateTime.Now;

			// ===============================
			// 防呆：首頁 PageTypeId 不允許修改
			// ===============================
			if (model.PageTypeId == HomePageTypeId)
			{
				// 找到實際的首頁
				var homePageEntity = _db.CntPages.FirstOrDefault(p => p.PageId == model.PageId);
				if (homePageEntity != null)
				{
					// 強制保持首頁類別
					model.PageTypeId = HomePageTypeId;
					homePageEntity.PageTypeId = HomePageTypeId;
				}
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

			return RedirectToAction(nameof(Index), new
			{
				page = model.Page,
				pageSize = model.PageSize > 0 ? model.PageSize : pageSize,
				keyword = model.Keyword,
				status = model.StatusFilter
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
		public IActionResult Details(int id, int? page, int pageSize = 8, string? keyword = null, string? status = null)
			{
				var pageEntity = _db.CntPages
					.Include(p => p.CntPageBlocks)               // 撈文章區塊
					.FirstOrDefault(p => p.PageId == id);

				if (pageEntity == null) return NotFound();

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
			var page = _db.CntPages.Find(id);
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
				RevisedDate = page.RevisedDate
			};

			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Delete(PageEditVM model, int? page, int pageSize = 8)
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
			return RedirectToAction(nameof(Index), new
			{
				page = model.Page,
				pageSize = model.PageSize > 0 ? model.PageSize : pageSize,   // ⭐ 保險一層
				keyword = model.Keyword,
				status = model.StatusFilter
			});
		}

		// ================================
		// 回收桶列表 (RecycleBin)
		// ================================
		public IActionResult RecycleBin(int? page, string keyword, int pageSize = 8)
		{
			int pageNumber = Math.Max(page ?? 1, 1);

			var query = _db.CntPages.Where(p => p.Status == ((int)PageStatus.Deleted).ToString());

			if (!string.IsNullOrWhiteSpace(keyword))
			{
				keyword = keyword.Trim();
				if (int.TryParse(keyword, out int idValue))
					query = query.Where(p => p.PageId == idValue || p.Title.Contains(keyword));
				else
					query = query.Where(p => p.Title.Contains(keyword));
			}

			var deletedPages = query
				.OrderByDescending(p => p.RevisedDate ?? p.CreatedDate)
				.Select(p => new PageListVM
				{
					PageId = p.PageId,
					Title = p.Title,
					Status = (PageStatus)int.Parse(p.Status),
					CreatedDate = p.CreatedDate,
					RevisedDate = p.RevisedDate
				});

			ViewBag.Keyword = keyword;
			ViewBag.PageSizeList = new SelectList(new[] { 5, 8, 10, 20, 50, 100 }, pageSize);

			return View(deletedPages.ToPagedList(pageNumber, pageSize));
		}

		// ================================
		// 復原 (Restore)
		// ================================
		public IActionResult Restore(int id, int? page, int pageSize = 8, string? keyword = null, string? status = null)
		{
			var pageEntity = _db.CntPages.Find(id);
			if (pageEntity == null) return NotFound();

			pageEntity.Status = ((int)PageStatus.Draft).ToString();
			pageEntity.RevisedDate = DateTime.Now;

			_db.SaveChanges();
			TempData["Msg"] = "文章已復原";

			return RedirectToAction(nameof(Index), new
			{
				page,
				pageSize,
				keyword,
				status
			});
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Destroy(int id, int? page, int pageSize = 8, string? keyword = null, string? status = null)
		{
			var pageEntity = _db.CntPages.Find(id);
			if (pageEntity == null) return NotFound();

			if (pageEntity.PageTypeId == HomePageTypeId)
			{
				return BadRequest("首頁不能永久刪除");
			}

			_db.CntPages.Remove(pageEntity);
			_db.SaveChanges();

			TempData["Msg"] = "文章已永久刪除";
			return RedirectToAction(nameof(Index), new
			{
				page,
				pageSize,
				keyword,
				status
			});
		}
	}
}
