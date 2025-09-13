using System.Linq;
using System.Threading.Tasks;
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

        public FaqKeywordsController(tHerdDBContext context)
        {
            _context = context;
        }

        // ====== List ======
        public async Task<IActionResult> Index()
        {
            var list = await _context.CsFaqKeywords
                                     .Include(k => k.Faq)           // 取 FAQ 標題
                                     .OrderBy(k => k.Keyword)
                                     .AsNoTracking()
                                     .ToListAsync();
            return View(list);
        }

        // ====== Details ======
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var kw = await _context.CsFaqKeywords
                                   .Include(k => k.Faq)
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(m => m.KeywordId == id);
            if (kw == null) return NotFound();
            return View(kw);
        }

        // ====== Create ======
        public IActionResult Create()
        {
            ViewData["FaqId"] = new SelectList(_context.CsFaqs.AsNoTracking()
                                                .OrderBy(f => f.Title), "FaqId", "Title");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FaqId,Keyword")] CsFaqKeyword kw)
        {
            if (!ModelState.IsValid)
            {
                ViewData["FaqId"] = new SelectList(_context.CsFaqs.AsNoTracking()
                                                    .OrderBy(f => f.Title), "FaqId", "Title", kw.FaqId);
                return View(kw);
            }

            kw.CreatedDate = DateTime.UtcNow;        // 伺服器填時間
            _context.Add(kw);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ====== Edit ======
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var kw = await _context.CsFaqKeywords.FindAsync(id);
            if (kw == null) return NotFound();

            ViewData["FaqId"] = new SelectList(_context.CsFaqs.AsNoTracking()
                                                .OrderBy(f => f.Title), "FaqId", "Title", kw.FaqId);
            return View(kw);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("KeywordId,FaqId,Keyword,CreatedDate")] CsFaqKeyword input)
        {
            if (id != input.KeywordId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["FaqId"] = new SelectList(_context.CsFaqs.AsNoTracking()
                                                    .OrderBy(f => f.Title), "FaqId", "Title", input.FaqId);
                return View(input);
            }

            var db = await _context.CsFaqKeywords.FirstOrDefaultAsync(x => x.KeywordId == id);
            if (db == null) return NotFound();

            // 僅更新允許欄位；CreatedDate 保留
            db.FaqId = input.FaqId;
            db.Keyword = input.Keyword;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ====== Delete (scaffold 頁面用；實務刪除走 AJAX) ======
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var kw = await _context.CsFaqKeywords
                                   .Include(k => k.Faq)
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(m => m.KeywordId == id);
            if (kw == null) return NotFound();
            return View(kw);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kw = await _context.CsFaqKeywords.FindAsync(id);
            if (kw != null)
            {
                _context.CsFaqKeywords.Remove(kw);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ====== AJAX：單筆刪除 ======
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
                // 關鍵字被唯一鍵/外鍵擋住的狀況
                return Json(new { ok = false, message = "關鍵字被引用或重複，無法刪除" });
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
                return Json(new { ok = false, message = "部分項目被引用或重複，無法刪除" });
            }
        }

        private bool CsFaqKeywordExists(int id) => _context.CsFaqKeywords.Any(e => e.KeywordId == id);
    }
}
