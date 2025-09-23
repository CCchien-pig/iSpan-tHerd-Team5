using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.ORD.Rcl.Areas.ORD.ViewModels
{
    public class CartItemVM
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }

        public string SpecCode { get; set; } = "";
    }

}

