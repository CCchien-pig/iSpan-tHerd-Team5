using System.Data;

namespace tHerdBackend.Infra.Repository.SUP
{
	public interface IShippingFeeRepository
	{
		Task<ShippingFeeRepositoryResult> GetShippingInfoAsync(
			int skuId,
			decimal totalWeight,
			int logisticsId,
			CancellationToken ct = default
		);
	}


	public class ShippingFeeRepositoryResult
	{
		public decimal? Weight { get; set; }
		public bool LogisticsIsActive { get; set; }
		public decimal? ShippingFee { get; set; }
	}

	
}