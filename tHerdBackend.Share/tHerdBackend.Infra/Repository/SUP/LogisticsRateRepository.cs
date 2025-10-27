using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Infra.Models;
using tHerdBackend.Core.DTOs.SUP.Logistics;

public class LogisticsRateRepository : ILogisticsRateRepository
{
	private readonly tHerdDBContext _context;
	public LogisticsRateRepository(tHerdDBContext context)
	{
		_context = context;
	}

	// 檢查物流商是否存在
	public async Task<bool> CheckLogisticsExistsAsync(int logisticsId)
	{
		return await _context.SupLogistics
			.AsNoTracking()
			.AnyAsync(l => l.LogisticsId == logisticsId);
	}

	// 取得該物流商所有運費率
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
