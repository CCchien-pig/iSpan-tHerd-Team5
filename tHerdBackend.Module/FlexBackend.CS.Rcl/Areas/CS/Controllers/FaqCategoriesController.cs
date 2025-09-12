using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
		// === 1) 給 Index.cshtml AJAX 載入的 Partial（分類列表表格） ===
		[HttpGet]
		public async Task<IActionResult> IndexPartial()
		{
			var list = await _context.CsFaqCategories
									 .OrderBy(c => c.OrderSeq)
									 .ToListAsync();
			// 依你的實際路徑調整。如果放在 Areas/CS/Views/Shared 以外，要換成對應路徑
			return PartialView("~/Areas/CS/Views/Shared/_FaqCategoriesTable.cshtml", list);
		}

		// === 2) AJAX：切換啟用/停用（供列表的開關用） ===
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleActive(int id, bool isActive)
		{
			var cat = await _context.CsFaqCategories.FindAsync(id);
			if (cat == null)
				return NotFound(new { ok = false, message = "找不到分類" });

			cat.IsActive = isActive;
			cat.RevisedDate = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return Json(new { ok = true });
		}

		// === 3) AJAX：單筆刪除（供列表上的「刪除」按鈕） ===
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteAjax(int id)
		{
			var cat = await _context.CsFaqCategories.FindAsync(id);
			if (cat == null)
				return NotFound(new { ok = false, message = "找不到分類" });

			try
			{
				_context.CsFaqCategories.Remove(cat);
				await _context.SaveChangesAsync();
				return Json(new { ok = true });
			}
			catch (DbUpdateException)
			{
				return Json(new { ok = false, message = "分類被引用，無法刪除" });
			}
		}

		// === 4) AJAX：批次刪除（供「刪除所選」） ===
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
				return Json(new { ok = false, message = "部分分類被引用，無法刪除" });
			}
		}

		// GET: CsFaqCategories
		public async Task<IActionResult> Index()
        {
            var therd_store_dbContext = _context.CsFaqCategories.Include(c => c.ParentCategory);
            return View(await therd_store_dbContext.ToListAsync());
        }

        // GET: CsFaqCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csFaqCategory = await _context.CsFaqCategories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (csFaqCategory == null)
            {
                return NotFound();
            }

            return View(csFaqCategory);
        }

        // GET: CsFaqCategories/Create
        public IActionResult Create()
        {
            ViewData["ParentCategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName");
            return View();
        }

        // POST: CsFaqCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,ParentCategoryId,CategoryName,OrderSeq,IsActive,CreatedDate,RevisedDate")] CsFaqCategory csFaqCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(csFaqCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentCategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csFaqCategory.ParentCategoryId);
            return View(csFaqCategory);
        }

        // GET: CsFaqCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csFaqCategory = await _context.CsFaqCategories.FindAsync(id);
            if (csFaqCategory == null)
            {
                return NotFound();
            }
            ViewData["ParentCategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csFaqCategory.ParentCategoryId);
            return View(csFaqCategory);
        }

        // POST: CsFaqCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,ParentCategoryId,CategoryName,OrderSeq,IsActive,CreatedDate,RevisedDate")] CsFaqCategory csFaqCategory)
        {
            if (id != csFaqCategory.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(csFaqCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CsFaqCategoryExists(csFaqCategory.CategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentCategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csFaqCategory.ParentCategoryId);
            return View(csFaqCategory);
        }

        // GET: CsFaqCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csFaqCategory = await _context.CsFaqCategories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (csFaqCategory == null)
            {
                return NotFound();
            }

            return View(csFaqCategory);
        }

        // POST: CsFaqCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var csFaqCategory = await _context.CsFaqCategories.FindAsync(id);
            if (csFaqCategory != null)
            {
                _context.CsFaqCategories.Remove(csFaqCategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CsFaqCategoryExists(int id)
        {
            return _context.CsFaqCategories.Any(e => e.CategoryId == id);
        }
    }
}
