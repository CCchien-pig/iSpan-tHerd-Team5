using Dapper;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Repository.SUP;

public class LogisticsRateService : ILogisticsRateService
{
	private readonly IShippingFeeRepository _repo;
	private readonly ISqlConnectionFactory _factory;
	private readonly tHerdDBContext _db;

	public LogisticsRateService(IShippingFeeRepository repo, ISqlConnectionFactory factory, tHerdDBContext db)
	{
		_repo = repo;
		_factory = factory;
		_db = db;
	}

	public async Task<ShippingFeeDto.ShippingFeeResponseDto> CalculateShippingFeeAsync(
		ShippingFeeDto.ShippingFeeRequestDto req,
		CancellationToken ct = default)
	{
		if (req.SkuId <= 0 || req.Qty <= 0 || req.LogisticsId <= 0)
			return new ShippingFeeDto.ShippingFeeResponseDto { Success = false, Message = "參數有誤", Data = null };

		// 先查 Product 重量
		var sql = @"SELECT p.Weight FROM PROD_ProductSku s JOIN PROD_Product p ON s.ProductId = p.ProductId WHERE s.SkuId = @SkuId";
		var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

		try
		{
			var productWeight = await conn.QueryFirstOrDefaultAsync<decimal?>(
				sql, new { SkuId = req.SkuId }, transaction: tx
			);

			if (productWeight == null)
				return new ShippingFeeDto.ShippingFeeResponseDto { Success = false, Message = "SKU不存在或商品資料不存在", Data = null };

			var totalWeight = productWeight.Value * req.Qty;

			// 查物流商與費率 Query 用 Repository（已支援交易）
			var result = await _repo.GetShippingInfoAsync(req.SkuId, totalWeight, req.LogisticsId, ct);

			if (result == null)
				return new ShippingFeeDto.ShippingFeeResponseDto { Success = false, Message = "資料不存在", Data = null };

			if (!result.LogisticsIsActive)
				return new ShippingFeeDto.ShippingFeeResponseDto { Success = false, Message = "物流商未啟用", Data = null };

			if (result.ShippingFee == null)
				return new ShippingFeeDto.ShippingFeeResponseDto { Success = false, Message = "找不到符合條件的物流費率", Data = null };

			return new ShippingFeeDto.ShippingFeeResponseDto
			{
				Success = true,
				Message = "運算成功",
				Data = new { shippingFee = result.ShippingFee }
			};
		}
		finally
		{
			if (needDispose) conn.Dispose();
		}
	}


	// 檢查物流商是否存在
	public async Task<bool> CheckLogisticsExistsAsync(int logisticsId)
	{
		return await _repo.LogisticsExistsAsync(logisticsId);
	}

	public async Task<List<LogisticsRateDto>> GetByLogisticsIdAsync(int logisticsId, CancellationToken ct = default)
	{
		var sql = @"
            SELECT LogisticsRateId, LogisticsId, WeightMin, WeightMax, ShippingFee, IsActive
            FROM SUP_LogisticsRate
            WHERE LogisticsId = @LogisticsId
        ";

		var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

		try
		{
			var rates = await conn.QueryAsync<LogisticsRateDto>(sql, new { LogisticsId = logisticsId }, transaction: tx);
			return rates.ToList();
		}
		finally
		{
			if (needDispose) conn.Dispose();
		}
	}
}
