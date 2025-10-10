using FlexBackend.Core.DTOs.SUP;

namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface ILogisticsRateRepository
	{
		Task<List<LogisticsRateDto>> GetByLogisticsIdAsync(int logisticsId);
	}
}
