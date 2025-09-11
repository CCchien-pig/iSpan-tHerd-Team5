namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels
{
	public class OrderListVM
	{
		public int OrderId { get; set; }
		public string OrderNo { get; set; }
		public string UserNumberId { get; set; }
		public string PaymentStatus { get; set; }
		public int ShippingStatusId { get; set; }
		public decimal Total { get; set; }
		public DateTime CreatedDate { get; set; }
		public bool IsVisibleToMember { get; set; }
	}
}
@model IEnumerable<FlexBackend.ORD.Rcl.Areas.ORD.ViewModels.OrderListVM>
