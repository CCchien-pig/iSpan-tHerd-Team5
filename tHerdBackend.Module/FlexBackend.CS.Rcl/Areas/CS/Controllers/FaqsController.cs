using System;
using System.Linq;
using System.Threading.Tasks;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.CS.Rcl.Areas.CS.Controllers
{
	[Area("CS")]
	public class FaqsController : Controller
	{
		private readonly tHerdDBContext _context;
		public FaqsController(tHerdDBContext context) => _context = context;

		// 首頁（當作 Hub 殼，前端用 AJAX 載入 Partial）
		[HttpGet]
		public IActionResult Index() => View();

		// 給 Index.cshtml AJAX 載入的 Partial（FAQ 主表清單）
		[HttpGet]
		public async Task<IActionResult> IndexPartial()
		{
			var list = await _context.CsFaqs
									 .Include(f => f.Category)
									 .OrderBy(f => f.OrderSeq)
									 .ThenByDescending(f => f.CreatedDate)
									 .ToListAsync();

			return PartialView("~/Areas/CS/Views/Shared/_FaqsTable.cshtml", list);
		}

		// ===== AJAX：切換啟用 =====
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleActive(int id, bool isActive)
		{
			var faq = await _context.CsFaqs.FindAsync(id);
			if (faq == null)
				return NotFound(new { ok = false, message = "找不到 FAQ" });

			faq.IsActive = isActive;
			faq.RevisedDate = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return Json(new { ok = true, isActive = faq.IsActive });
		}

		// ===== AJAX：單筆刪除 =====
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteAjax(int id)
		{
			var faq = await _context.CsFaqs.FindAsync(id);
			if (faq == null)
				return NotFound(new { ok = false, message = "找不到 FAQ" });

			try
			{
				_context.CsFaqs.Remove(faq);
				await _context.SaveChangesAsync();
				return Json(new { ok = true });
			}
			catch (DbUpdateException)
			{
				return Json(new { ok = false, message = "此 FAQ 已被引用，無法刪除" });
			}
		}

		// ===== AJAX：批次刪除 =====
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteManyAjax([FromForm] int[] ids)
		{
			if (ids == null || ids.Length == 0)
				return Json(new { ok = false, message = "沒有要刪除的項目" });

			var list = await _context.CsFaqs.Where(f => ids.Contains(f.FaqId)).ToListAsync();
			if (list.Count == 0)
				return Json(new { ok = false, message = "找不到要刪除的項目" });

			try
			{
				_context.CsFaqs.RemoveRange(list);
				await _context.SaveChangesAsync();
				return Json(new { ok = true, count = list.Count });
			}
			catch (DbUpdateException)
			{
				return Json(new { ok = false, message = "部分資料被引用，無法刪除" });
			}
		}

		// ====== Scaffolded CRUD（保留你的樣式）======

		// GET: CsFaqs/Details/5
		[HttpGet]
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var csFaq = await _context.CsFaqs
									  .Include(c => c.Category)
									  .FirstOrDefaultAsync(m => m.FaqId == id);
			if (csFaq == null) return NotFound();
			return View(csFaq);
		}

		// GET: CsFaqs/Create
		[HttpGet]
		public IActionResult Create()
		{
			ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName");
			return View();
		}

		// POST: CsFaqs/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("FaqId,Title,AnswerHtml,Status,CategoryId,OrderSeq,LastPublishedTime,IsActive,CreatedDate,RevisedDate")] CsFaq csFaq)
		{
			if (!ModelState.IsValid)
			{
				ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csFaq.CategoryId);
				return View(csFaq);
			}

			// 補上時間欄位
			csFaq.CreatedDate = csFaq.CreatedDate == default ? DateTime.UtcNow : csFaq.CreatedDate;
			csFaq.RevisedDate = DateTime.UtcNow;

			_context.Add(csFaq);
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// GET: CsFaqs/Edit/5
		[HttpGet]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var csFaq = await _context.CsFaqs.FindAsync(id);
			if (csFaq == null) return NotFound();

			ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csFaq.CategoryId);
			return View(csFaq);
		}

		// POST: CsFaqs/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("FaqId,Title,AnswerHtml,Status,CategoryId,OrderSeq,LastPublishedTime,IsActive,CreatedDate,RevisedDate")] CsFaq csFaq)
		{
			if (id != csFaq.FaqId) return NotFound();

			if (!ModelState.IsValid)
			{
				ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csFaq.CategoryId);
				return View(csFaq);
			}

			try
			{
				// 為避免覆寫 CreatedDate，可選擇只標記變更或直接 Update（若你已正確綁定即可）
				_context.Update(csFaq);
				csFaq.RevisedDate = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!CsFaqExists(csFaq.FaqId)) return NotFound();
				throw;
			}

			return RedirectToAction(nameof(Index));
		}

		// GET: CsFaqs/Delete/5
		[HttpGet]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();

			var csFaq = await _context.CsFaqs
									  .Include(c => c.Category)
									  .FirstOrDefaultAsync(m => m.FaqId == id);
			if (csFaq == null) return NotFound();

			return View(csFaq);
		}

		// POST: CsFaqs/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var csFaq = await _context.CsFaqs.FindAsync(id);
			if (csFaq != null) _context.CsFaqs.Remove(csFaq);

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool CsFaqExists(int id) => _context.CsFaqs.Any(e => e.FaqId == id);
	}
}
