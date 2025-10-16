using tHerdBackend.Core.DTOs.SUP;

public interface ILogisticsRateService
{
	Task<List<LogisticsRateDto>> GetByLogisticsIdAsync(int logisticsId);
}
