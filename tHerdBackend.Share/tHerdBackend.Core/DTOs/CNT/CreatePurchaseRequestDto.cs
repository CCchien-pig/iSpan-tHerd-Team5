using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.CNT
{
	/// <summary>建立購買紀錄 Request</summary>
	public class CreatePurchaseRequestDto
	{
		/// <summary>付款方式：先固定 LINEPAY / ECPAY</summary>
		public string PaymentMethod { get; set; } = "LINEPAY";
	}
}
