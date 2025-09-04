using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.Core.Entities
{
    public class Product
    {
        public int ProductId { get; set; }   // PK
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}
