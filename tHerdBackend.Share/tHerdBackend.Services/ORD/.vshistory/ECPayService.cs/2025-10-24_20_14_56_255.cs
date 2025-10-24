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

        // 綠界測試環境參數
        private const string MerchantID = "3002607";
        private const string HashKey = "pwFHCqoQZGmho4w6";
        private const string HashIV = "EkRm7iFT261dpevs";
        private const string ActionUrl = "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5";

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
        public string CreatePaymentForm(string orderNo, int totalAmount, string itemName)
        {
            // 產生唯一的交易編號 (14 字元)
            string merchantTradeNo = DateTime.Now.ToString("yyyyMMddHHmmss");

            // 準備參數 (注意: 不要包含 CheckMacValue)
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "MerchantID", MerchantID },
                { "MerchantTradeNo", merchantTradeNo },
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", totalAmount.ToString() },
                { "TradeDesc", "tHerd Online Order" },
                { "ItemName", SanitizeItemName(itemName) },
                { "ReturnURL", "https://your-domain.com/api/ord/payment/notify" },
                { "ChoosePayment", "Credit" },
                { "EncryptType", "1" }
            };

            // 🔥 關鍵: 計算 CheckMacValue
            string checkMacValue = GenerateCheckMacValue(parameters);

            _logger.LogInformation("=== 綠界付款表單參數 ===");
            foreach (var p in parameters)
            {
                _logger.LogInformation($"{p.Key} = {p.Value}");
            }
            _logger.LogInformation($"CheckMacValue = {checkMacValue}");
            _logger.LogInformation("========================");

            // 產生 HTML 表單
            var form = new StringBuilder();
            form.AppendLine($"<form id='ecpayForm' method='post' action='{ActionUrl}'>");

            // 加入所有參數
            foreach (var param in parameters)
            {
                form.AppendLine($"  <input type='hidden' name='{param.Key}' value='{param.Value}' />");
            }

            // 🔥 最後加入 CheckMacValue
            form.AppendLine($"  <input type='hidden' name='CheckMacValue' value='{checkMacValue}' />");

            form.AppendLine("</form>");

            return form.ToString();
        }

        /// <summary>
        /// 計算 CheckMacValue (綠界專用演算法)
        /// </summary>
        private string GenerateCheckMacValue(SortedDictionary<string, string> parameters)
        {
            // Step 1: 參數依照 Key 排序 (已使用 SortedDictionary)
            var paramList = parameters.Select(p => $"{p.Key}={p.Value}");

            // Step 2: 組合字串
            string rawString = $"HashKey={HashKey}&{string.Join("&", paramList)}&HashIV={HashIV}";

            _logger.LogDebug($"原始字串: {rawString}");

            // Step 3: URL Encode (綠界規則)
            string encodedString = CustomUrlEncode(rawString);

            _logger.LogDebug($"編碼字串: {encodedString}");

            // Step 4: 轉小寫
            encodedString = encodedString.ToLower();

            // Step 5: SHA256 雜湊
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encodedString));
                string checkMacValue = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();

                _logger.LogDebug($"CheckMacValue: {checkMacValue}");
                return checkMacValue;
            }
        }

        /// <summary>
        /// 綠界專用的 URL Encode
        /// 規則: 
        /// 1. 英數字、- _ . ~ 不編碼
        /// 2. 空格轉為 +
        /// 3. 其他字元轉為 %HH (UTF-8 編碼)
        /// </summary>
        private string CustomUrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var encoded = new StringBuilder();

            foreach (char c in value)
            {
                if (IsUnreservedChar(c))
                {
                    // 不需編碼的字元
                    encoded.Append(c);
                }
                else if (c == ' ')
                {
                    // 空格 → +
                    encoded.Append('+');
                }
                else
                {
                    // 其他字元 → %HH
                    byte[] bytes = Encoding.UTF8.GetBytes(c.ToString());
                    foreach (byte b in bytes)
                    {
                        encoded.AppendFormat("%{0:X2}", b);
                    }
                }
            }

            return encoded.ToString();
        }

        /// <summary>
        /// 判斷是否為不需編碼的字元
        /// </summary>
        private bool IsUnreservedChar(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z') ||
                   c == '-' || c == '_' || c == '.' || c == '~';
        }

        /// <summary>
        /// 清理商品名稱
        /// </summary>
        private string SanitizeItemName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return "Order Items";

            // 移除特殊字元
            itemName = itemName.Replace("'", "")
                               .Replace("\"", "")
                               .Replace("<", "")
                               .Replace(">", "")
                               .Replace("\r", "")
                               .Replace("\n", "");

            // 限制長度 (綠界限制 200 字元)
            if (itemName.Length > 200)
                itemName = itemName.Substring(0, 200);

            return itemName;
        }

        /// <summary>
        /// 驗證 CheckMacValue (接收綠界回傳時使用)
        /// </summary>
        public bool ValidateCheckMacValue(Dictionary<string, string> parameters)
        {
            if (!parameters.ContainsKey("CheckMacValue"))
            {
                _logger.LogWarning("❌ 缺少 CheckMacValue");
                return false;
            }

            var receivedCheckMac = parameters["CheckMacValue"];
            parameters.Remove("CheckMacValue");

            // 轉為 SortedDictionary
            var sortedParams = new SortedDictionary<string, string>(parameters, StringComparer.Ordinal);

            // 計算本地 CheckMacValue
            var localCheckMac = GenerateCheckMacValue(sortedParams);

            bool isValid = localCheckMac.Equals(receivedCheckMac, StringComparison.OrdinalIgnoreCase);

            if (isValid)
            {
                _logger.LogInformation("✅ CheckMacValue 驗證成功");
            }
            else
            {
                _logger.LogWarning($"❌ CheckMacValue 驗證失敗! 接收={receivedCheckMac}, 計算={localCheckMac}");
            }

            return isValid;
        }

        /// <summary>
        /// 處理付款結果通知
        /// </summary>
        public async Task<bool> ProcessPaymentNotificationAsync(EcpayNotificationDto dto)
        {
            try
            {
                await _notificationRepo.CreateAsync(dto);
                _logger.LogInformation($"✅ 收到綠界通知：TradeNo={dto.TradeNo}, RtnCode={dto.RtnCode}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ 處理綠界通知失敗：TradeNo={dto.TradeNo}");
                return false;
            }
        }
    }
}