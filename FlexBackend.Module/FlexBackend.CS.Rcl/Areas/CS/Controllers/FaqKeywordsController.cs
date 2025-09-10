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

        // GET: CsFaqKeywords
        public async Task<IActionResult> Index()
        {
            var therd_store_dbContext = _context.CsFaqKeywords.Include(c => c.Faq);
            return View(await therd_store_dbContext.ToListAsync());
        }

        // GET: CsFaqKeywords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csFaqKeyword = await _context.CsFaqKeywords
                .Include(c => c.Faq)
                .FirstOrDefaultAsync(m => m.KeywordId == id);
            if (csFaqKeyword == null)
            {
                return NotFound();
            }

            return View(csFaqKeyword);
        }

        // GET: CsFaqKeywords/Create
        public IActionResult Create()
        {
            ViewData["FaqId"] = new SelectList(_context.CsFaqs, "FaqId", "AnswerHtml");
            return View();
        }

        // POST: CsFaqKeywords/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KeywordId,FaqId,Keyword,CreatedDate")] CsFaqKeyword csFaqKeyword)
        {
            if (ModelState.IsValid)
            {
                _context.Add(csFaqKeyword);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FaqId"] = new SelectList(_context.CsFaqs, "FaqId", "AnswerHtml", csFaqKeyword.FaqId);
            return View(csFaqKeyword);
        }

        // GET: CsFaqKeywords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csFaqKeyword = await _context.CsFaqKeywords.FindAsync(id);
            if (csFaqKeyword == null)
            {
                return NotFound();
            }
            ViewData["FaqId"] = new SelectList(_context.CsFaqs, "FaqId", "AnswerHtml", csFaqKeyword.FaqId);
            return View(csFaqKeyword);
        }

        // POST: CsFaqKeywords/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("KeywordId,FaqId,Keyword,CreatedDate")] CsFaqKeyword csFaqKeyword)
        {
            if (id != csFaqKeyword.KeywordId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(csFaqKeyword);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CsFaqKeywordExists(csFaqKeyword.KeywordId))
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
            ViewData["FaqId"] = new SelectList(_context.CsFaqs, "FaqId", "AnswerHtml", csFaqKeyword.FaqId);
            return View(csFaqKeyword);
        }

        // GET: CsFaqKeywords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csFaqKeyword = await _context.CsFaqKeywords
                .Include(c => c.Faq)
                .FirstOrDefaultAsync(m => m.KeywordId == id);
            if (csFaqKeyword == null)
            {
                return NotFound();
            }

            return View(csFaqKeyword);
        }

        // POST: CsFaqKeywords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var csFaqKeyword = await _context.CsFaqKeywords.FindAsync(id);
            if (csFaqKeyword != null)
            {
                _context.CsFaqKeywords.Remove(csFaqKeyword);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CsFaqKeywordExists(int id)
        {
            return _context.CsFaqKeywords.Any(e => e.KeywordId == id);
        }
    }
}
