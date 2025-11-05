using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.ORD;
using System.Collections.Generic;

namespace tHerdBackend.Core.DTOs.ORD
{
    /// <summary>
    /// 訂單項目
    /// </summary>
    public class OrderItemDto
    {
        public int SkuId { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 計算訂單請求
    /// </summary>
    public class CalculateOrderRequest
    {
        public int? UserNumberId { get; set; }
        public List<OrderItemDto> CartItems { get; set; } = new();
        public string? CouponCode { get; set; }
    }

    /// <summary>
    /// 計算結果
    /// </summary>
    public class CalculationResult
    {
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string? AppliedCouponCode { get; set; }
        public decimal FreeShippingThreshold { get; set; }
        public decimal NeedMoreForFreeShipping { get; set; }
    }
}