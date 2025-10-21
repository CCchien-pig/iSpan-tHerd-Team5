using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.ORD
{
    /// <summary>
    /// 綠界金流設定
    /// </summary>
    public class ECPayConfig
    {
        public string MerchantID { get; set; } = string.Empty;
        public string HashKey { get; set; } = string.Empty;
        public string HashIV { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string OrderResultUrl { get; set; } = string.Empty;
        public bool IsProduction { get; set; }
    }

    /// <summary>
    /// 綠界付款結果通知 DTO
    /// </summary>
    public class EcpayNotificationDto
    {
        public string MerchantID { get; set; }
        public string PlatformID { get; set; }
        public string StoreID { get; set; }
        public string MerchantTradeNo { get; set; }
        public string TradeNo { get; set; }
        public int RtnCode { get; set; }
        public string RtnMsg { get; set; }
        public int TradeAmt { get; set; }
        public string PaymentType { get; set; }
        public decimal? PaymentTypeChargeFee { get; set; }
        public string TradeDate { get; set; }
        public string PaymentDate { get; set; }
        public int? SimulatePaid { get; set; }
        public string CustomField1 { get; set; }
        public string CustomField2 { get; set; }
        public string CustomField3 { get; set; }
        public string CustomField4 { get; set; }
        public string CheckMacValue { get; set; }
        public string RawBody { get; set; }
        public string RawHeaders { get; set; }
        public string FailReason { get; set; }
    }

}
