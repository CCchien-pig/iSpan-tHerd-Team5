using Dapper;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.SUP
{
	public class ShippingFeeRepository : IShippingFeeRepository
	{
		private readonly ISqlConnectionFactory _factory;
		private readonly tHerdDBContext _db;

		public ShippingFeeRepository(ISqlConnectionFactory factory, tHerdDBContext db)
		{
			_factory = factory;
			_db = db;
		}

		public async Task<ShippingFeeRepositoryResult> GetShippingInfoAsync(
			int skuId,
			decimal totalWeight,
			int logisticsId,
			CancellationToken ct = default)
		{
			var sql = @"
                SELECT 
                    p.Weight,
                    l.IsActive AS LogisticsIsActive,
                    r.ShippingFee
                FROM PROD_ProductSku s
                JOIN PROD_Product p ON s.ProductId = p.ProductId
                JOIN SUP_Logistics l ON l.LogisticsId = @LogisticsId
                LEFT JOIN SUP_LogisticsRate r 
                    ON r.LogisticsId = l.LogisticsId
                    AND r.IsActive = 1
                    AND @TotalWeight >= r.WeightMin
                    AND (@TotalWeight < r.WeightMax OR r.WeightMax IS NULL)
                WHERE s.SkuId = @SkuId";

			// 取得連線：依照有無 dbContext 以及用意來自動選擇連線
			var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

			try
			{
				var result = await conn.QueryFirstOrDefaultAsync<ShippingFeeRepositoryResult>(
					sql,
					new
					{
						SkuId = skuId,
						LogisticsId = logisticsId,
						TotalWeight = totalWeight
					},
					transaction: tx
				);
				return result;
			}
			finally
			{
				if (needDispose) conn.Dispose();
			}
		}
	}
}
