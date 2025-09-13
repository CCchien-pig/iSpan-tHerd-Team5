
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.CS.Rcl.Areas.CS.Controllers
{
	[Area("CS")]
	public class FaqKeywordsController : Controller
	{
		private readonly tHerdDBContext _context;
		public FaqKeywordsController(tHerdDBContext context) => _context = context;

		// ====== 首頁（可保留為獨立頁面，不影響 AJAX Partial 的載入）======
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var list = await _context.CsFaqKeywords
									 .Include(k => k.Faq) // 若無導航屬性可移除
									 .OrderByDescending(k => k.CreatedDate)
									 .ToListAsync();
			return View(list);
		}

		// ====== 給 Index.cshtml（或 Hub）AJAX 載入的 Partial ======
		[HttpGet]
		public async Task<IActionResult> IndexPartial()
		{
			var list = await _context.CsFaqKeywords
									 .Include(k => k.Faq) // 若無導航屬性可移除
									 .OrderByDescending(k => k.CreatedDate)
									 .ToListAsync();

			// 依你的實際放置位置調整 Partial 路徑
			return PartialView("~/Areas/CS/Views/Shared/_FaqKeywordsTable.cshtml", list);
		}

		// ====== 詳細 ======
		[HttpGet]
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var kw = await _context.CsFaqKeywords
								   .Include(k => k.Faq)
								   .FirstOrDefaultAsync(m => m.KeywordId == id);
			if (kw == null) return NotFound();

			return View(kw);
		}

		// ====== 新增 ======
		[HttpGet]
		public IActionResult Create()
		{
			// 下拉顯示 FAQ 標題；若沒有 Title 欄位，改成你要的顯示欄位
			ViewData["FaqId"] = new SelectList(_context.CsFaqs, "FaqId", "Title");
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("FaqId,Keyword")] CsFaqKeyword input)
		{
			if (!ModelState.IsValid)
			{
				ViewData["FaqId"] = new SelectList(_context.CsFaqs, "FaqId", "Title", input.FaqId);
				return View(input);
			}

			input.CreatedDate = DateTime.UtcNow;
			_context.CsFaqKeywords.Add(input);
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// ====== 編輯 ======
		[HttpGet]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var kw = await _context.CsFaqKeywords.FindAsync(id);
			if (kw == null) return NotFound();

			ViewData["FaqId"] = new SelectList(_context.CsFaqs, "FaqId", "Title", kw.FaqId);
			return View(kw);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("KeywordId,FaqId,Keyword")] CsFaqKeyword input)
		{
			if (id != input.KeywordId) return NotFound();

			var kw = await _context.CsFaqKeywords.FindAsync(id);
			if (kw == null) return NotFound();

			if (!ModelState.IsValid)
			{
				ViewData["FaqId"] = new SelectList(_context.CsFaqs, "FaqId", "Title", input.FaqId);
				return View(input);
			}

			// 僅更新可編輯欄位，避免覆寫 CreatedDate
			kw.FaqId = input.FaqId;
			kw.Keyword = input.Keyword;

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// ====== 刪除（頁面版）======
		[HttpGet]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();

			var kw = await _context.CsFaqKeywords
								   .Include(k => k.Faq)
								   .FirstOrDefaultAsync(m => m.KeywordId == id);
			if (kw == null) return NotFound();

			return View(kw);
		}

		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var kw = await _context.CsFaqKeywords.FindAsync(id);
			if (kw != null) _context.CsFaqKeywords.Remove(kw);

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// ====== AJAX：單筆刪除（供 DataTables/SweetAlert2）======
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteAjax(int id)
		{
			var kw = await _context.CsFaqKeywords.FindAsync(id);
			if (kw == null) return NotFound(new { ok = false, message = "找不到關鍵字" });

			try
			{
				_context.CsFaqKeywords.Remove(kw);
				await _context.SaveChangesAsync();
				return Json(new { ok = true });
			}
			catch (DbUpdateException)
			{
				return Json(new { ok = false, message = "此關鍵字被引用，無法刪除" });
			}
		}

		// ====== AJAX：批次刪除 ======
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteManyAjax([FromForm] int[] ids)
		{
			if (ids == null || ids.Length == 0)
				return Json(new { ok = false, message = "沒有要刪除的項目" });

			var list = await _context.CsFaqKeywords
									 .Where(k => ids.Contains(k.KeywordId))
									 .ToListAsync();
			if (list.Count == 0)
				return Json(new { ok = false, message = "找不到要刪除的項目" });

			try
			{
				_context.CsFaqKeywords.RemoveRange(list);
				await _context.SaveChangesAsync();
				return Json(new { ok = true, count = list.Count });
			}
			catch (DbUpdateException)
			{
				return Json(new { ok = false, message = "部分資料被引用，無法刪除" });
			}
		}

		private bool CsFaqKeywordExists(int id) =>
			_context.CsFaqKeywords.Any(e => e.KeywordId == id);
	}
}
