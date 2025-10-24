using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
            _logger.LogInformation("============================================================");
            _logger.LogInformation("🔥 開始產生綠界付款表單");
            _logger.LogInformation($"訂單編號: {orderNo}");
            _logger.LogInformation($"訂單金額: {totalAmount}");

            var now = DateTime.Now;
            var tradeNo = now.ToString("yyyyMMddHHmmss");
            var tradeDate = now.ToString("yyyy/MM/dd HH:mm:ss");

            // 🔥 清理商品名稱 (保留中文,但移除危險字元)
            itemName = CleanItemName(itemName);

            _logger.LogInformation($"交易編號: {tradeNo}");
            _logger.LogInformation($"交易時間: {tradeDate}");
            _logger.LogInformation($"商品名稱: {itemName}");
            _logger.LogInformation($"商品名稱長度: {itemName.Length} 字元");

            // 準備參數 (字典會自動按 Key 排序)
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

            _logger.LogInformation($"CheckMacValue: {mac}");
            _logger.LogInformation("============================================================");

            // 產生 HTML 表單
            var sb = new StringBuilder();
            sb.AppendLine("<form id='ecpayForm' method='post' action='https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5'>");

            foreach (var kv in param)
            {
                // 🔥 HTML 屬性值必須 escape
                var escapedValue = System.Security.SecurityElement.Escape(kv.Value);
                sb.AppendLine($"  <input type='hidden' name='{kv.Key}' value='{escapedValue}' />");
            }

            sb.AppendLine($"  <input type='hidden' name='CheckMacValue' value='{mac}' />");
            sb.AppendLine("</form>");

            return sb.ToString();
        }

        /// <summary>
        /// 清理商品名稱 (保留中文)
        /// </summary>
        private string CleanItemName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return "Order Items";

            // 只移除會造成問題的字元
            itemName = itemName
                .Replace("&", " and ")     // & 會干擾 URL
                .Replace("'", "")           // 單引號
                .Replace("\"", "")          // 雙引號
                .Replace("<", "")           // HTML 標籤
                .Replace(">", "")           // HTML 標籤
                .Replace("\r", "")          // 換行
                .Replace("\n", " ")         // 換行改空格
                .Replace("\t", " ")         // Tab
                .Replace("|", " ")          // 管線符號
                .Trim();

            // 移除連續空格
            while (itemName.Contains("  "))
            {
                itemName = itemName.Replace("  ", " ");
            }

            // 🔥 綠界限制 400 字元 (官方文件)
            if (itemName.Length > 400)
            {
                itemName = itemName.Substring(0, 397) + "...";
                _logger.LogWarning($"商品名稱過長,已截斷至 400 字元");
            }

            return itemName;
        }

        /// <summary>
        /// 計算 CheckMacValue (綠界官方演算法)
        /// </summary>
        private string GetCheckMacValue(Dictionary<string, string> param)
        {
            // Step 1: 參數按照 Key 排序 (區分大小寫)
            var sorted = param
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}");

            // Step 2: 組合字串 (前後加 HashKey 和 HashIV)
            var raw = $"HashKey={HashKey}&{string.Join("&", sorted)}&HashIV={HashIV}";

            _logger.LogInformation($"Step 1 原始字串長度: {raw.Length}");
            _logger.LogInformation($"Step 1 原始字串: {raw.Substring(0, Math.Min(200, raw.Length))}...");

            // Step 3: URL Encode (使用 .NET Core 標準方法)
            var encoded = System.Net.WebUtility.UrlEncode(raw);

            _logger.LogInformation($"Step 2 URL編碼長度: {encoded.Length}");
            _logger.LogInformation($"Step 2 URL編碼: {encoded.Substring(0, Math.Min(200, encoded.Length))}...");

            // Step 4: 轉小寫
            encoded = encoded.ToLower();

            // Step 5: SHA256 雜湊
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