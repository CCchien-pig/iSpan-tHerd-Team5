using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using System;
using System.Linq;
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
		public IActionResult Index(int? page)
		{
			int pageNumber = page ?? 1;   // 預設第 1 頁
			int pageSize = 6;             // 每頁顯示 6 筆

			var pages = _db.CntPages
				.Where(p => p.Status != "9")  // 過濾掉已刪除（回收桶）
				.OrderByDescending(p => p.CreatedDate)
				.Select(p => new PageListVM
				{
					PageId = p.PageId,
					Title = p.Title,
					Status = p.Status,
					CreatedDate = p.CreatedDate,
					RevisedDate = p.RevisedDate   
				});

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
				CreatedDate = DateTime.UtcNow
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
				Status = page.Status
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
			page.RevisedDate = DateTime.UtcNow;

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
			page.RevisedDate = DateTime.UtcNow;

			_db.SaveChanges();
			TempData["Msg"] = "文章已移到回收桶";
			return RedirectToAction(nameof(Index));
		}

		// ================================
		// 回收桶列表 (RecycleBin)
		// ================================
		public IActionResult RecycleBin(int? page)
		{
			int pageNumber = page ?? 1;
			int pageSize = 6;

			var deletedPages = _db.CntPages
				.Where(p => p.Status == "9")
				.OrderByDescending(p => p.RevisedDate ?? p.CreatedDate)
				.Select(p => new PageListVM
				{
					PageId = p.PageId,
					Title = p.Title,
					Status = p.Status,
					CreatedDate = p.CreatedDate
				});

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
			page.RevisedDate = DateTime.UtcNow;

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
