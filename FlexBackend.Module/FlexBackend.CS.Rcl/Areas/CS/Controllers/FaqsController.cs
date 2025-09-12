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

		public FaqsController(tHerdDBContext context)
		{
			_context = context;
		}

		// ========== Views (CRUD) ==========

		// GET: CS/Faqs
		public async Task<IActionResult> Index()
		{
			var query = _context.CsFaqs
				.Include(f => f.Category)
				.AsNoTracking();

			var list = await query
				.OrderByDescending(f => f.CreatedDate)
				.ToListAsync();

			return View(list);
		}

		// GET: CS/Faqs/Details/1001
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var faq = await _context.CsFaqs
				.Include(f => f.Category)
				.AsNoTracking()
				.FirstOrDefaultAsync(m => m.FaqId == id);

			if (faq == null) return NotFound();

			return View(faq);
		}

		// GET: CS/Faqs/Create
		public IActionResult Create()
		{
			ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories.AsNoTracking(), "CategoryId", "CategoryName");
			return View();
		}

		// POST: CS/Faqs/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("FaqId,Title,AnswerHtml,Status,CategoryId,OrderSeq,LastPublishedTime,IsActive")] CsFaq csFaq)
		{
			if (!ModelState.IsValid)
			{
				ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories.AsNoTracking(), "CategoryId", "CategoryName", csFaq.CategoryId);
				return View(csFaq);
			}

			// 由伺服器填寫審計欄位
			csFaq.CreatedDate = DateTime.UtcNow;
			csFaq.RevisedDate = null;

			_context.Add(csFaq);
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// GET: CS/Faqs/Edit/1001
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var csFaq = await _context.CsFaqs.FindAsync(id);
			if (csFaq == null) return NotFound();

			ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories.AsNoTracking(), "CategoryId", "CategoryName", csFaq.CategoryId);
			return View(csFaq);
		}

		// POST: CS/Faqs/Edit/1001
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("FaqId,Title,AnswerHtml,Status,CategoryId,OrderSeq,LastPublishedTime,IsActive,CreatedDate")] CsFaq csFaq)
		{
			if (id != csFaq.FaqId) return NotFound();

			if (!ModelState.IsValid)
			{
				ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories.AsNoTracking(), "CategoryId", "CategoryName", csFaq.CategoryId);
				return View(csFaq);
			}

			try
			{
				// 只更新允許的欄位與 RevisedDate
				var dbEntity = await _context.CsFaqs.FirstOrDefaultAsync(x => x.FaqId == id);
				if (dbEntity == null) return NotFound();

				dbEntity.Title = csFaq.Title;
				dbEntity.AnswerHtml = csFaq.AnswerHtml;
				dbEntity.CategoryId = csFaq.CategoryId;
				dbEntity.OrderSeq = csFaq.OrderSeq;
				dbEntity.LastPublishedTime = csFaq.LastPublishedTime;
				dbEntity.IsActive = csFaq.IsActive;
				dbEntity.RevisedDate = DateTime.UtcNow;

				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!CsFaqExists(csFaq.FaqId)) return NotFound();
				throw;
			}

			return RedirectToAction(nameof(Index));
		}

		// GET: CS/Faqs/Delete/1001  (保留給 scaffold 用；實務刪除走 AJAX)
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();

			var csFaq = await _context.CsFaqs
				.Include(f => f.Category)
				.AsNoTracking()
				.FirstOrDefaultAsync(m => m.FaqId == id);

			if (csFaq == null) return NotFound();

			return View(csFaq);
		}

		// POST: CS/Faqs/Delete/1001
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var csFaq = await _context.CsFaqs.FindAsync(id);
			if (csFaq != null)
			{
				_context.CsFaqs.Remove(csFaq);
				await _context.SaveChangesAsync();
			}
			return RedirectToAction(nameof(Index));
		}

		// ========== AJAX Endpoints ==========

		/// <summary>
		/// 切換 IsActive
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleActive(int id, bool isActive)
		{
			var faq = await _context.CsFaqs.FirstOrDefaultAsync(x => x.FaqId == id);
			if (faq == null)
				return NotFound(new { ok = false, message = "找不到該 FAQ。" });

			faq.IsActive = isActive;
			faq.RevisedDate = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			return Json(new { ok = true, isActive = faq.IsActive });
		}

		/// <summary>
		/// 單筆刪除（供列表 AJAX 使用）
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteAjax(int id)
		{
			var faq = await _context.CsFaqs.FindAsync(id);
			if (faq == null)
				return NotFound(new { ok = false, message = "找不到該 FAQ。" });

			try
			{
				_context.CsFaqs.Remove(faq);
				await _context.SaveChangesAsync();
				return Json(new { ok = true });
			}
			catch (DbUpdateException)
			{
				return Json(new { ok = false, message = "此 FAQ 已被引用，無法刪除。" });
			}
		}

		/// <summary>
		/// 多筆刪除（ids[]=1&ids[]=2...）
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteManyAjax([FromForm] int[] ids)
		{
			if (ids == null || ids.Length == 0)
				return Json(new { ok = false, message = "沒有要刪除的項目。" });

			var list = await _context.CsFaqs.Where(f => ids.Contains(f.FaqId)).ToListAsync();
			if (list.Count == 0)
				return Json(new { ok = false, message = "找不到要刪除的項目。" });

			try
			{
				_context.CsFaqs.RemoveRange(list);
				await _context.SaveChangesAsync();
				return Json(new { ok = true, count = list.Count });
			}
			catch (DbUpdateException)
			{
				return Json(new { ok = false, message = "有資料被引用而無法刪除。" });
			}
		}

		// ========== Helpers ==========

		private bool CsFaqExists(int id) => _context.CsFaqs.Any(e => e.FaqId == id);
	}
}
