using FlexBackend.Core.DTOs.SUP;

namespace FlexBackend.Core.Interfaces.SUP
{
	public interface IStockService
	{
		Task<StockAdjustResultDto> AdjustStockAsync(
			int skuId,
			int changeQty,
			int userId,
			string changeType, // "Adjust", "Sale", "Return"...
			List<int> batchIds = null,
			string remark = "");

		Task<StockAdjustResultDto> ReturnStockAsync(
			int skuId,
			int changeQty,
			List<int> batchIds,
			int reviserId = 0,
			string remark = null);


		Task<List<SupStockBatchDto>> GetBatchesBySkuAsync(int skuId);
	}
}

// [SYS_Code]
// ModuleId CodeId  CodeNo CodeDesc
// ('SUP', '00', '01', N'庫存異動類型'),
// ('SUP', '01', 'Purchase', N'採購入庫'),
// ('SUP', '01', 'Sale', N'銷售出庫'),
// ('SUP', '01', 'Return', N'退貨入庫'),
// ('SUP', '01', 'Expire', N'到期報廢'),
// ('SUP', '01', 'Adjust', N'手動調整'),