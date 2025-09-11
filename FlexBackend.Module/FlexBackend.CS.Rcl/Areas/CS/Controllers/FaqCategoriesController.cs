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
