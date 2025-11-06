using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.ORD
{
    /// <summary>
    /// 結帳請求
    /// </summary>
    public class CheckoutRequest
    {
        public string? SessionId { get; set; }
        public int? UserNumberId { get; set; }
        public List<CheckoutItemRequest> CartItems { get; set; } = new();
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ReceiverAddress { get; set; }
        public string? CouponCode { get; set; }
    }

    /// <summary>
    /// 結帳項目
    /// </summary>
    public class CheckoutItemRequest
    {
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public string? ProductName { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 加入購物車
    /// </summary>
    public class AddToCartDto
    {
        public string? SessionId { get; set; }
        public int? UserNumberId { get; set; }
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
    }

    /// <summary>
    /// 更新數量
    /// </summary>
    public class UpdateQtyDto
    {
        public int Qty { get; set; }
    }
}