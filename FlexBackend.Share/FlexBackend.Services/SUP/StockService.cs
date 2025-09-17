using FlexBackend.Core.DTOs.SUP;
using FlexBackend.Core.Interfaces.SUP;
using FlexBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;
using SupStockHistory = FlexBackend.Infra.Models.SupStockHistory;


namespace FlexBackend.Services.SUP
{
	public class StockService : IStockService
	{
		private readonly tHerdDBContext _context;
		public StockService(tHerdDBContext context) => _context = context;


		/// <summary>
		/// 出庫(Sale)/調整(Adjust)
		/// FIFO 扣庫存(先比ExpireDate再比CreatedDate)
		/// 有效期最早的先出
		/// 沒有設定效期排到最後
		/// 同效期則先建檔先出
		/// 更新異動紀錄(SupStockHistory)
		/// 更新SKU總庫存(ProdProductSkus.StockQty)
		/// 
		/// 目前只處理 Purchase（採購入庫） 與 Adjust（手動調整）
		/// </summary>
		public async Task<StockAdjustResultDto> AdjustStockAsync(
			int batchId,
			int skuId,
			int changeQty,        // 前端正整數
			bool isAdd,           // 是否增加 (手動調整專用)
			string movementType,  // 異動類型: "Purchase", "Adjust", "Order", etc.
			int reviserId,
			string remark)
		{
			if (changeQty <= 0)
				throw new InvalidOperationException("異動數量必須大於 0");

			var result = new StockAdjustResultDto
			{
				SkuId = skuId,
				BatchMovements = new List<SupStockMovementDto>()
			};

			// 取得 SKU
			var sku = await _context.ProdProductSkus
				.Include(s => s.Product)
					.ThenInclude(p => p.Brand)
				.FirstOrDefaultAsync(s => s.SkuId == skuId);

			if (sku == null)
				return new StockAdjustResultDto { Success = false, Message = "找不到 SKU 資料" };

			int beforeSkuQty = sku.StockQty; // 異動前 SKU 總庫存
			int remainingChangeQty = changeQty;

			if (isAdd)
			{
				// 上限檢查
				int maxAddQty = sku.MaxStockQty - sku.StockQty;
				int actualAddQty = Math.Min(remainingChangeQty, maxAddQty);
				if (actualAddQty <= 0)
					return new StockAdjustResultDto { Success = false, Message = "已達最大庫存" };

				// 更新批號 Qty
				var batch = await _context.SupStockBatches.FirstOrDefaultAsync(b => b.StockBatchId == batchId);
				if (batch == null) return new StockAdjustResultDto { Success = false, Message = "找不到批號資料" };

				int batchBeforeQty = batch.Qty;
				batch.Qty += actualAddQty;
				batch.Reviser = reviserId;
				batch.RevisedDate = DateTime.Now;

				// 更新 SKU 總庫存
				sku.StockQty += actualAddQty;

				// 異動紀錄
				_context.SupStockHistories.Add(new SupStockHistory
				{
					StockBatchId = batch.StockBatchId,
					ChangeType = movementType,
					ChangeQty = actualAddQty,
					BeforeQty = beforeSkuQty,
					AfterQty = sku.StockQty,
					Reviser = reviserId,
					RevisedDate = DateTime.Now,
					Remark = remark
				});

				result.BatchMovements.Add(new SupStockMovementDto
				{
					SkuId = skuId,
					StockBatchId = batch.StockBatchId,
					MovementType = movementType,
					ChangeQty = actualAddQty,
					IsAdd = true,
					CurrentQty = batchBeforeQty,
					AfterQty = batch.Qty,
					SkuCode = sku.SkuCode,
					ProductName = sku.Product.ProductName,
					BrandName = sku.Product.Brand.BrandName,
					BatchNumber = batch.BatchNumber,
					PredictedQty = sku.StockQty
				});

				result.AdjustedQty = actualAddQty;
			}
			else
			{
				// 扣庫 (FIFO)
				var batches = await _context.SupStockBatches
					.Where(b => b.SkuId == skuId && b.Qty > 0)
					.OrderBy(b => b.ExpireDate ?? DateTime.MaxValue)
					.ThenBy(b => b.CreatedDate)
					.ToListAsync();

				if (!batches.Any())
					return new StockAdjustResultDto { Success = false, Message = "無可扣庫存批號" };

				foreach (var batch in batches)
				{
					if (remainingChangeQty <= 0) break;

					int deductQty = Math.Min(batch.Qty, remainingChangeQty);
					int batchBeforeQty = batch.Qty;

					batch.Qty -= deductQty;
					batch.Reviser = reviserId;
					batch.RevisedDate = DateTime.Now;

					int skuBeforeQtyForThisBatch = sku.StockQty;
					sku.StockQty -= deductQty;

					_context.SupStockHistories.Add(new SupStockHistory
					{
						StockBatchId = batch.StockBatchId,
						ChangeType = movementType,
						ChangeQty = deductQty,
						BeforeQty = skuBeforeQtyForThisBatch,
						AfterQty = sku.StockQty,
						Reviser = reviserId,
						RevisedDate = DateTime.Now,
						Remark = remark
					});

					result.BatchMovements.Add(new SupStockMovementDto
					{
						SkuId = skuId,
						StockBatchId = batch.StockBatchId,
						MovementType = movementType,
						ChangeQty = deductQty,
						IsAdd = false,
						CurrentQty = batchBeforeQty,
						AfterQty = batch.Qty,
						SkuCode = sku.SkuCode,
						ProductName = sku.Product.ProductName,
						BrandName = sku.Product.Brand.BrandName,
						BatchNumber = batch.BatchNumber,
						PredictedQty = sku.StockQty
					});

					remainingChangeQty -= deductQty;
				}

				result.AdjustedQty = changeQty - remainingChangeQty;
			}

			await _context.SaveChangesAsync();

			result.Success = true;
			result.TotalStock = sku.StockQty;
			result.PredictedQty = sku.StockQty;
			result.Message = "庫存調整成功";

			return result;
		}



		/// <summary>
		/// 退貨(Return)
		/// 回原批次(依batchIds傳入順序)
		/// 當退貨數量大於單批次可回庫量時，自動拆分多批次回庫
		/// 批次可回庫量=MaxStockQty-當前批次數量
		/// 回傳StockAdjustResultDto(AdjustedQty/RemainingQty)
		/// 更新異動紀錄
		/// 更新SKU總庫存
		/// </summary>
		public async Task<StockAdjustResultDto> ReturnStockAsync(
			int skuId,
			int changeQty,
			List<int> batchIds,
			int reviserId = 0,
			string remark = null)
		{
			if (changeQty <= 0)
				return new StockAdjustResultDto
				{
					SkuId = skuId,
					TotalStock = await _context.ProdProductSkus
						.Where(s => s.SkuId == skuId)
						.SumAsync(s => s.StockQty),
					Success = true,
					AdjustedQty = 0,
					RemainingQty = 0
				};

			if (batchIds == null || !batchIds.Any())
				throw new ArgumentException("退貨必須指定批次ID");

			// 取得退貨批次，依傳入順序回庫
			var batches = await _context.SupStockBatches
				.Where(b => batchIds.Contains(b.StockBatchId))
				.OrderBy(b => batchIds.IndexOf(b.StockBatchId))
				.ToListAsync();

			if (batches.Count != batchIds.Count)
				throw new InvalidOperationException("部分指定批次不存在，無法退貨");

			int remaining = changeQty;
			int adjustedQty = 0;

			foreach (var batch in batches)
			{
				if (remaining <= 0) break;

				int beforeQty = batch.Qty;
				// 批次可回庫量 = MaxStockQty - 當前批次數量
				int maxReturnable = batch.Sku?.MaxStockQty - batch.Qty ?? int.MaxValue;
				int qtyToAdd = Math.Min(remaining, maxReturnable);

				if (qtyToAdd <= 0) continue; // 批次已滿，跳過

				batch.Qty += qtyToAdd;
				remaining -= qtyToAdd;
				adjustedQty += qtyToAdd;

				_context.SupStockHistories.Add(new SupStockHistory
				{
					StockBatchId = batch.StockBatchId,
					ChangeType = "Return",
					ChangeQty = qtyToAdd,
					Reviser = reviserId,
					RevisedDate = DateTime.Now,
					BeforeQty = beforeQty,
					AfterQty = batch.Qty,
					Remark = remark
				});
			}

			// 更新 SKU 總庫存
			var sku = await _context.ProdProductSkus.FirstOrDefaultAsync(s => s.SkuId == skuId);
			if (sku != null)
			{
				sku.StockQty = await _context.SupStockBatches
					.Where(b => b.SkuId == skuId)
					.SumAsync(b => b.Qty);
			}

			await _context.SaveChangesAsync();

			return new StockAdjustResultDto
			{
				SkuId = skuId,
				TotalStock = sku?.StockQty ?? 0,
				Success = true,
				AdjustedQty = adjustedQty,
				RemainingQty = remaining
			};
		}



		/// <summary>
		/// 取得SKU批次列表
		/// 先ExpireDate再CreatedDate
		/// 日期只取年月日
		/// </summary>
		public async Task<List<SupStockBatchDto>> GetBatchesBySkuAsync(int skuId, bool forDecrease = false)
		{
			var query = _context.SupStockBatches
				.Where(b => b.SkuId == skuId);

			if (forDecrease)
			{
				// 扣庫存才需要 Qty > 0
				query = query.Where(b => b.Qty > 0);
			}

			return await query
				.OrderBy(b => b.ExpireDate.HasValue ? b.ExpireDate.Value.Date : DateTime.MaxValue)
				.ThenBy(b => b.CreatedDate)
				.Select(b => new SupStockBatchDto
				{
					StockBatchId = b.StockBatchId,
					SkuId = b.SkuId,
					BatchNumber = b.BatchNumber,
					Qty = b.Qty,
					ExpireDate = b.ExpireDate.HasValue ? b.ExpireDate.Value.Date : (DateTime?)null,
					ManufactureDate = b.ManufactureDate.HasValue ? b.ManufactureDate.Value.Date : (DateTime?)null,
					IsSellable = b.IsSellable,
					MaxStockQty = b.Sku.MaxStockQty,
					ReorderPoint = b.Sku.ReorderPoint,
					SafetyStockQty = b.Sku.SafetyStockQty,
					IsAllowBackorder = b.Sku.IsAllowBackorder
				})
				.ToListAsync();
		}




	}
}