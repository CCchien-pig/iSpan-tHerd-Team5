using tHerdBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface ILogisticsRateRepository
	{
		Task<bool> CheckLogisticsExistsAsync(int logisticsId);
		Task<List<LogisticsRateDto>> GetByLogisticsIdAsync(int logisticsId);
	}
}
