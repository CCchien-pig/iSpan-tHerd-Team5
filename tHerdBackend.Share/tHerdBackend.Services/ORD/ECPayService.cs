using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Options;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;

namespace tHerdBackend.Services.ORD
{
    public class ECPayService : IECPayService
    {
        private readonly ECPayConfig _config;
        private readonly IEcpayNotificationRepository _notificationRepo;
        private readonly IPaymentRepository _paymentRepo;

        public ECPayService(
            IOptions<ECPayConfig> config,
            IEcpayNotificationRepository notificationRepo,
            IPaymentRepository paymentRepo)
        {
            _config = config.Value;
            _notificationRepo = notificationRepo;
            _paymentRepo = paymentRepo;
        }

        public string CreatePaymentForm(string orderId, int totalAmount, string itemName)
        {
            var merchantTradeNo = $"{orderId}_{DateTime.Now:yyyyMMddHHmmss}";
            var tradeDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            var parameters = new SortedDictionary<string, string>
            {
                { "MerchantID", _config.MerchantID },
                { "MerchantTradeNo", merchantTradeNo },
                { "MerchantTradeDate", tradeDate },
                { "PaymentType", "aio" },
                { "TotalAmount", totalAmount.ToString() },
                { "TradeDesc", "tHerd商城購物" },
                { "ItemName", itemName },
                { "ReturnURL", _config.OrderResultUrl },
                { "ChoosePayment", "Credit" },
                { "EncryptType", "1" },
                { "ClientBackURL", _config.ReturnUrl }
            };

            var checkMacValue = GenerateCheckMacValue(parameters);
            parameters.Add("CheckMacValue", checkMacValue);

            var formHtml = new StringBuilder();
            formHtml.AppendLine($"<form id='ecpayForm' method='post' action='{_config.PaymentUrl}'>");

            foreach (var param in parameters)
            {
                formHtml.AppendLine($"<input type='hidden' name='{param.Key}' value='{HttpUtility.HtmlEncode(param.Value)}' />");
            }

            formHtml.AppendLine("</form>");
            formHtml.AppendLine("<script>document.getElementById('ecpayForm').submit();</script>");

            return formHtml.ToString();
        }

        private string GenerateCheckMacValue(SortedDictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            sb.Append($"HashKey={_config.HashKey}");

            foreach (var param in parameters)
            {
                sb.Append($"&{param.Key}={param.Value}");
            }

            sb.Append($"&HashIV={_config.HashIV}");

            var encodedString = HttpUtility.UrlEncode(sb.ToString()).ToLower();

            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(encodedString));
                return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
            }
        }

        public bool ValidateCheckMacValue(Dictionary<string, string> parameters)
        {
            if (!parameters.ContainsKey("CheckMacValue"))
                return false;

            var receivedCheckMac = parameters["CheckMacValue"];
            parameters.Remove("CheckMacValue");

            var sortedParams = new SortedDictionary<string, string>(parameters);
            var calculatedCheckMac = GenerateCheckMacValue(sortedParams);

            return receivedCheckMac == calculatedCheckMac;
        }

        public async Task<bool> ProcessPaymentNotificationAsync(EcpayNotificationDto dto)
        {
            try
            {
                // 1. 儲存通知記錄
                await _notificationRepo.CreateAsync(dto);

                // 2. 更新 Payment 狀態
                if (dto.RtnCode == 1) // 成功
                {
                    await _paymentRepo.UpdatePaymentByTradeNoAsync(
                        dto.TradeNo,
                        "success",
                        string.IsNullOrEmpty(dto.PaymentDate) ? null : DateTime.ParseExact(dto.PaymentDate, "yyyy/MM/dd HH:mm:ss", null),
                        dto.RtnCode,
                        dto.RtnMsg
                    );
                }
                else // 失敗
                {
                    await _paymentRepo.UpdatePaymentByTradeNoAsync(
                        dto.TradeNo,
                        "failed",
                        null,
                        dto.RtnCode,
                        dto.RtnMsg
                    );
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}