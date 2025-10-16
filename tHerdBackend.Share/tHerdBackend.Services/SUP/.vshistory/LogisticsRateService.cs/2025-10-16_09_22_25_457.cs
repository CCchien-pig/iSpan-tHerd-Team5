using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;

public class LogisticsRateService : ILogisticsRateService
{
	private readonly ILogisticsRateRepository _repo;

	public LogisticsRateService(ILogisticsRateRepository repo)
	{
		_repo = repo;
	}

	public async Task<List<LogisticsRateDto>> GetByLogisticsIdAsync(int logisticsId)
		=> await _repo.GetByLogisticsIdAsync(logisticsId);
}
