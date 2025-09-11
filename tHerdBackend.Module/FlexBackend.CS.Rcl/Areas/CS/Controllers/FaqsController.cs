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

        // GET: CsFaqs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csFaq = await _context.CsFaqs.FindAsync(id);
            if (csFaq == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csFaq.CategoryId);
            return View(csFaq);
        }

        // POST: CsFaqs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FaqId,Title,AnswerHtml,Status,CategoryId,OrderSeq,LastPublishedTime,IsActive,CreatedDate,RevisedDate")] CsFaq csFaq)
        {
            if (id != csFaq.FaqId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(csFaq);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CsFaqExists(csFaq.FaqId))
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
            ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csFaq.CategoryId);
            return View(csFaq);
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
