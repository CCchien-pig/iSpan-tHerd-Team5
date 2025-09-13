using FlexBackend.Core.DTOs.SUP;

namespace FlexBackend.Core.Interfaces.SUP
{
	public interface IStockBatchService
	{
		Task<SupStockBatchDto?> GetStockBatchForEditAsync(int id);
	}

}
