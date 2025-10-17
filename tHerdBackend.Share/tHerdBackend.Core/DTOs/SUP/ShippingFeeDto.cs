using System.Text.Json.Serialization;

namespace tHerdBackend.Core.DTOs.SUP
{
	public class ShippingFeeDto
	{

		public class ShippingFeeRequestDto
		{
			public int SkuId { get; set; }
			public int Qty { get; set; }
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
