using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.ORD;

namespace tHerdBackend.Core.Interfaces.ORD
{
    public interface IECPayService
    {
        /// <summary>
        /// 建立綠界付款表單 HTML
        /// </summary>
        string CreatePaymentForm(string orderId, int totalAmount, string itemName);

        /// <summary>
        /// 驗證 CheckMacValue
        /// </summary>
        bool ValidateCheckMacValue(Dictionary<string, string> parameters);

        /// <summary>
        /// 處理付款結果通知
        /// </summary>
        Task<bool> ProcessPaymentNotificationAsync(EcpayNotificationDto dto);
    }
}