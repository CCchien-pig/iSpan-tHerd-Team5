using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
		// 文章列表 (Index)
		// ================================
		public IActionResult Index(int? page, string keyword, string status, int pageSize = 8)
		{
			int pageNumber = page ?? 1;

			var query = _db.CntPages.Where(p => p.Status != "9");

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
					Status = p.Status,
					CreatedDate = p.CreatedDate,
					RevisedDate = p.RevisedDate
				});

			// 給 ViewBag
			ViewBag.Keyword = keyword;
			ViewBag.Status = status;

			// 狀態下拉
			ViewBag.StatusList = new SelectList(new[]
			{
		new { Value = "", Text = "全部狀態" },
		new { Value = "0", Text = "草稿" },
		new { Value = "1", Text = "已發布" },
		new { Value = "2", Text = "下架/封存" }
	}, "Value", "Text", status);

			// ✅ 每頁筆數下拉
			ViewBag.PageSizeList = new SelectList(
				new[] { 5, 8, 20 },
				pageSize   // 預設選中值
			);

			return View(pages.ToPagedList(pageNumber, pageSize));
		}





		// ================================
		// 新增 (Create)
		// ================================
		public IActionResult Create() => View();

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(PageEditVM model)
		{
			if (!ModelState.IsValid) return View(model);

			var page = new CntPage
			{
				Title = model.Title,
				Status = model.Status,
				CreatedDate = DateTime.Now
			};

			_db.CntPages.Add(page);
			_db.SaveChanges();

			TempData["Msg"] = "文章新增成功";
			return RedirectToAction(nameof(Index));
		}

		// ================================
		// 編輯 (Edit)
		// ================================
		public IActionResult Edit(int id)
		{
			var page = _db.CntPages.Find(id);
			if (page == null) return NotFound();

			var vm = new PageEditVM
			{
				PageId = page.PageId,
				Title = page.Title,
				Status = page.Status,
				RevisedDate = page.RevisedDate
			};

			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(PageEditVM model)
		{
			if (!ModelState.IsValid) return View(model);

			var page = _db.CntPages.Find(model.PageId);
			if (page == null) return NotFound();

			page.Title = model.Title;
			page.Status = model.Status;
			page.RevisedDate = DateTime.Now;

			_db.SaveChanges();
			TempData["Msg"] = "文章修改成功";
			return RedirectToAction(nameof(Index));
		}

		// ================================
		// 詳細頁面 (Details)
		// ================================
		public IActionResult Details(int id)
		{
			var page = _db.CntPages.Find(id);
			if (page == null) return NotFound();

			var vm = new PageListVM
			{
				PageId = page.PageId,
				Title = page.Title,
				Status = page.Status,
				CreatedDate = page.CreatedDate,
				RevisedDate = page.RevisedDate
			};

			return View(vm);
		}

		// ================================
		// 刪除 → 移到回收桶
		// ================================
		// GET: Pages/Delete/5
		public IActionResult Delete(int id)
		{
			var page = _db.CntPages.Find(id);
			if (page == null) return NotFound();

			var vm = new PageEditVM
			{
				PageId = page.PageId,
				Title = page.Title,
				Status = page.Status,
				RevisedDate = page.RevisedDate
			};

			return View(vm);
		}

		// POST: Pages/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Delete(PageEditVM model)
		{
			var page = _db.CntPages.Find(model.PageId);
			if (page == null) return NotFound();

			// ★ 這裡如果是 "回收桶" 機制 → 設定 Status = "9"
			page.Status = "9";
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

			var query = _db.CntPages
				.Where(p => p.Status == "9"); // 只抓回收桶文章

			// 關鍵字搜尋
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
					Status = p.Status,
					CreatedDate = p.CreatedDate,
					RevisedDate = p.RevisedDate
				});

			ViewBag.Keyword = keyword;

			// ✅ 每頁筆數下拉 (回收桶也要)
			ViewBag.PageSizeList = new SelectList(
				new[] { 5, 8, 20 },
				pageSize
			);

			return View(deletedPages.ToPagedList(pageNumber, pageSize));
		}


		// ================================
		// 復原 (Restore)
		// ================================
		public IActionResult Restore(int id)
		{
			var page = _db.CntPages.Find(id);
			if (page == null) return NotFound();

			page.Status = "0"; // 復原成草稿
			page.RevisedDate = DateTime.Now;

			_db.SaveChanges();
			TempData["Msg"] = "文章已復原";
			return RedirectToAction(nameof(RecycleBin));
		}

		// ================================
		// 永久刪除 (Destroy)
		// ================================
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
