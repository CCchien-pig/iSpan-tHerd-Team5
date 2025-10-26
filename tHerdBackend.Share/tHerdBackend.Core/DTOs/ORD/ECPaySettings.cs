using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.ORD
{
    public class ECPaySettings
    {
        public string MerchantID { get; set; } = string.Empty;
        public string HashKey { get; set; } = string.Empty;
        public string HashIV { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
        public string ReturnURL { get; set; } = string.Empty;
        public string ClientBackURL { get; set; } = string.Empty;
    }
}