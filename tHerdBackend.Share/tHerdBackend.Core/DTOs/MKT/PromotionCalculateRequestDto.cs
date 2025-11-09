using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.MKT
{
    public class PromotionCalculateRequestDto
    {
        public int UserNumberId { get; set; }
        public decimal Subtotal { get; set; }
        public string? CouponCode { get; set; }  // 優惠券代碼

		public string? CouponId { get; set; }
	}

}
