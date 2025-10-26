using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;

namespace tHerdBackend.Services.ORD
{
    public class ECPayService : IECPayService
    {
        private readonly IEcpayNotificationRepository _notificationRepo;
        private readonly ILogger<ECPayService> _logger;
        private readonly IConfiguration _configuration;

        // 🔥 從設定檔讀取
        private string MerchantID => _configuration["ECPay:MerchantID"] ?? "3002607";
        private string HashKey => _configuration["ECPay:HashKey"] ?? "pwFHCqoQZGmho4w6";
        private string HashIV => _configuration["ECPay:HashIV"] ?? "EkRm7iFT261dpevs";
        private string ActionUrl => _configuration["ECPay:ActionUrl"] ?? "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5";
        private string ReturnURL => _configuration["ECPay:ReturnURL"] ?? "https://your-domain.com/api/ord/payment/notify";
        private string ClientBackURL => _configuration["ECPay:ClientBackURL"] ?? "http://localhost:5173/order/complete";

        public ECPayService(
            IEcpayNotificationRepository notificationRepo,
            ILogger<ECPayService> logger,
            IConfiguration configuration)
        {
            _notificationRepo = notificationRepo;
            _logger = logger;
            _configuration = configuration;
        }

        public string CreatePaymentForm(string orderNo, int totalAmount, string itemName)
        {
            _logger.LogInformation("============================================================");
            _logger.LogInformation("🔥 開始產生綠界付款表單");
            _logger.LogInformation($"訂單編號: {orderNo}");
            _logger.LogInformation($"訂單金額: {totalAmount}");

            var now = DateTime.Now;
            var tradeNo = now.ToString("yyyyMMddHHmmss");
            var tradeDate = now.ToString("yyyy/MM/dd HH:mm:ss");

            itemName = CleanItemName(itemName);

            _logger.LogInformation($"交易編號: {tradeNo}");
            _logger.LogInformation($"交易時間: {tradeDate}");
            _logger.LogInformation($"商品名稱: {itemName}");
            _logger.LogInformation($"商品名稱長度: {itemName.Length} 字元");

            // 🔥 使用屬性讀取設定
            var param = new Dictionary<string, string>
            {
                ["MerchantID"] = MerchantID,
                ["MerchantTradeNo"] = tradeNo,
                ["MerchantTradeDate"] = tradeDate,
                ["PaymentType"] = "aio",
                ["TotalAmount"] = totalAmount.ToString(),
                ["TradeDesc"] = "tHerd Order",
                ["ItemName"] = itemName,
                ["ReturnURL"] = ReturnURL,  // 🔥 從設定檔讀取
                ["ChoosePayment"] = "Credit",
                ["EncryptType"] = "1"
            };

            if (!string.IsNullOrEmpty(ClientBackURL))
            {
                param["ClientBackURL"] = ClientBackURL;
            }

            _logger.LogInformation($"🔗 ReturnURL: {ReturnURL}");
            _logger.LogInformation($"🔗 ClientBackURL: {ClientBackURL}");

            var mac = GetCheckMacValue(param);

            _logger.LogInformation($"CheckMacValue: {mac}");
            _logger.LogInformation("============================================================");

            var sb = new StringBuilder();
            sb.AppendLine($"<form id='ecpayForm' method='post' action='{ActionUrl}'>");

            foreach (var kv in param)
            {
                var escapedValue = System.Security.SecurityElement.Escape(kv.Value);
                sb.AppendLine($"  <input type='hidden' name='{kv.Key}' value='{escapedValue}' />");
            }

            sb.AppendLine($"  <input type='hidden' name='CheckMacValue' value='{mac}' />");
            sb.AppendLine("</form>");

            return sb.ToString();
        }

        private string CleanItemName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return "Order Items";

            itemName = itemName
                .Replace("&", " and ")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("\r", "")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Replace("|", " ")
                .Trim();

            while (itemName.Contains("  "))
            {
                itemName = itemName.Replace("  ", " ");
            }

            if (itemName.Length > 400)
            {
                itemName = itemName.Substring(0, 397) + "...";
                _logger.LogWarning($"商品名稱過長,已截斷至 400 字元");
            }

            return itemName;
        }

        private string GetCheckMacValue(Dictionary<string, string> param)
        {
            var sorted = param
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}");

            var raw = $"HashKey={HashKey}&{string.Join("&", sorted)}&HashIV={HashIV}";

            _logger.LogInformation($"Step 1 原始字串長度: {raw.Length}");
            _logger.LogInformation($"Step 1 原始字串: {raw.Substring(0, Math.Min(200, raw.Length))}...");

            var encoded = System.Net.WebUtility.UrlEncode(raw);

            _logger.LogInformation($"Step 2 URL編碼長度: {encoded.Length}");
            _logger.LogInformation($"Step 2 URL編碼: {encoded.Substring(0, Math.Min(200, encoded.Length))}...");

            encoded = encoded.ToLower();

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(encoded);
            var hash = sha.ComputeHash(bytes);
            var result = BitConverter.ToString(hash).Replace("-", "").ToUpper();

            _logger.LogInformation($"Step 3 SHA256: {result}");

            return result;
        }

        public bool ValidateCheckMacValue(Dictionary<string, string> parameters)
        {
            if (!parameters.ContainsKey("CheckMacValue"))
            {
                _logger.LogWarning("❌ 缺少 CheckMacValue");
                return false;
            }

            var received = parameters["CheckMacValue"];
            parameters.Remove("CheckMacValue");

            var calculated = GetCheckMacValue(parameters);

            bool isValid = received.Equals(calculated, StringComparison.OrdinalIgnoreCase);

            if (isValid)
            {
                _logger.LogInformation("✅ CheckMacValue 驗證成功");
            }
            else
            {
                _logger.LogWarning("❌ CheckMacValue 驗證失敗!");
                _logger.LogWarning($"接收: {received}");
                _logger.LogWarning($"計算: {calculated}");
            }

            return isValid;
        }

        public async Task<bool> ProcessPaymentNotificationAsync(EcpayNotificationDto dto)
        {
            try
            {
                await _notificationRepo.CreateAsync(dto);
                _logger.LogInformation($"✅ 綠界通知: TradeNo={dto.TradeNo}, RtnCode={dto.RtnCode}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 處理通知失敗");
                return false;
            }
        }
    }
}