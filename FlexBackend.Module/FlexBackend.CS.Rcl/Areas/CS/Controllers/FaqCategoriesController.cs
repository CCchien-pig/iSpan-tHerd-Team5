using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FlexBackend.CS.Rcl.Areas.CS.ViewModels;

namespace FlexBackend.CS.Rcl.Areas.CS.Controllers
{
    [Area("CS")]
    public class FaqCategoriesController : Controller
    {
        private readonly tHerdDBContext _context;

        public FaqCategoriesController(tHerdDBContext context)
        {
            _context = context;
        }

        // ========= Index：用 VM 投影（帶上層名稱、FAQ 數量、排序、時間等）=========
        public async Task<IActionResult> Index()
        {
            var list = await _context.CsFaqCategories
                .Select(c => new FaqCategoryListVM
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null,
                    OrderSeq = c.OrderSeq,
                    IsActive = c.IsActive,
                    CreatedDate = c.CreatedDate,
                    RevisedDate = c.RevisedDate,
                    ActiveFaqCount = _context.CsFaqs.Count(f => f.CategoryId == c.CategoryId && f.IsActive),
                    TotalFaqCount = _context.CsFaqs.Count(f => f.CategoryId == c.CategoryId)
                })
                .OrderBy(x => x.OrderSeq).ThenBy(x => x.CategoryName)
                .AsNoTracking()
                .ToListAsync();

            return View(list);
        }

        // （可選）給 Index.cshtml AJAX 載入的 Partial
        [HttpGet]
        public async Task<IActionResult> IndexPartial()
        {
            var list = await _context.CsFaqCategories
                                     .OrderBy(c => c.OrderSeq)
                                     .ToListAsync();
            return PartialView("~/Areas/CS/Views/Shared/_FaqCategoriesTable.cshtml", list);
        }

        // ===== AJAX：切換啟用/停用 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, bool isActive)
        {
            var cat = await _context.CsFaqCategories.FindAsync(id);
            if (cat == null) return NotFound(new { ok = false, message = "找不到分類" });

            cat.IsActive = isActive;
            cat.RevisedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Json(new { ok = true });
        }

        // ===== AJAX：更新排序 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int id, int orderSeq)
        {
            var cat = await _context.CsFaqCategories.FindAsync(id);
            if (cat == null) return NotFound(new { ok = false, message = "找不到分類" });

            if (orderSeq < 0) orderSeq = 0;
            cat.OrderSeq = orderSeq;
            cat.RevisedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Json(new { ok = true, order = cat.OrderSeq });
        }

        // ===== AJAX：單筆刪除 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var cat = await _context.CsFaqCategories.FindAsync(id);
            if (cat == null) return NotFound(new { ok = false, message = "找不到分類" });

            try
            {
                _context.CsFaqCategories.Remove(cat);
                await _context.SaveChangesAsync();
                return Json(new { ok = true });
            }
            catch (DbUpdateException)
            {
                return Json(new { ok = false, message = "分類被引用或有子分類，無法刪除" });
            }
        }

        // ===== AJAX：批次刪除 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteManyAjax([FromForm] int[] ids)
        {
            if (ids == null || ids.Length == 0)
                return Json(new { ok = false, message = "沒有要刪除的項目" });

            var list = await _context.CsFaqCategories
                                     .Where(c => ids.Contains(c.CategoryId))
                                     .ToListAsync();
            if (list.Count == 0)
                return Json(new { ok = false, message = "找不到要刪除的項目" });

            try
            {
                _context.CsFaqCategories.RemoveRange(list);
                await _context.SaveChangesAsync();
                return Json(new { ok = true, count = list.Count });
            }
            catch (DbUpdateException)
            {
                return Json(new { ok = false, message = "部分分類被引用或有子分類，無法刪除" });
            }
        }

        // ===== Scaffold 預設頁（保留） =====

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var csFaqCategory = await _context.CsFaqCategories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (csFaqCategory == null) return NotFound();

            return View(csFaqCategory);
        }

        public IActionResult Create()
        {
            ViewData["ParentCategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,ParentCategoryId,CategoryName,OrderSeq,IsActive")] CsFaqCategory cat)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ParentCategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", cat.ParentCategoryId);
                return View(cat);
            }

            cat.CreatedDate = DateTime.UtcNow;
            cat.RevisedDate = null;

            _context.Add(cat);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cat = await _context.CsFaqCategories.FindAsync(id);
            if (cat == null) return NotFound();

            ViewData["ParentCategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", cat.ParentCategoryId);
            return View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,ParentCategoryId,CategoryName,OrderSeq,IsActive,CreatedDate")] CsFaqCategory input)
        {
            if (id != input.CategoryId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["ParentCategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", input.ParentCategoryId);
                return View(input);
            }

            var db = await _context.CsFaqCategories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (db == null) return NotFound();

            db.ParentCategoryId = input.ParentCategoryId;
            db.CategoryName = input.CategoryName;
            db.OrderSeq = input.OrderSeq;
            db.IsActive = input.IsActive;
            db.RevisedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cat = await _context.CsFaqCategories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (cat == null) return NotFound();

            return View(cat);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cat = await _context.CsFaqCategories.FindAsync(id);
            if (cat != null)
            {
                _context.CsFaqCategories.Remove(cat);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CsFaqCategoryExists(int id)
            => _context.CsFaqCategories.Any(e => e.CategoryId == id);
    }
}
