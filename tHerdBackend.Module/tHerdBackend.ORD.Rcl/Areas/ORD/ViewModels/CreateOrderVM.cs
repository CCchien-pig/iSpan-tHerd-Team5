using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.ORD.Rcl.Areas.ORD.ViewModels
{
    public class CreateOrderVM
    {
        [Required] public int UserNumberId { get; set; }
        [Required] public string OrderStatusId { get; set; }
        [Required] public string PaymentStatus { get; set; } = "unpaid"; // unpaid/paid/refunded...
        [Required] public string ShippingStatusId { get; set; }

        [Range(0, double.MaxValue)] public decimal Subtotal { get; set; }

        public int? CouponId { get; set; }
        [Range(0, double.MaxValue)] public decimal DiscountTotal { get; set; } = 0;
        [Range(0, double.MaxValue)] public decimal ShippingFee { get; set; } = 0;

        [Required] public int PaymentConfigId { get; set; }
        public int? LogisticsId { get; set; }

        [Required, StringLength(100)] public string ReceiverName { get; set; } = "";
        [Required, StringLength(30)] public string ReceiverPhone { get; set; } = "";
        [Required, StringLength(200)] public string ReceiverAddress { get; set; } = "";

        public bool HasShippingLabel { get; set; } = false;    // DB 預設 0
        public bool IsVisibleToMember { get; set; } = true;    // DB 預設 1
    }
}
