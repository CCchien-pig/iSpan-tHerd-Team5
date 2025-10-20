using tHerdBackend.Infra.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.ORD.Rcl.Areas.ORD.ViewModels
{
	public class PrintOrderVM
	{
			public OrdOrder Order { get; set; }
			public string ShippingMethod { get; set; }
			public string? CouponName { get; set; }
			public List<PrintOrderItemVM> Items { get; set; }

	}
}
