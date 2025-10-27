using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.DTOs.SUP.Stock;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IStockBatchService
	{
		Task<SupStockBatchDto?> GetStockBatchForEditAsync(int id);
		Task<string?> GetLastRemarkAsync(int stockBatchId);
		Task<SupStockMovementDto> SaveStockMovementAsync(SupStockMovementDto dto);
	}

}
