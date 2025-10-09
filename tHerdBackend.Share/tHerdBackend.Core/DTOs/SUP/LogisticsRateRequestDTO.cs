namespace FlexBackend.SUP.Rcl.Areas.SUP.Controllers.ApiControllers
{
	public class LogisticsRateRequestDTO
	{
		public class CreateLogisticsRateRequest
		{
			public int LogisticsId { get; set; }
			public decimal WeightMin { get; set; }
			public decimal? WeightMax { get; set; }
			public decimal ShippingFee { get; set; }
			public bool IsActive { get; set; }
		}

		public class UpdateLogisticsRateRequest
		{
			public decimal WeightMin { get; set; }
			public decimal? WeightMax { get; set; }
			public decimal ShippingFee { get; set; }
			public bool IsActive { get; set; }
		}

	}
}