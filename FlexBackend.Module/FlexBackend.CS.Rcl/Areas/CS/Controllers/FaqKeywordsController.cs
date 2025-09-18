using FlexBackend.CS.Rcl.Areas.CS.ViewModels;
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

        // =============== Index ===============
        // 視圖建議：@model IEnumerable<FaqKeywordListVM>
        public async Task<IActionResult> Index()
        {
            var list = await _context.CsFaqKeywords
                .AsNoTracking()
                .Select(k => new
                {
                    k.KeywordId,
                    k.Keyword,
                    k.CreatedDate,
                    k.FaqId,
                    FaqTitle = k.Faq.Title
                })
                .GroupBy(x => x.Keyword)
                .Select(g => new FaqKeywordListVM
                {
                    // 代表用，哪一個都行
                    KeywordId = g.Max(x => x.KeywordId),
                    Keyword = g.Key,
                    CreatedDate = g.Min(x => x.CreatedDate),
                    Faqs = g.OrderBy(x => x.FaqTitle)
                            .Select(x => new FaqRefVM { FaqId = x.FaqId, Title = x.FaqTitle })
                            .ToList()
                })
                .OrderBy(x => x.Keyword)
                .ToListAsync();

            return View(list);
        }



        // =============== Details ===============
        // 視圖建議：@model FaqKeywordListVM
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var key = await _context.CsFaqKeywords
                .AsNoTracking()
                .Where(k => k.KeywordId == id)
                .Select(k => k.Keyword)
                .FirstOrDefaultAsync();
            if (key == null) return NotFound();

            var vm = await _context.CsFaqKeywords
                .AsNoTracking()
                .Where(k => k.Keyword == key)
                .Select(k => new
                {
                    k.KeywordId,
                    k.Keyword,
                    k.CreatedDate,
                    k.FaqId,
                    FaqTitle = k.Faq.Title
                })
                .GroupBy(x => x.Keyword)
                .Select(g => new FaqKeywordListVM
                {
                    KeywordId = g.Max(x => x.KeywordId),
                    Keyword = g.Key,
                    CreatedDate = g.Min(x => x.CreatedDate),
                    Faqs = g.OrderBy(x => x.FaqTitle)
                            .Select(x => new FaqRefVM { FaqId = x.FaqId, Title = x.FaqTitle })
                            .ToList()
                })
                .FirstAsync();

            return View(vm);
        }


        // =============== Create ===============
        // 視圖建議：@model FaqKeywordEditVM
        public IActionResult Create()
        {
            ViewBag.FaqId = new SelectList(_context.CsFaqs.AsNoTracking().OrderBy(f => f.Title), "FaqId", "Title");
            return View(new FaqKeywordEditVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FaqKeywordEditVM vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.FaqId = new SelectList(_context.CsFaqs.AsNoTracking().OrderBy(f => f.Title), "FaqId", "Title", vm.FaqId);
                return View(vm);
            }

            // -> 伺服器端去除空白並做唯一性檢查（同一 FAQ 下不得重複）
            var keyword = (vm.Keyword ?? string.Empty).Trim();
            var exists = await _context.CsFaqKeywords
                .AsNoTracking()
                .AnyAsync(k => k.FaqId == vm.FaqId && k.Keyword == keyword);

            if (exists)
            {
                ModelState.AddModelError(nameof(vm.Keyword), "此 FAQ 已存在相同關鍵字。");
                ViewBag.FaqId = new SelectList(_context.CsFaqs.AsNoTracking().OrderBy(f => f.Title), "FaqId", "Title", vm.FaqId);
                return View(vm);
            }

            var entity = new CsFaqKeyword
            {
                FaqId = vm.FaqId,
                Keyword = keyword,
                CreatedDate = DateTime.Now
            };

            _context.CsFaqKeywords.Add(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =============== Edit ===============
        // 視圖建議：@model FaqKeywordEditVM
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var entity = await _context.CsFaqKeywords.AsNoTracking().FirstOrDefaultAsync(k => k.KeywordId == id);
            if (entity == null) return NotFound();

            var vm = new FaqKeywordEditVM
            {
                KeywordId = entity.KeywordId,
                FaqId = entity.FaqId,
                Keyword = entity.Keyword
            };

            ViewBag.FaqId = new SelectList(_context.CsFaqs.AsNoTracking().OrderBy(f => f.Title), "FaqId", "Title", vm.FaqId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FaqKeywordEditVM vm)
        {
            if (id != vm.KeywordId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.FaqId = new SelectList(_context.CsFaqs.AsNoTracking().OrderBy(f => f.Title), "FaqId", "Title", vm.FaqId);
                return View(vm);
            }

            var entity = await _context.CsFaqKeywords.FirstOrDefaultAsync(k => k.KeywordId == id);
            if (entity == null) return NotFound();

            var keyword = (vm.Keyword ?? string.Empty).Trim();

            // 編輯時也做唯一性檢查（排除自己）
            var exists = await _context.CsFaqKeywords
                .AsNoTracking()
                .AnyAsync(k => k.KeywordId != id && k.FaqId == vm.FaqId && k.Keyword == keyword);

            if (exists)
            {
                ModelState.AddModelError(nameof(vm.Keyword), "此 FAQ 已存在相同關鍵字。");
                ViewBag.FaqId = new SelectList(_context.CsFaqs.AsNoTracking().OrderBy(f => f.Title), "FaqId", "Title", vm.FaqId);
                return View(vm);
            }

            entity.FaqId = vm.FaqId;
            entity.Keyword = keyword;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =============== Delete（傳統頁面版） ===============
        // 視圖建議：@model FaqKeywordListVM
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var key = await _context.CsFaqKeywords
                .AsNoTracking()
                .Where(k => k.KeywordId == id)
                .Select(k => k.Keyword)
                .FirstOrDefaultAsync();
            if (key == null) return NotFound();

            var vm = await _context.CsFaqKeywords
                .AsNoTracking()
                .Where(k => k.Keyword == key)
                .Select(k => new
                {
                    k.KeywordId,
                    k.Keyword,
                    k.CreatedDate,
                    k.FaqId,
                    FaqTitle = k.Faq.Title
                })
                .GroupBy(x => x.Keyword)
                .Select(g => new FaqKeywordListVM
                {
                    KeywordId = g.Max(x => x.KeywordId),
                    Keyword = g.Key,
                    CreatedDate = g.Min(x => x.CreatedDate),
                    Faqs = g.OrderBy(x => x.FaqTitle)
                            .Select(x => new FaqRefVM { FaqId = x.FaqId, Title = x.FaqTitle })
                            .ToList()
                })
                .FirstAsync();

            return View(vm);
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

        // =============== AJAX：單筆刪除 ===============
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
                return Json(new { ok = false, message = "關鍵字被引用或重複，無法刪除" });
            }
        }

        // =============== AJAX：批次刪除 ===============
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

        private bool CsFaqKeywordExists(int id)
            => _context.CsFaqKeywords.Any(e => e.KeywordId == id);

    }
}
