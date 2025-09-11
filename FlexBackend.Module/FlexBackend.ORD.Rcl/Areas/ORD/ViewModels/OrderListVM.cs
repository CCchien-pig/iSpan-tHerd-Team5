using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;

public class OrderListVM
{
	public int OrderId { get; set; }
	public string OrderNo { get; set; } = "";
	public int UserNumberId { get; set; }

	// 付款狀態（ORD/04：pending/paid/...）
	public string PaymentStatus { get; set; } = "";

	// 配送狀態（ORD/05 → SYS_Code.Id）
	public int ShippingStatusId { get; set; }

	public decimal Subtotal { get; set; }
	public decimal DiscountTotal { get; set; }
	public decimal ShippingFee { get; set; }
	public DateTime CreatedDate { get; set; }
	public bool IsVisibleToMember { get; set; }

	public decimal Total => Subtotal - DiscountTotal + ShippingFee;
}
