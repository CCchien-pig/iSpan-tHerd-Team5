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

        // ========== Views (CRUD) ==========

        // GET: CS/Faqs
        public async Task<IActionResult> Index()
        {
            var list = await _context.CsFaqs
                .Include(f => f.Category)
                .AsNoTracking()
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

            // ★ 改用本地時間，避免與 UI 時區不一致
            csFaq.CreatedDate = DateTime.Now;
            csFaq.RevisedDate = DateTime.Now;

            // 若「建立時」就啟用，且未輸入 LastPublishedTime，幫忙補現在
            if (csFaq.IsActive && csFaq.LastPublishedTime == null)
                csFaq.LastPublishedTime = DateTime.Now;

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
        public async Task<IActionResult> Edit(
            int id,
            [Bind("FaqId,Title,AnswerHtml,Status,CategoryId,OrderSeq,LastPublishedTime,IsActive,CreatedDate")]
            CsFaq input,
            bool publishNow = false
        )
        {
            if (id != input.FaqId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories.AsNoTracking(), "CategoryId", "CategoryName", input.CategoryId);
                return View(input);
            }

            try
            {
                var db = await _context.CsFaqs.FirstOrDefaultAsync(x => x.FaqId == id);
                if (db == null) return NotFound();

                // 允許更新的欄位
                db.Title = input.Title;
                db.AnswerHtml = input.AnswerHtml;
                db.Status = input.Status;
                db.CategoryId = input.CategoryId;
                db.OrderSeq = input.OrderSeq;
                db.IsActive = input.IsActive;

                // 發布邏輯：
                // 1) 如果按了「儲存並發布」→ 一律啟用 + 未填發布時間則補現在
                // 2) 如果只是一般儲存，但使用者把 IsActive 勾成 true 且尚未發布過 → 補現在
                if (publishNow)
                {
                    db.IsActive = true;
                    db.LastPublishedTime ??= DateTime.Now;
                }
                else if (db.IsActive && db.LastPublishedTime == null)
                {
                    db.LastPublishedTime = DateTime.Now;
                }
                else
                {
                    // 若表單有帶值（例如使用者手動輸入時間），就以表單為準
                    db.LastPublishedTime = input.LastPublishedTime;
                }

                db.RevisedDate = DateTime.Now;  // ★ 本地時間

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CsFaqExists(input.FaqId)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: CS/Faqs/Delete/1001（保留）
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

        // POST: CS/Faqs/Delete/1001（保留）
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

        /// <summary>切換 IsActive（從停用→啟用且未發布過時，自動補發布時間）</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, bool isActive)
        {
            var faq = await _context.CsFaqs.FirstOrDefaultAsync(x => x.FaqId == id);
            if (faq == null)
                return NotFound(new { ok = false, message = "找不到該 FAQ。" });

            bool wasActive = faq.IsActive;
            faq.IsActive = isActive;

            if (!wasActive && isActive && faq.LastPublishedTime == null)
                faq.LastPublishedTime = DateTime.Now; // ★ 本地時間

            faq.RevisedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { ok = true, isActive = faq.IsActive });
        }

        /// <summary>單筆刪除（供列表 AJAX 使用）</summary>
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

        /// <summary>多筆刪除（ids[]=1&ids[]=2...）</summary>
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

        private bool CsFaqExists(int id) => _context.CsFaqs.Any(e => e.FaqId == id);
    }
}
