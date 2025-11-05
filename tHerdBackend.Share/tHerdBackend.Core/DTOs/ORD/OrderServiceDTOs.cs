using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.ORD
{
    /// <summary>
    /// 建立訂單請求
    /// </summary>
    public class OrderCreateDto
    {
        public int UserNumberId { get; set; }
        public decimal Subtotal { get; set; }
        public List<OrderDetailDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 訂單明細（用於建立訂單）
    /// </summary>
    public class OrderDetailDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    /// <summary>
    /// 訂單操作結果
    /// </summary>
    public class OrderResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OrderNo { get; set; }
        public int OrderId { get; set; }
    }

    /// <summary>
    /// SUP API 回應
    /// </summary>
    public class SupApiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
