using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.PROD.ord
{
    public class AddToCartDto
    {
        public int SkuId { get; set; }
        public int ProductId { get; set; } // 可選，但保留關聯
        public int Qty { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }
}
