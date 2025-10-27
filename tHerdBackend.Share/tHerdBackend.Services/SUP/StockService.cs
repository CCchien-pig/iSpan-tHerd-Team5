using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;
using SupStockHistory = tHerdBackend.Infra.Models.SupStockHistory;
using tHerdBackend.Core.DTOs.SUP.Stock;


namespace tHerdBackend.Services.SUP
{
	public class StockService : IStockService
	{
		private readonly tHerdDBContext _context;
		public StockService(tHerdDBContext context) => _context = context;


		/// <summary>
		/// FIFO 扣庫存(先比ExpireDate再比CreatedDate)
		/// 有效期最早的先出
		/// 沒有設定效期排到最後
		/// 同效期則先建檔先出
		/// 更新異動紀錄(SupStockHistory)
		/// 更新SKU總庫存(ProdProductSkus.StockQty)
		/// 
		/// 庫存異動服務
		/// 支援：
		/// Purchase (採購入庫)
		/// Adjust   (手動調整 + / -)
		/// Sale/ORD (銷售出庫)
		/// Return   (退貨入庫，若超過最大庫存自動報廢 FIFO)
		/// Expire   (到期報廢 FIFO)
		/// </summary>
		public async Task<StockAdjustResultDto> AdjustStockAsync(
			int batchId,
			int skuId,
			int changeQty,        // 前端傳入正整數
			bool isAdd,           // 手動調整才用
			string movementType,  // "Purchase", "Adjust", "Sale", "Return", "Expire"
			int reviserId,
			string remark,
			int? orderItemId = null // 銷售/退貨要傳訂單明細編號
		)
		{

			var result = new StockAdjustResultDto
			{
				SkuId = skuId,
				BatchMovements = new List<SupStockMovementDto>(),
				ReturnedToOriginal = 0,
				ExpiredQty = 0
			};

			if (changeQty <= 0)
				return Fail(result, "異動數量必須大於 0");

			var sku = await _context.ProdProductSkus
				.Include(s => s.Product).ThenInclude(p => p.Brand)
				.FirstOrDefaultAsync(s => s.SkuId == skuId);

			if (sku == null)
				return Fail(result, "找不到 SKU 資料");

			try
			{
				// ===============================
				// 入庫 / 手動調整+
				// ===============================
				if (isAdd && (movementType == "Purchase" || movementType == "Adjust"))
				{
					int maxAdd = sku.MaxStockQty - sku.StockQty;
					int actualAdd = Math.Min(changeQty, maxAdd);
					if (actualAdd <= 0)
						return Fail(result, "已達最大庫存");

					await AddToBatch(batchId, sku, actualAdd, reviserId, movementType, remark, orderItemId, result);
					result.AdjustedQty = actualAdd;
				}
				// ===============================
				// Sale / 手動調整-
				// ===============================
				else if (!isAdd && (movementType == "Sale" || movementType == "Adjust" || movementType == "Expire"))
				{
					await DeductFromBatches(sku, changeQty, reviserId, movementType, remark, orderItemId, result);
					result.AdjustedQty = changeQty - result.RemainingQty;
				}
				// ===============================
				// Return：退貨入庫 回原批次 + 超過報廢(FIFO)
				// ===============================
				else if (isAdd && movementType == "Return")
				{
					var originalBatches = await _context.SupStockHistories
						.Where(h => h.OrderItemId == orderItemId && h.ChangeType == "Sale")
						.OrderBy(h => h.RevisedDate)
						.Select(h => h.StockBatchId)
						.Distinct()
						.ToListAsync();

					int remainingReturnQty = changeQty;
					int returnedToOriginal = 0;

					var batchesUsed = new List<SupStockBatch>();

					// 回原批次
					foreach (var batchIdInHistory in originalBatches)
					{
						if (remainingReturnQty <= 0) break;

						var batch = await _context.SupStockBatches.FirstOrDefaultAsync(b => b.StockBatchId == batchIdInHistory);
						if (batch == null) continue;

						int maxAdd = sku.MaxStockQty - sku.StockQty;
						if (maxAdd <= 0) break;

						int qtyToAdd = Math.Min(remainingReturnQty, maxAdd);

						await AddToBatch(batch.StockBatchId, sku, qtyToAdd, reviserId, "Return",
							$"退回原批次，自訂備註: {remark}", orderItemId, result);

						returnedToOriginal += qtyToAdd;
						remainingReturnQty -= qtyToAdd;

						batchesUsed.Add(batch); // 記錄剛退回的批次
					}

					int expiredQty = 0;
					if (remainingReturnQty > 0)
					{
						// 報廢時包含剛退回的批次
						var allBatchesForExpire = await _context.SupStockBatches
							.Where(b => b.SkuId == sku.SkuId && b.Qty > 0)
							.OrderBy(b => b.ExpireDate ?? DateTime.MaxValue) // FIFO
							.ThenBy(b => b.CreatedDate)
							.ToListAsync();

						await ExpireBatches(sku, remainingReturnQty, reviserId,
							$"退貨超過最大庫存，自動報廢，自訂備註: {remark}", orderItemId, result, allBatchesForExpire);


						expiredQty = remainingReturnQty;
						remainingReturnQty = 0;
					}

					result.AdjustedQty = changeQty;
					result.ReturnedToOriginal = returnedToOriginal;
					result.ExpiredQty = expiredQty;
				}


				await _context.SaveChangesAsync();

				result.Success = true;
				result.TotalStock = sku.StockQty;
				result.PredictedQty = sku.StockQty;
				result.Message = "庫存異動成功";

				return result;
			}
			catch (Exception ex)
			{
				return Fail(result, $"伺服器錯誤: {ex.Message}");
			}
		}


		#region 🔹 Private Helper Methods

		/// 入庫 (新增到指定批號)
		/// 專責單批號入庫
		/// 更新 batch + sku，寫歷史，填 BatchMovements
		private async Task<bool> AddToBatch(
			int batchId,
			ProdProductSku sku,
			int addQty,
			int reviserId,
			string movementType,
			string remark,
			int? orderItemId,
			StockAdjustResultDto result)
		{
			var batch = await _context.SupStockBatches.FirstOrDefaultAsync(b => b.StockBatchId == batchId);
			if (batch == null)
			{
				Fail(result, $"找不到批號 {batchId} 資料"); // 設定 result
				return false; // 回傳 bool
			}


			int before = batch.Qty;
			batch.Qty += addQty;
			batch.Reviser = reviserId;
			batch.RevisedDate = DateTime.Now;

			sku.StockQty += addQty;

			_context.SupStockHistories.Add(new SupStockHistory
			{
				StockBatchId = batch.StockBatchId,
				ChangeType = movementType,
				ChangeQty = addQty,
				BeforeQty = before,
				AfterQty = batch.Qty,
				Reviser = reviserId,
				RevisedDate = DateTime.Now,
				Remark = remark,
				OrderItemId = orderItemId
			});

			result.BatchMovements.Add(new SupStockMovementDto
			{
				SkuId = sku.SkuId,
				StockBatchId = batch.StockBatchId,
				MovementType = movementType,
				ChangeQty = addQty,
				IsAdd = true,
				CurrentQty = before,
				AfterQty = batch.Qty,
				SkuCode = sku.SkuCode,
				ProductName = sku.Product.ProductName,
				BrandName = sku.Product.Brand.BrandName,
				BatchNumber = batch.BatchNumber,
				PredictedQty = sku.StockQty,
				Remark = remark
			});
			return true;
		}


		/// 出庫 (FIFO 扣庫)
		/// 專責 FIFO 扣庫
		/// 更新多批號，寫歷史，填 BatchMovements
		private async Task DeductFromBatches(
			ProdProductSku sku,
			int deductQty,
			int reviserId,
			string movementType,
			string remark,
			int? orderItemId,
			StockAdjustResultDto result)
		{
			var batches = await _context.SupStockBatches
				.Where(b => b.SkuId == sku.SkuId && b.Qty > 0)
				.OrderBy(b => b.ExpireDate ?? DateTime.MaxValue)
				.ThenBy(b => b.CreatedDate)
				.ToListAsync();

			if (!batches.Any())
			{
				result.Success = false;
				result.Message = "無可扣庫存批號";
				result.RemainingQty = deductQty;
				return;
			}

			int remaining = deductQty;

			foreach (var batch in batches)
			{
				if (remaining <= 0) break;

				int qty = Math.Min(batch.Qty, remaining);
				int before = batch.Qty;

				batch.Qty -= qty;
				batch.Reviser = reviserId;
				batch.RevisedDate = DateTime.Now;

				sku.StockQty -= qty;

				_context.SupStockHistories.Add(new SupStockHistory
				{
					StockBatchId = batch.StockBatchId,
					ChangeType = movementType,
					ChangeQty = qty,
					BeforeQty = before,
					AfterQty = batch.Qty,
					Reviser = reviserId,
					RevisedDate = DateTime.Now,
					Remark = remark,
					OrderItemId = orderItemId
				});

				result.BatchMovements.Add(new SupStockMovementDto
				{
					SkuId = sku.SkuId,
					StockBatchId = batch.StockBatchId,
					MovementType = movementType,
					ChangeQty = qty,
					IsAdd = false,
					CurrentQty = before,
					AfterQty = batch.Qty,
					SkuCode = sku.SkuCode,
					ProductName = sku.Product.ProductName,
					BrandName = sku.Product.Brand.BrandName,
					BatchNumber = batch.BatchNumber,
					PredictedQty = sku.StockQty,
					Remark = remark
				});

				remaining -= qty;
			}

			result.RemainingQty = remaining;
		}


		// 報廢(FIFO)
		private async Task ExpireBatches(
			ProdProductSku sku,
			int expireQty,
			int reviserId,
			string remark,
			int? orderItemId,
			StockAdjustResultDto result,
			List<SupStockBatch>? batches = null // 可自訂批次列表
		)
		{
			if (batches == null)
			{
				batches = await _context.SupStockBatches
					.Where(b => b.SkuId == sku.SkuId && b.Qty > 0)
					.OrderBy(b => b.ExpireDate ?? DateTime.MaxValue)
					.ThenBy(b => b.CreatedDate)
					.ToListAsync();
			}

			int remaining = expireQty;

			foreach (var batch in batches)
			{
				if (remaining <= 0) break;

				int qty = Math.Min(batch.Qty, remaining);
				int before = batch.Qty;

				batch.Qty -= qty;
				batch.Reviser = reviserId;
				batch.RevisedDate = DateTime.Now;

				sku.StockQty -= qty;

				_context.SupStockHistories.Add(new SupStockHistory
				{
					StockBatchId = batch.StockBatchId,
					ChangeType = "Expire",
					ChangeQty = qty,
					BeforeQty = before,
					AfterQty = batch.Qty,
					Reviser = reviserId,
					RevisedDate = DateTime.Now,
					Remark = remark,
					OrderItemId = orderItemId
				});

				result.BatchMovements.Add(new SupStockMovementDto
				{
					SkuId = sku.SkuId,
					StockBatchId = batch.StockBatchId,
					MovementType = "Expire",
					ChangeQty = qty,
					IsAdd = false,
					CurrentQty = before,
					AfterQty = batch.Qty,
					SkuCode = sku.SkuCode,
					ProductName = sku.Product.ProductName,
					BrandName = sku.Product.Brand.BrandName,
					BatchNumber = batch.BatchNumber,
					PredictedQty = sku.StockQty,
					Remark = remark
				});

				remaining -= qty;
			}

			result.RemainingQty = remaining;
		}


		#endregion

		// 回傳失敗
		private StockAdjustResultDto Fail(StockAdjustResultDto result, string message)
		{
			result.Success = false;
			result.Message = message;
			return result;
		}

	}
}