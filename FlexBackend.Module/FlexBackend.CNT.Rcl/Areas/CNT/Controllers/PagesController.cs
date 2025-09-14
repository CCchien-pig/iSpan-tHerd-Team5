using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using X.PagedList;
using X.PagedList.Extensions;

namespace FlexBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[Area("CNT")]
	public class PagesController : Controller
	{
		private readonly tHerdDBContext _db;

		public PagesController(tHerdDBContext db)
		{
			_db = db;
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
			ViewBag.PageSizeList = new SelectList(new[] { 5, 8, 10, 20, 50, 100 }, pageSize);

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
				_db.CntTags.Where(t => t.IsActive == true).ToList(),
			"TagId", "TagName"
				)
			};
			return View(vm);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(PageEditVM model)
		{
			if (!ModelState.IsValid)
			{
				model.StatusList = GetStatusSelectList(model.Status);
				model.TagOptions = new MultiSelectList(
					_db.CntTags.Where(t => t.IsActive == true).ToList(),
					"TagId", "TagName", model.SelectedTagIds
				);
				return View(model);
			}

			var page = new CntPage
			{
				Title = model.Title,
				Status = ((int)model.Status).ToString(),
				CreatedDate = DateTime.Now,
				RevisedDate = DateTime.Now
			};

			_db.CntPages.Add(page);
			_db.SaveChanges(); // ⭐ 先存，確保 PageId 產生

			// ⭐ 新增 CNT_PageTag 關聯
			if (model.SelectedTagIds != null)
			{
				foreach (var tagId in model.SelectedTagIds)
				{
					_db.CntPageTags.Add(new CntPageTag
					{
						PageId = page.PageId,
						TagId = tagId,
						CreatedDate = DateTime.Now
					});
				}				
			}

			// ⭐ 建立第一個 PageBlock（如果有輸入內容）
			if (!string.IsNullOrWhiteSpace(model.NewBlockContent))
			{
				var block = new CntPageBlock
				{
					PageId = page.PageId,
					BlockType = model.NewBlockType,
					Content = model.NewBlockContent,
					OrderSeq = 1,
					CreatedDate = DateTime.Now
				};
				_db.CntPageBlocks.Add(block);
			}

			_db.SaveChanges();

			TempData["Msg"] = "文章已建立並新增內容區塊";
			return RedirectToAction(nameof(Edit), new { id = page.PageId });
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
				_db.CntTags.Where(t => t.IsActive == true).ToList(),
			"TagId", "TagName", selectedTagIds
		),
				Blocks = page.CntPageBlocks.OrderBy(b => b.OrderSeq).ToList() // ⭐ 區塊載入
			};

			return View(vm);
		}



		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(PageEditVM model)
		{
			if (!ModelState.IsValid)
			{
				model.StatusList = GetStatusSelectList(model.Status);
				model.TagOptions = new MultiSelectList(
					_db.CntTags.Where(t => t.IsActive == true).ToList(),
					"TagId", "TagName", model.SelectedTagIds
				);

				// 重新載入 Blocks
				model.Blocks = _db.CntPageBlocks
					.Where(b => b.PageId == model.PageId)
					.OrderBy(b => b.OrderSeq).ToList();

				return View(model);
			}

			var page = _db.CntPages.FirstOrDefault(p => p.PageId == model.PageId);
			if (page == null) return NotFound();

			// 更新 Page 基本資料
			page.Title = model.Title;
			page.Status = ((int)model.Status).ToString();
			page.RevisedDate = DateTime.Now;

			// ⭐ 更新 CNT_PageTag 關聯
			var oldTags = _db.CntPageTags.Where(pt => pt.PageId == page.PageId);
			_db.CntPageTags.RemoveRange(oldTags);

			if (model.SelectedTagIds != null)
			{
				foreach (var tagId in model.SelectedTagIds)
				{
					_db.CntPageTags.Add(new CntPageTag
					{
						PageId = page.PageId,
						TagId = tagId,
						CreatedDate = DateTime.Now
					});
				}
			}

			_db.SaveChanges();
			TempData["Msg"] = "文章修改成功";

			return RedirectToAction(nameof(Edit), new { id = model.PageId });
		}



		// ================================
		// 詳細頁面 (Details)
		// ================================
		public IActionResult Details(int id)
		{
			var page = _db.CntPages.Find(id);
			if (page == null) return NotFound();

			var tagNames = (from pt in _db.CntPageTags
							join t in _db.CntTags on pt.TagId equals t.TagId
							where pt.PageId == id
							select t.TagName).ToList();

			var vm = new PageDetailVM
			{
				PageId = page.PageId,
				Title = page.Title,
				Status = (PageStatus)int.Parse(page.Status),
				CreatedDate = page.CreatedDate,
				RevisedDate = page.RevisedDate,
				TagNames = tagNames
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
		public IActionResult Delete(PageEditVM model)
		{
			var page = _db.CntPages.Find(model.PageId);
			if (page == null) return NotFound();

			page.Status = ((int)PageStatus.Deleted).ToString();
			page.RevisedDate = DateTime.Now;

			_db.SaveChanges();
			TempData["Msg"] = "文章已移到回收桶";
			return RedirectToAction(nameof(Index));
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
			ViewBag.PageSizeList = new SelectList(new[] { 5, 8, 20 }, pageSize);

			return View(deletedPages.ToPagedList(pageNumber, pageSize));
		}

		// ================================
		// 復原 (Restore)
		// ================================
		public IActionResult Restore(int id)
		{
			var page = _db.CntPages.Find(id);
			if (page == null) return NotFound();

			page.Status = ((int)PageStatus.Draft).ToString();
			page.RevisedDate = DateTime.Now;

			_db.SaveChanges();
			TempData["Msg"] = "文章已復原";
			return RedirectToAction(nameof(RecycleBin));
		}

		// ================================
		// 永久刪除 (Destroy) → POST 才能執行
		// ================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Destroy(int id)
		{
			var page = _db.CntPages.Find(id);
			if (page == null) return NotFound();

			_db.CntPages.Remove(page);
			_db.SaveChanges();

			TempData["Msg"] = "文章已永久刪除";
			return RedirectToAction(nameof(RecycleBin));
		}
	}
}
