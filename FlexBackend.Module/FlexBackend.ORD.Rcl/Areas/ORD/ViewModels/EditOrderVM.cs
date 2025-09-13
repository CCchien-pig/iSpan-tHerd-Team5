using System.ComponentModel.DataAnnotations;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels
{
	public class EditOrderVM
	{
		public int OrderId { get; set; }
		public string OrderNo { get; set; } = "";
		public int UserNumberId { get; set; }

		[Required] public string OrderStatusId { get; set; }
		[Required] public string PaymentStatus { get; set; } = "unpaid";
		[Required] public string ShippingStatusId { get; set; }

		[Range(0, double.MaxValue)] public decimal Subtotal { get; set; }
		[Range(0, double.MaxValue)] public decimal DiscountTotal { get; set; }
		[Range(0, double.MaxValue)] public decimal ShippingFee { get; set; }

		[Required] public int PaymentConfigId { get; set; }
		public int? LogisticsId { get; set; }

		[Required, StringLength(100)] public string ReceiverName { get; set; } = "";
		[Required, StringLength(30)] public string ReceiverPhone { get; set; } = "";
		[Required, StringLength(200)] public string ReceiverAddress { get; set; } = "";

		public bool IsVisibleToMember { get; set; }
	}
}
