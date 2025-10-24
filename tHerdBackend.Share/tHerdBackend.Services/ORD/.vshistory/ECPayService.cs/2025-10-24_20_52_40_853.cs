using CloudinaryDotNet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;

namespace tHerdBackend.Services.ORD
{
    public class ECPayService : IECPayService
    {
        private readonly IEcpayNotificationRepository _notificationRepo;
        private readonly ILogger<ECPayService> _logger;

        private const string MerchantID = "3002607";
        private const string HashKey = "pwFHCqoQZGmho4w6";
        private const string HashIV = "EkRm7iFT261dpevs";

        public ECPayService(
            IEcpayNotificationRepository notificationRepo,
            ILogger<ECPayService> logger)
        {
            _notificationRepo = notificationRepo;
            _logger = logger;
        }

        public string CreatePaymentForm(string orderNo, int totalAmount, string itemName)
        {
            Console.WriteLine("============================================================");
            Console.WriteLine($"🔥 開始產生綠界付款表單");
            Console.WriteLine($"訂單編號: {orderNo}");
            Console.WriteLine($"訂單金額: {totalAmount}");

            var now = DateTime.Now;
            var tradeNo = now.ToString("yyyyMMddHHmmss");
            var tradeDate = now.ToString("yyyy/MM/dd HH:mm:ss");

            // 清理商品名稱
            if (string.IsNullOrWhiteSpace(itemName))
                itemName = "Order Items";

            itemName = itemName
                .Replace("&", "and")
                .Replace("#", "No")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("\r", "")
                .Replace("\n", "");

            if (itemName.Length > 200)
                itemName = itemName.Substring(0, 200);

            Console.WriteLine($"交易編號: {tradeNo}");
            Console.WriteLine($"交易時間: {tradeDate}");
            Console.WriteLine($"商品名稱: {itemName}");

            // 準備參數
            var param = new Dictionary<string, string>
            {
                ["MerchantID"] = MerchantID,
                ["MerchantTradeNo"] = tradeNo,
                ["MerchantTradeDate"] = tradeDate,
                ["PaymentType"] = "aio",
                ["TotalAmount"] = totalAmount.ToString(),
                ["TradeDesc"] = "tHerd Order",
                ["ItemName"] = itemName,
                ["ReturnURL"] = "https://your-domain.com/api/payment/notify",
                ["ChoosePayment"] = "Credit",
                ["EncryptType"] = "1"
            };

            // 計算 CheckMacValue
            var mac = GetCheckMacValue(param);

            Console.WriteLine($"CheckMacValue: {mac}");
            Console.WriteLine("============================================================");

            // 產生 HTML 表單
            var sb = new StringBuilder();
            sb.AppendLine("<form id='ecpayForm' method='post' action='https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5'>");

            foreach (var kv in param)
            {
                sb.AppendLine($"  <input type='hidden' name='{kv.Key}' value='{kv.Value}' />");
            }

            sb.AppendLine($"  <input type='hidden' name='CheckMacValue' value='{mac}' />");
            sb.AppendLine("</form>");

            return sb.ToString();
        }

        private string GetCheckMacValue(Dictionary<string, string> param)
        {
            // Step 1: 參數排序
            var sorted = param.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}");

            // Step 2: 組合字串
            var raw = $"HashKey={HashKey}&{string.Join("&", sorted)}&HashIV={HashIV}";

            Console.WriteLine($"Step 1 原始字串: {raw}");

            // Step 3: URL Encode (自訂實作)
            var encoded = UrlEncode(raw);

            Console.WriteLine($"Step 2 URL編碼: {encoded}");

            // Step 4: 轉小寫
            encoded = encoded.ToLower();

            Console.WriteLine($"Step 3 轉小寫: {encoded}");

            // Step 5: SHA256 雜湊
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(encoded);
            var hash = sha.ComputeHash(bytes);
            var result = BitConverter.ToString(hash).Replace("-", "").ToUpper();

            Console.WriteLine($"Step 4 SHA256: {result}");

            return result;
        }

        /// <summary>
        /// URL Encode (綠界規則)
        /// </summary>
        private string UrlEncode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var result = new StringBuilder();

            foreach (char c in input)
            {
                // 不需編碼的字元: A-Z a-z 0-9 - _ . ~
                if ((c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= 'a' && c <= 'z') ||
                    c == '-' || c == '_' || c == '.' || c == '~')
                {
                    result.Append(c);
                }
                else if (c == ' ')
                {
                    // 空格轉為 +
                    result.Append('+');
                }
                else
                {
                    // 其他字元轉為 %HH
                    byte[] bytes = Encoding.UTF8.GetBytes(c.ToString());
                    foreach (byte b in bytes)
                    {
                        result.AppendFormat("%{0:X2}", b);
                    }
                }
            }

            return result.ToString();
        }

        public bool ValidateCheckMacValue(Dictionary<string, string> parameters)
        {
            if (!parameters.ContainsKey("CheckMacValue"))
            {
                Console.WriteLine("❌ 缺少 CheckMacValue");
                return false;
            }

            var received = parameters["CheckMacValue"];
            parameters.Remove("CheckMacValue");

            var calculated = GetCheckMacValue(parameters);

            bool isValid = received.Equals(calculated, StringComparison.OrdinalIgnoreCase);

            if (isValid)
            {
                Console.WriteLine("✅ CheckMacValue 驗證成功");
            }
            else
            {
                Console.WriteLine($"❌ CheckMacValue 驗證失敗!");
                Console.WriteLine($"接收: {received}");
                Console.WriteLine($"計算: {calculated}");
            }

            return isValid;
        }

        public async Task<bool> ProcessPaymentNotificationAsync(EcpayNotificationDto dto)
        {
            try
            {
                await _notificationRepo.CreateAsync(dto);
                Console.WriteLine($"✅ 綠界通知: TradeNo={dto.TradeNo}, RtnCode={dto.RtnCode}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 處理通知失敗: {ex.Message}");
                _logger.LogError(ex, "處理綠界通知失敗");
                return false;
            }
        }
    }
}