using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.SUP.Rcl.Areas.SUP.Controllers
{
	[Area("SUP")]
	public class StockHistoriesController : Controller
    {
        private readonly tHerdDBContext _context;

        public StockHistoriesController(tHerdDBContext context)
        {
            _context = context;
        }

		// GET: SUP/StockHistories/Index
		[HttpGet]
		public IActionResult StockHistoriesIndex()
		{
			return View();
		}

		// POST: SUP/SupStockHistories/IndexJson
		[HttpPost]
		public async Task<IActionResult> IndexJson()
		{
			var draw = Request.Form["draw"].FirstOrDefault() ?? "1";
			var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
			var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
			var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";

			// 取得排序資訊
			var sortColumnIndex = Convert.ToInt32(Request.Form["order[0][column]"].FirstOrDefault() ?? "0");
			var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault() ?? "desc";

			// 查詢 SupStockHistories，包含 StockBatch → Sku → Product → Brand
			var query = _context.SupStockHistories
				.Include(h => h.StockBatch)
					.ThenInclude(b => b.Sku)
						.ThenInclude(s => s.Product)
							.ThenInclude(p => p.Brand)
				.Include(h => h.StockBatch.Sku.SpecificationOptions) // 取得 SKU 規格
				.AsQueryable();

			// 搜尋條件：SKU、商品名稱、品牌名稱、批號、異動類型
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(h =>
					EF.Functions.Like(h.StockBatch.Sku.SkuCode, $"%{searchValue}%") ||
					EF.Functions.Like(h.StockBatch.Sku.Product.ProductName, $"%{searchValue}%") ||
					EF.Functions.Like(h.StockBatch.Sku.Product.Brand.BrandName, $"%{searchValue}%") ||
					EF.Functions.Like(h.StockBatch.BatchNumber, $"%{searchValue}%") ||
					EF.Functions.Like(h.ChangeType, $"%{searchValue}%")
				);
			}

			var recordsTotal = await query.CountAsync();

			// 動態排序
			query = sortColumnIndex switch
			{
				0 => sortDirection == "asc" ? query.OrderBy(h => h.RevisedDate) : query.OrderByDescending(h => h.RevisedDate),
				1 => sortDirection == "asc" ? query.OrderBy(h => h.StockBatch.Sku.SkuCode) : query.OrderByDescending(h => h.StockBatch.Sku.SkuCode),
				2 => sortDirection == "asc" ? query.OrderBy(h => h.StockBatch.Sku.Product.Brand.BrandName) : query.OrderByDescending(h => h.StockBatch.Sku.Product.Brand.BrandName),
				3 => sortDirection == "asc" ? query.OrderBy(h => h.StockBatch.Sku.Product.ProductName) : query.OrderByDescending(h => h.StockBatch.Sku.Product.ProductName),
				4 => sortDirection == "asc" ? query.OrderBy(h => h.StockBatch.BatchNumber) : query.OrderByDescending(h => h.StockBatch.BatchNumber),
				5 => sortDirection == "asc" ? query.OrderBy(h => h.ChangeType) : query.OrderByDescending(h => h.ChangeType),
				6 => sortDirection == "asc" ? query.OrderBy(h => h.ChangeQty) : query.OrderByDescending(h => h.ChangeQty),
				_ => query.OrderByDescending(h => h.RevisedDate)
			};

			var data = await query
				.OrderByDescending(h => h.RevisedDate)
				.Skip(start)
				.Take(length)
				.Select(h => new
				{
					ChangedAt = h.RevisedDate,
					RefDocumentType = h.ChangeType,
					RefDocumentId = h.OrderItem != null ? h.OrderItem.Order.OrderNo : "",
					SkuCode = h.StockBatch.Sku.SkuCode,
					ProductName = h.StockBatch.Sku.Product.ProductName,
					BrandName = h.StockBatch.Sku.Product.Brand.BrandName,
					SpecOptions = h.StockBatch.Sku.SpecificationOptions
								   .OrderBy(o => o.OrderSeq)
								   .Select(o => o.OptionName)
								   .ToList(), // 傳規格選項列表
					ChangeQty = (h.ChangeType == "Sale" ? -h.ChangeQty : h.ChangeQty),
					UnitCost = h.StockBatch.Sku.CostPrice ?? 0m,
					TotalCost = (h.StockBatch.Sku.CostPrice ?? 0m) * h.ChangeQty,
					ChangedBy = h.Reviser
				})
				.ToListAsync();


			return Json(new
			{
				draw,
				recordsTotal,
				recordsFiltered = recordsTotal,
				data
			});
		}


		// GET: SUP/StockHistories/Details/5
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

		// GET: SUP/StockHistories/Create
		public IActionResult Create()
        {
            ViewData["StockBatchId"] = new SelectList(_context.SupStockBatches, "StockBatchId", "BatchNumber");
            return View();
        }

		// POST: SUP/StockHistories/Create
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

		// GET: SUP/StockHistories/Edit/5
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

		// POST: SUP/StockHistories/Edit/5
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

		// GET: SUP/StockHistories/Delete/5
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

		// POST: SUP/StockHistories/Delete/5
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

		public async Task<IActionResult> RecordStockChangeForOrder(int orderItemId, string movementType, int changeQty, int userId, string remark = "")
		{
			// 1. 取得訂單明細，包含 SKU 與 Product
			var orderItem = await _context.OrdOrderItems
				.Include(oi => oi.Sku)
				.ThenInclude(s => s.Product)
				.FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

			if (orderItem == null)
				return BadRequest("找不到訂單明細");

			var sku = orderItem.Sku;

			// 2. 取得該 SKU 的可用批次（例如先進先出）
			var stockBatch = await _context.SupStockBatches
				.Where(sb => sb.SkuId == sku.SkuId && sb.Qty > 0)
				.OrderBy(sb => sb.CreatedDate)
				.FirstOrDefaultAsync();

			if (stockBatch == null)
				return BadRequest("該 SKU 無可用庫存批次");

			// 3. 計算新的批次庫存
			int newQty = stockBatch.Qty;

			switch (movementType)
			{
				case "Sale":    // 出庫
					newQty -= changeQty;
					break;
				case "Return":  // 退貨回庫
					newQty += changeQty;
					break;
				default:
					return BadRequest("無效的異動類型");
			}

			// 4. 更新 SupStockBatch
			int beforeQty = stockBatch.Qty;
			stockBatch.Qty = newQty;
			await _context.SaveChangesAsync();

			// 5. 記錄 SupStockHistory
			var history = new SupStockHistory
			{
				StockBatchId = stockBatch.StockBatchId,
				OrderItemId = orderItem.OrderItemId,
				ChangeType = movementType,
				ChangeQty = changeQty,
				BeforeQty = beforeQty,
				AfterQty = newQty,
				Reviser = userId,
				RevisedDate = DateTime.Now,
				Remark = remark
			};

			// 單位成本價與成本
			decimal unitCost = sku.CostPrice ?? 0m;
			decimal totalCost = unitCost * changeQty;

			// 如果 SupStockHistory 有額外欄位存成本，可以這樣賦值
			// history.UnitCost = unitCost;
			// history.TotalCost = totalCost;

			_context.SupStockHistories.Add(history);
			await _context.SaveChangesAsync();

			return Json(new { success = true, stockBatchId = stockBatch.StockBatchId, newQty });
		}

		/// <summary>
		/// 更新庫存並記錄異動（出庫 / 回庫）
		/// </summary>
		/// <param name="orderItemId">訂單明細ID</param>
		/// <param name="movementType">異動類型: "Sale" = 銷售出庫, "Return" = 退貨回庫</param>
		/// <param name="changeQty">異動數量</param>
		/// <param name="userId">異動人員ID</param>
		/// <param name="remark">備註，可選</param>
		public async Task<IActionResult> UpdateStockAndRecordHistory(int orderItemId, string movementType, int changeQty, int userId, string remark = "")
		{
			// 1. 取得訂單明細及 SKU 與 Product
			var orderItem = await _context.OrdOrderItems
				.Include(oi => oi.Sku)
					.ThenInclude(s => s.Product)
				.FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

			if (orderItem == null)
				return BadRequest("找不到訂單明細");

			var sku = orderItem.Sku;

			// 2. 取得可用批次（先進先出）
			var stockBatch = await _context.SupStockBatches
				.Where(sb => sb.SkuId == sku.SkuId && sb.Qty > 0)
				.OrderBy(sb => sb.CreatedDate)
				.FirstOrDefaultAsync();

			if (stockBatch == null)
				return BadRequest("該 SKU 無可用庫存批次");

			// 3. 計算庫存變化
			int beforeQty = stockBatch.Qty;
			int afterQty;

			switch (movementType)
			{
				case "Sale":    // 出庫
					afterQty = beforeQty - changeQty;
					if (afterQty < 0) return BadRequest("庫存不足，無法出庫");
					break;

				case "Return":  // 回庫
					afterQty = beforeQty + changeQty;
					break;

				default:
					return BadRequest("無效的異動類型");
			}

			// 4. 更新 SupStockBatch
			stockBatch.Qty = afterQty;
			stockBatch.Reviser = userId;
			stockBatch.RevisedDate = DateTime.Now;
			_context.SupStockBatches.Update(stockBatch);

			// 5. 記錄 SupStockHistory
			var unitCost = sku.CostPrice ?? 0m;
			var totalCost = unitCost * changeQty;

			var history = new SupStockHistory
			{
				StockBatchId = stockBatch.StockBatchId,
				OrderItemId = orderItem.OrderItemId,
				ChangeType = movementType,
				ChangeQty = changeQty,
				BeforeQty = beforeQty,
				AfterQty = afterQty,
				Reviser = userId,
				RevisedDate = DateTime.Now,
				Remark = remark
				// 如果 SupStockHistory 有欄位存成本，可補充：
				// UnitCost = unitCost,
				// TotalCost = totalCost
			};

			_context.SupStockHistories.Add(history);

			await _context.SaveChangesAsync();

			return Json(new
			{
				success = true,
				stockBatchId = stockBatch.StockBatchId,
				skuCode = sku.SkuCode,
				beforeQty,
				afterQty,
				unitCost,
				totalCost
			});
		}





	}
}
