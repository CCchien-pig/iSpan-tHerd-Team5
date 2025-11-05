using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.ORD
{
    /// <summary>
    /// 收件地址
    /// </summary>
    public class ShippingAddressDto
    {
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string ReceiverAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// 地理編碼請求
    /// </summary>
    public class AddressGeocodeRequest
    {
        public string Address { get; set; } = string.Empty;
    }
}