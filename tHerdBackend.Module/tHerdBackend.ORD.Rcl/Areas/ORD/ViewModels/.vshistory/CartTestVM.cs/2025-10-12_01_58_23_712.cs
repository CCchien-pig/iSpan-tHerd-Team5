using System;
using System.Collections.Generic;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels
{
    /// <summary>
    /// 購物車項目 ViewModel
    /// </summary>
    public class CartItemVM
    {
        /// <summary>
        /// 商品編號
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// SKU 編號
        /// </summary>
        public int SkuId { get; set; }

        /// <summary>
        /// 商品名稱
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// 規格名稱
        /// </summary>
        public string OptionName { get; set; }

        /// <summary>
        /// 原價
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 售價
        /// </summary>
        public decimal SalePrice { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 小計 (售價 * 數量)
        /// </summary>
        public decimal Subtotal => SalePrice * Quantity;
    }

    /// <summary>
    /// 結帳請求 ViewModel
    /// </summary>
    public class CheckoutRequestVM
    {
        public List<CartItemVM> CartItems { get; set; }
    }

    /// <summary>
    /// 結帳回應 ViewModel
    /// </summary>
    public class CheckoutResponseVM
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 訂單編號
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// 訂單總額
        /// </summary>
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// 訂單明細查詢 ViewModel
    /// </summary>
    public class OrderDetailVM
    {
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public List<OrderDetailItemVM> Details { get; set; }
    }

    /// <summary>
    /// 訂單明細項目 ViewModel
    /// </summary>
    public class OrderDetailItemVM
    {
        public string ProductName { get; set; }
        public string OptionName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    /// <summary>
    /// SKU 庫存查詢 ViewModel
    /// </summary>
    public class SkuStockVM
    {
        public int SkuId { get; set; }
        public string OptionName { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
    }

    public class CheckoutErrorVM
    {
        public string ProductName { get; set; } = "";
        public string OptionName { get; set; } = "";
        public string Reason { get; set; } = "";
        public int? CurrentStock { get; set; }
    }
}