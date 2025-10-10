using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.SUP
{
	public class LogisticsRepository : ILogisticsRepository
	{
		private readonly tHerdDBContext _context;
		public LogisticsRepository(tHerdDBContext context)
		{
			_context = context;
		}

		public async Task<List<LogisticsDto>> GetAllAsync()
		{
			return await _context.SupLogistics
				.AsNoTracking()
				.Select(l => new LogisticsDto
				{
					LogisticsId = l.LogisticsId,
					ShippingMethod = l.ShippingMethod,
					LogisticsName = l.LogisticsName,
					IsActive = l.IsActive,
					Creator = l.Creator,
					CreatedDate = l.CreatedDate,
					Reviser = l.Reviser,
					RevisedDate = l.RevisedDate
				})
				.ToListAsync();
		}

		public async Task<LogisticsDto?> GetByIdAsync(int id)
		{
			return await _context.SupLogistics
				.AsNoTracking()
				.Where(l => l.LogisticsId == id)
				.Select(l => new LogisticsDto
				{
					LogisticsId = l.LogisticsId,
					ShippingMethod = l.ShippingMethod,
					LogisticsName = l.LogisticsName,
					IsActive = l.IsActive,
					Creator = l.Creator,
					CreatedDate = l.CreatedDate,
					Reviser = l.Reviser,
					RevisedDate = l.RevisedDate
				})
				.FirstOrDefaultAsync();
		}

	}
}
