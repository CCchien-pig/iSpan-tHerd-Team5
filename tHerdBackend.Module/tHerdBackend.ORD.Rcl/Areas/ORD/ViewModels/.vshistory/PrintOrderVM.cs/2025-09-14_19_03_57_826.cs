using FlexBackend.Infra.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels
{
	public class PrintOrderVM
	{
			public OrdOrder Order { get; set; }
			public string ShippingMethod { get; set; }
			public List<PrintOrderItemVM> Items { get; set; }

	}
}
