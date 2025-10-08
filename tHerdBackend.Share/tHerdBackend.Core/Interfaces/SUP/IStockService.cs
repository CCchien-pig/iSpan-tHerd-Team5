using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IStockService
	{
		 /// <summary>
		 /// 出庫(Sale)/調整(Adjust)
		 /// 更新指定批號庫存及 SKU 總庫存
		 /// 建立庫存異動紀錄
		 /// </summary>
		Task<StockAdjustResultDto> AdjustStockAsync(
			int batchId,
			int skuId,
			int changeQty,        // 前端正整數
			bool isAdd,           // 是否增加 (手動調整專用)
			string movementType,  // 異動類型: "Purchase", "Adjust", "Order", etc.
			int reviserId,
			string remark,
			int? orderItemId = null);



		//Task<StockAdjustResultDto> ReturnStockAsync(
		//	int skuId,
		//	int changeQty,
		//	List<int> batchIds,
		//	int reviserId = 0,
		//	string remark = null);


		//Task<List<SupStockBatchDto>> GetBatchesBySkuAsync(
		//	int skuId, 
		//	bool forDecrease);
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