using FlexBackend.Core.DTOs.SUP;

namespace FlexBackend.Core.Interfaces.SUP
{
	public interface IStockService
	{
		 /// <summary>
		 /// 出庫(Sale)/調整(Adjust)
		 /// 更新指定批號庫存及 SKU 總庫存
		 /// 建立庫存異動紀錄
		 /// </summary>
		 /// <param name="batchId">要操作的批號 ID</param>
		 /// <param name="skuId">對應 SKU ID</param>
		 /// <param name="changeQty">變動庫存量</param>
		 /// <param name="isAdd">是否增加庫存（手動調整時）</param>
		 /// <param name="reviserId">操作使用者 ID</param>
		 /// <param name="remark">備註</param>
		 /// <param name="isAllowBackorder">是否允許預購可為負數</param>
		 /// <returns>回傳調整結果 DTO</returns>
		Task<StockAdjustResultDto> AdjustStockAsync(
			int batchId,
			int skuId,
			int changeQty,
			bool isAdd,
			int? reviserId,
			string remark,
			bool isAllowBackorder);



		Task<StockAdjustResultDto> ReturnStockAsync(
			int skuId,
			int changeQty,
			List<int> batchIds,
			int reviserId = 0,
			string remark = null);


		Task<List<SupStockBatchDto>> GetBatchesBySkuAsync(
			int skuId, 
			bool forDecrease);
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