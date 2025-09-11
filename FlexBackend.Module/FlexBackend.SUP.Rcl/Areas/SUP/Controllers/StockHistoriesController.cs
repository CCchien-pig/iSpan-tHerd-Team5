using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.SUP.Rcl.Areas.SUP.Controllers
{
    public class StockHistoriesController : Controller
    {
        private readonly tHerdDBContext _context;

        public StockHistoriesController(tHerdDBContext context)
        {
            _context = context;
        }

		// GET: SupStockHistories/StockHistoriesIndex
		[HttpGet]
		public IActionResult StockHistoriesIndex()
		{
			return View();
		}

		// GET: SupStockHistories/Details/5
		public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supStockHistory = await _context.SupStockHistories
                .Include(s => s.StockBatch)
                .FirstOrDefaultAsync(m => m.StockHistoryId == id);
            if (supStockHistory == null)
            {
                return NotFound();
            }

            return View(supStockHistory);
        }

        // GET: SupStockHistories/Create
        public IActionResult Create()
        {
            ViewData["StockBatchId"] = new SelectList(_context.SupStockBatches, "StockBatchId", "BatchNumber");
            return View();
        }

        // POST: SupStockHistories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StockHistoryId,StockBatchId,OrderItemId,ChangeType,ChangeQty,Reviser,RevisedDate,BeforeQty,AfterQty,Remark")] SupStockHistory supStockHistory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(supStockHistory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["StockBatchId"] = new SelectList(_context.SupStockBatches, "StockBatchId", "BatchNumber", supStockHistory.StockBatchId);
            return View(supStockHistory);
        }

        // GET: SupStockHistories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supStockHistory = await _context.SupStockHistories.FindAsync(id);
            if (supStockHistory == null)
            {
                return NotFound();
            }
            ViewData["StockBatchId"] = new SelectList(_context.SupStockBatches, "StockBatchId", "BatchNumber", supStockHistory.StockBatchId);
            return View(supStockHistory);
        }

        // POST: SupStockHistories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StockHistoryId,StockBatchId,OrderItemId,ChangeType,ChangeQty,Reviser,RevisedDate,BeforeQty,AfterQty,Remark")] SupStockHistory supStockHistory)
        {
            if (id != supStockHistory.StockHistoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supStockHistory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupStockHistoryExists(supStockHistory.StockHistoryId))
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
            ViewData["StockBatchId"] = new SelectList(_context.SupStockBatches, "StockBatchId", "BatchNumber", supStockHistory.StockBatchId);
            return View(supStockHistory);
        }

        // GET: SupStockHistories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supStockHistory = await _context.SupStockHistories
                .Include(s => s.StockBatch)
                .FirstOrDefaultAsync(m => m.StockHistoryId == id);
            if (supStockHistory == null)
            {
                return NotFound();
            }

            return View(supStockHistory);
        }

        // POST: SupStockHistories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supStockHistory = await _context.SupStockHistories.FindAsync(id);
            if (supStockHistory != null)
            {
                _context.SupStockHistories.Remove(supStockHistory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SupStockHistoryExists(int id)
        {
            return _context.SupStockHistories.Any(e => e.StockHistoryId == id);
        }
    }
}
