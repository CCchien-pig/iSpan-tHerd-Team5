namespace tHerdBackend.Core.DTOs.SUP
{
	public class ShippingFeeDto
	{

		public class ShippingFeeRequestDto
		{
			public int SkuId { get; set; }
			public int Quantity { get; set; }
			public int LogisticsId { get; set; }
		}

		public class ShippingFeeResponseDto
		{
			public bool Success { get; set; }
			public string Message { get; set; }
			public object Data { get; set; }
		}

	}
}
