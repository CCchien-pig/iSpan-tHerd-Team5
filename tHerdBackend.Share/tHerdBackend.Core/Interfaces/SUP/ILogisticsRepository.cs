using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface ILogisticsRepository
	{
		Task<List<LogisticsDto>> GetAllAsync();
		Task<LogisticsDto?> GetByIdAsync(int id);
	}
}
