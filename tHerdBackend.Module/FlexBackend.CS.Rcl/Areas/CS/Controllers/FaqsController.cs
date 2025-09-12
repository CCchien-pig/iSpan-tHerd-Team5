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
        // AJAX: 切換 FAQ 啟用狀態
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, bool isActive)
        {
            // 1) 取得資料
            var faq = await _context.CsFaqs.FirstOrDefaultAsync(x => x.FaqId == id);
            if (faq == null)
                return NotFound(new { ok = false, message = "找不到該 FAQ。" });

            // 2) 更新狀態 + 異動時間
            faq.IsActive = isActive;
            faq.RevisedDate = DateTime.UtcNow;

            // 3) 儲存
            await _context.SaveChangesAsync();

            // 4) 回傳 JSON 給前端
            return Json(new { ok = true, isActive = faq.IsActive });
        }
        // AJAX: 刪除 FAQ
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
                // 常見：有外鍵關聯（例如被其它表參照）導致無法刪除
                return Json(new { ok = false, message = "此 FAQ 已被引用，無法刪除。" });
            }
        }
        // AJAX: 批次刪除 FAQ
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

        // GET: CsFaqs
        public async Task<IActionResult> Index()
        {
            var therd_store_dbContext = _context.CsFaqs.Include(c => c.Category);
            return View(await therd_store_dbContext.ToListAsync());
        }

        // GET: CsFaqs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csFaq = await _context.CsFaqs
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.FaqId == id);
            if (csFaq == null)
            {
                return NotFound();
            }

            return View(csFaq);
        }

        // GET: CsFaqs/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName");
            return View();
        }

        // POST: CsFaqs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FaqId,Title,AnswerHtml,Status,CategoryId,OrderSeq,LastPublishedTime,IsActive,CreatedDate,RevisedDate")] CsFaq csFaq)
        {
            if (ModelState.IsValid)
            {
                _context.Add(csFaq);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csFaq.CategoryId);
            return View(csFaq);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CsFaq input, bool publishNow = false)
        {
            var faq = await _context.CsFaqs.FindAsync(id);
            if (faq == null) return NotFound();

            // 只更新允許的欄位，避免 overposting
            faq.Title = input.Title;
            faq.AnswerHtml = input.AnswerHtml;
            faq.Status = input.Status;
            faq.CategoryId = input.CategoryId;
            faq.OrderSeq = input.OrderSeq;
            faq.LastPublishedTime = input.LastPublishedTime;
            faq.IsActive = input.IsActive;

            if (publishNow)
            {
                faq.IsActive = true;
                faq.LastPublishedTime ??= DateTime.UtcNow;
            }
            faq.RevisedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }




        // GET: CsFaqs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csFaq = await _context.CsFaqs
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.FaqId == id);
            if (csFaq == null)
            {
                return NotFound();
            }

            return View(csFaq);
        }

        // POST: CsFaqs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var csFaq = await _context.CsFaqs.FindAsync(id);
            if (csFaq != null)
            {
                _context.CsFaqs.Remove(csFaq);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CsFaqExists(int id)
        {
            return _context.CsFaqs.Any(e => e.FaqId == id);
        }
    }
}
