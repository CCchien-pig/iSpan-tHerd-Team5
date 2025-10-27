using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SUP.Stock;

namespace tHerdBackend.Services.SUP
{
	public class StockBatchService : IStockBatchService
	{
		private readonly tHerdDBContext _context;

		public StockBatchService(tHerdDBContext context)
		{
			_context = context;
		}

		public async Task<SupStockBatchDto?> GetStockBatchForEditAsync(int id)
		{
			var supStockBatch = await _context.SupStockBatches
				.Include(sb => sb.Sku)
					.ThenInclude(s => s.Product)
						.ThenInclude(p => p.Brand)
				.FirstOrDefaultAsync(sb => sb.StockBatchId == id);

			if (supStockBatch == null) return null;

			var sku = supStockBatch.Sku;
			var product = sku?.Product;
			var brand = product?.Brand;

			// 計算有效日期
			DateTime? expireDate = null;
			if (supStockBatch.ManufactureDate.HasValue && sku?.ShelfLifeDays > 0)
			{
				expireDate = supStockBatch.ManufactureDate.Value.AddDays(sku.ShelfLifeDays);
			}

			return new SupStockBatchDto
			{
				StockBatchId = supStockBatch.StockBatchId,
				SkuId = supStockBatch.SkuId,
				SkuCode = sku?.SkuCode,
				ProductName = product?.ProductName,
				BrandName = brand?.BrandName,
				BatchNumber = supStockBatch.BatchNumber,
				IsSellable = supStockBatch.IsSellable,
				ManufactureDate = supStockBatch.ManufactureDate,
				ExpireDate = expireDate,
				Qty = supStockBatch.Qty,
				SafetyStockQty = sku?.SafetyStockQty ?? 0,
				ReorderPoint = sku?.ReorderPoint ?? 0,
				MaxStockQty = sku?.MaxStockQty ?? 0,
				Creator = supStockBatch.Creator,
				CreatedDate = supStockBatch.CreatedDate,
				Reviser = supStockBatch.Reviser,
				RevisedDate = supStockBatch.RevisedDate
			};
		}

		public async Task<string?> GetLastRemarkAsync(int stockBatchId)
		{
			return await _context.SupStockHistories
				.Where(h => h.StockBatchId == stockBatchId)
				.OrderByDescending(h => h.RevisedDate)
				.Select(h => h.Remark)
				.FirstOrDefaultAsync();
		}

		/// <summary>
		/// 單筆手動調整
		/// </summary>
		public async Task<SupStockMovementDto> SaveStockMovementAsync(SupStockMovementDto dto)
		{
			if (dto == null)
				throw new ArgumentException("資料無效");
			if (dto.ChangeQty <= 0)
				throw new ArgumentException("變動數量必須大於 0");

			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				dto.MovementType = "Adjust"; // 強制手動調整

				var batch = await _context.SupStockBatches
					.Include(sb => sb.Sku)
						.ThenInclude(s => s.Product)
							.ThenInclude(p => p.Brand)
					.FirstOrDefaultAsync(sb => sb.StockBatchId == dto.StockBatchId);

				if (batch == null)
					throw new InvalidOperationException($"找不到庫存批次: {dto.StockBatchId}");

				int beforeQty = batch.Qty;
				int newQty = beforeQty + (dto.IsAdd ? dto.ChangeQty : -dto.ChangeQty);

				if (newQty < 0)
					throw new InvalidOperationException("異動後庫存不能小於 0");
				if (batch.Sku.MaxStockQty > 0 && newQty > batch.Sku.MaxStockQty)
					throw new InvalidOperationException($"異動後庫存不能超過最大庫存量 {batch.Sku.MaxStockQty}");

				_context.SupStockHistories.Add(new Infra.Models.SupStockHistory
				{
					StockBatchId = batch.StockBatchId,
					ChangeType = dto.MovementType,
					ChangeQty = dto.ChangeQty,
					BeforeQty = beforeQty,
					AfterQty = newQty,
					Reviser = dto.UserId ?? 0,
					RevisedDate = DateTime.Now,
					Remark = dto.Remark
				});

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				return new SupStockMovementDto
				{
					StockBatchId = batch.StockBatchId,
					SkuCode = batch.Sku.SkuCode,
					ProductName = batch.Sku.Product.ProductName,
					BrandName = batch.Sku.Product.Brand.BrandName,
					BatchNumber = batch.BatchNumber,
					CurrentQty = beforeQty,
					PredictedQty = newQty,
					MovementType = dto.MovementType,
					ChangeQty = dto.IsAdd ? dto.ChangeQty : -dto.ChangeQty,
					Remark = dto.Remark
				};
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}

	}

}