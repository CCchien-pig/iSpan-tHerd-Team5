using FlexBackend.Core.DTOs.SUP;
using FlexBackend.Core.Interfaces.SUP;
using FlexBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.Services.SUP
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
	}

}