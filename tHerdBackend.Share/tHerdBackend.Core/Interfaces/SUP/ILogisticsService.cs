using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface ILogisticsService
	{
		Task<List<LogisticsDto>> GetAllAsync();
		Task<LogisticsDto?> GetByIdAsync(int id);
	}
}
