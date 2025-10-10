using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;

namespace tHerdBackend.Services.SUP
{
	public class LogisticsService : ILogisticsService
	{
		private readonly ILogisticsRepository _repository;
		public LogisticsService(ILogisticsRepository repository)
		{
			_repository = repository;
		}
		public async Task<List<LogisticsDto>> GetAllAsync()
			=> await _repository.GetAllAsync();

		public async Task<LogisticsDto?> GetByIdAsync(int id)
			=> await _repository.GetByIdAsync(id);
	}
}
