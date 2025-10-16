using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;



namespace test2.Controllers
{
	[Area("CS")]
	public class TicketsController : Controller
    {
        private readonly tHerdDBContext _context;

        public TicketsController(tHerdDBContext context)
        {
            _context = context;
        }

        // GET: CsTickets
        public async Task<IActionResult> Index()
        {
            var therdStoreContext = _context.CsTickets.Include(c => c.Category);
            return View(await therdStoreContext.ToListAsync());
        }

        // GET: CsTickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csTicket = await _context.CsTickets
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.TicketId == id);
            if (csTicket == null)
            {
                return NotFound();
            }

            return View(csTicket);
        }

        // GET: CsTickets/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName");
            return View();
        }

        // POST: CsTickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TicketId,UserId,CategoryId,Subject,Status,Priority,CreatedDate,RevisedDate")] CsTicket csTicket)
        {
            if (ModelState.IsValid)
            {
                _context.Add(csTicket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csTicket.CategoryId);
            return View(csTicket);
        }

        // GET: CsTickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csTicket = await _context.CsTickets.FindAsync(id);
            if (csTicket == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csTicket.CategoryId);
            return View(csTicket);
        }

        // POST: CsTickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TicketId,UserId,CategoryId,Subject,Status,Priority,CreatedDate,RevisedDate")] CsTicket csTicket)
        {
            if (id != csTicket.TicketId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(csTicket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CsTicketExists(csTicket.TicketId))
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
            ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories, "CategoryId", "CategoryName", csTicket.CategoryId);
            return View(csTicket);
        }

        // GET: CsTickets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var csTicket = await _context.CsTickets
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.TicketId == id);
            if (csTicket == null)
            {
                return NotFound();
            }

            return View(csTicket);
        }

        // POST: CsTickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var csTicket = await _context.CsTickets.FindAsync(id);
            if (csTicket != null)
            {
                _context.CsTickets.Remove(csTicket);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CsTicketExists(int id)
        {
            return _context.CsTickets.Any(e => e.TicketId == id);
        }
    }
}
