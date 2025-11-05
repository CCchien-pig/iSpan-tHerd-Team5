using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.MKT
{
    public class PromotionCalculateResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public PromotionData Data { get; set; }

        public class PromotionData
        {
            public List<DiscountDetail> Discounts { get; set; } = new();
            public decimal TotalDiscount => Discounts.Sum(d => d.DiscountAmount);
        }

        public class DiscountDetail
        {
            public int CouponId { get; set; }
            public string CouponCode { get; set; }
            public string CouponType { get; set; }
            public string CouponName { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal DefaultCondition { get; set; }
            public string Description { get; set; }
        }
    }

}
