using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Repository.ORD;

namespace tHerdBackend.Services.ORD
{
    public class ECPayService : IECPayService
    {
        private readonly IEcpayNotificationRepository _notificationRepo;
        private readonly ILogger<ECPayService> _logger;

        // 模擬用參數 (實務上應從設定檔取)
        private const string MerchantID = "3002607";    // 綠界測試商店代號
        private const string HashKey = "pwFHCqoQZGmho4w6";
        private const string HashIV = "EkRm7iFT261dpevs";

        public ECPayService(
            IEcpayNotificationRepository notificationRepo,
            ILogger<ECPayService> logger)
        {
            _notificationRepo = notificationRepo;
            _logger = logger;
        }

        /// <summary>
        /// 建立綠界付款表單 HTML
        /// </summary>
        public string CreatePaymentForm(string orderId, int totalAmount, string itemName)
        {
            var merchantTradeNo = $"{orderId}_{DateTime.Now:yyyyMMddHHmmss}";
            var form = new StringBuilder();

            form.AppendLine("<form id='ecpayForm' method='post' action='https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5'>");
            form.AppendLine($"<input type='hidden' name='MerchantID' value='{MerchantID}' />");
            form.AppendLine($"<input type='hidden' name='MerchantTradeNo' value='{merchantTradeNo}' />");
            form.AppendLine($"<input type='hidden' name='MerchantTradeDate' value='{DateTime.Now:yyyy/MM/dd HH:mm:ss}' />");
            form.AppendLine($"<input type='hidden' name='PaymentType' value='aio' />");
            form.AppendLine($"<input type='hidden' name='TotalAmount' value='{totalAmount}' />");
            form.AppendLine($"<input type='hidden' name='TradeDesc' value='Order Payment' />");
            form.AppendLine($"<input type='hidden' name='ItemName' value='{itemName}' />");
            form.AppendLine("<input type='hidden' name='ReturnURL' value='https://yourdomain.com/api/ord/payment/notify' />");
            form.AppendLine("<input type='hidden' name='ChoosePayment' value='Credit' />");
            form.AppendLine("<script>document.getElementById('ecpayForm').submit();</script>");
            form.AppendLine("</form>");

            _logger.LogInformation($"產生綠界表單: OrderNo={orderId}, Total={totalAmount}");
            return form.ToString();
        }

        /// <summary>
        /// 驗證 CheckMacValue
        /// </summary>
        public bool ValidateCheckMacValue(Dictionary<string, string> parameters)
        {
            if (!parameters.ContainsKey("CheckMacValue"))
                return false;

            var checkMacValue = parameters["CheckMacValue"];
            parameters.Remove("CheckMacValue");

            var sorted = parameters.OrderBy(p => p.Key.ToLower())
                                   .Select(p => $"{p.Key}={p.Value}")
                                   .ToList();

            var raw = $"HashKey={HashKey}&{string.Join("&", sorted)}&HashIV={HashIV}";
            var encoded = Uri.EscapeDataString(raw).ToLower();

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(encoded));
            var localMac = BitConverter.ToString(hash).Replace("-", "").ToUpper();

            return localMac == checkMacValue.ToUpper();
        }

        /// <summary>
        /// 處理付款結果通知
        /// </summary>
        public async Task<bool> ProcessPaymentNotificationAsync(EcpayNotificationDto dto)
        {
            try
            {
                await _notificationRepo.CreateAsync(dto);
                _logger.LogInformation($"收到綠界通知：TradeNo={dto.TradeNo}, RtnCode={dto.RtnCode}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"處理綠界通知失敗：TradeNo={dto.TradeNo}");
                return false;
            }
        }
    }
}
