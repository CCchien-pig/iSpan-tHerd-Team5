using FlexBackend.Core.DTOs.SUP;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;

public class LogisticsRateRepository : ILogisticsRateRepository
{
	private readonly tHerdDBContext _context;
	public LogisticsRateRepository(tHerdDBContext context)
	{
		_context = context;
	}

	public async Task<List<LogisticsRateDto>> GetByLogisticsIdAsync(int logisticsId)
	{
		return await _context.SupLogisticsRates
			.AsNoTracking()
			.Where(r => r.LogisticsId == logisticsId)
			.OrderBy(r => r.WeightMin)
			.Select(r => new LogisticsRateDto
			{
				LogisticsRateId = r.LogisticsRateId,
				LogisticsId = r.LogisticsId,
				WeightMin = r.WeightMin,
				WeightMax = r.WeightMax,
				ShippingFee = r.ShippingFee,
				IsActive = r.IsActive,
				Reviser = r.Reviser,
				RevisedDate = r.RevisedDate
			})
			.ToListAsync();
	}
}
