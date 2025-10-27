using tHerdBackend.Core.DTOs.SUP.Logistics;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface ILogisticsRepository
	{
		Task<List<LogisticsDto>> GetAllAsync();
		Task<LogisticsDto?> GetByIdAsync(int id);
		Task<List<LogisticsDto>> GetActiveAsync();

	}
}
