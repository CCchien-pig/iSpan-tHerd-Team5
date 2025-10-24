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
            try
            {
                // 產生唯一的交易編號 (14 字元)
                string merchantTradeNo = DateTime.Now.ToString("yyyyMMddHHmmss");
                string merchantTradeDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                // 清理商品名稱
                itemName = SanitizeItemName(itemName);

                // 準備所有參數 (按字母順序排列很重要!)
                var parameters = new Dictionary<string, string>
                {
                    { "ChoosePayment", "Credit" },
                    { "EncryptType", "1" },
                    { "ItemName", itemName },
                    { "MerchantID", MerchantID },
                    { "MerchantTradeDate", merchantTradeDate },
                    { "MerchantTradeNo", merchantTradeNo },
                    { "PaymentType", "aio" },
                    { "ReturnURL", "https://your-domain.com/api/ord/payment/notify" },
                    { "TotalAmount", totalAmount.ToString() },
                    { "TradeDesc", "tHerd Order Payment" }
                };

                // 🔥 計算 CheckMacValue
                string checkMacValue = GenerateCheckMacValue(parameters);

                // 輸出除錯資訊
                _logger.LogInformation("========== 綠界付款表單 ==========");
                _logger.LogInformation($"訂單編號: {orderNo}");
                _logger.LogInformation($"交易編號: {merchantTradeNo}");
                _logger.LogInformation($"交易日期: {merchantTradeDate}");
                _logger.LogInformation($"交易金額: {totalAmount}");
                _logger.LogInformation($"商品名稱: {itemName}");
                _logger.LogInformation("--- 表單參數 ---");

                // 按字母順序輸出
                foreach (var p in parameters.OrderBy(x => x.Key))
                {
                    _logger.LogInformation($"{p.Key} = {p.Value}");
                }

                _logger.LogInformation($"CheckMacValue = {checkMacValue}");
                _logger.LogInformation("==================================");

                // 產生 HTML 表單
                var form = new StringBuilder();
                form.AppendLine($"<form id='ecpayForm' method='post' action='{ActionUrl}'>");

                // 加入所有參數 (順序不重要,綠界會自己排序)
                foreach (var param in parameters)
                {
                    form.AppendLine($"  <input type='hidden' name='{param.Key}' value='{param.Value}' />");
                }

                // 加入 CheckMacValue
                form.AppendLine($"  <input type='hidden' name='CheckMacValue' value='{checkMacValue}' />");

                form.AppendLine("</form>");

                return form.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "產生綠界表單失敗");
                throw;
            }
        }

        /// <summary>
        /// 計算 CheckMacValue (綠界官方演算法)
        /// </summary>
        private string GenerateCheckMacValue(Dictionary<string, string> parameters)
        {
            // Step 1: 將參數依照 Key 排序 (字母順序,區分大小寫)
            var sortedParams = parameters
                .OrderBy(p => p.Key)
                .Select(p => $"{p.Key}={p.Value}");

            // Step 2: 組合字串 (前後加上 HashKey 和 HashIV)
            string rawString = $"HashKey={HashKey}&{string.Join("&", sortedParams)}&HashIV={HashIV}";

            _logger.LogDebug($"Step 1 - 原始字串: {rawString}");

            // Step 3: URL Encode (使用 .NET 的 Uri.EscapeDataString)
            string encodedString = HttpUtility.UrlEncode(rawString, Encoding.UTF8);

            _logger.LogDebug($"Step 2 - URL編碼: {encodedString}");

            // Step 4: 轉小寫
            encodedString = encodedString.ToLower();

            _logger.LogDebug($"Step 3 - 轉小寫: {encodedString}");

            // Step 5: SHA256 雜湊
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encodedString));
                string checkMacValue = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();

                _logger.LogDebug($"Step 4 - SHA256: {checkMacValue}");

                return checkMacValue;
            }
        }

        /// <summary>
        /// URL Encode (使用 System.Web.HttpUtility)
        /// 注意: 需要安裝 System.Web NuGet 套件
        /// </summary>
        private static class HttpUtility
        {
            public static string UrlEncode(string value, Encoding encoding)
            {
                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                var bytes = encoding.GetBytes(value);
                var encoded = new StringBuilder();

                foreach (byte b in bytes)
                {
                    char c = (char)b;

                    // 不需要編碼的字元: A-Z a-z 0-9 - _ . ~
                    if ((c >= '0' && c <= '9') ||
                        (c >= 'A' && c <= 'Z') ||
                        (c >= 'a' && c <= 'z') ||
                        c == '-' || c == '_' || c == '.' || c == '~')
                    {
                        encoded.Append(c);
                    }
                    else if (c == ' ')
                    {
                        encoded.Append('+');
                    }
                    else
                    {
                        encoded.AppendFormat("%{0:X2}", b);
                    }
                }

                return encoded.ToString();
            }
        }

        /// <summary>
        /// 清理商品名稱
        /// </summary>
        private string SanitizeItemName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return "Order Items";

            // 移除可能造成問題的字元
            itemName = itemName
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("&", "and")
                .Replace("#", "");

            // 如果名稱太長,截斷並加上省略號
            if (itemName.Length > 200)
            {
                itemName = itemName.Substring(0, 197) + "...";
            }

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

            // 計算本地 CheckMacValue
            var localCheckMac = GenerateCheckMacValue(parameters);

            bool isValid = localCheckMac.Equals(receivedCheckMac, StringComparison.OrdinalIgnoreCase);

            if (isValid)
            {
                _logger.LogInformation("✅ CheckMacValue 驗證成功");
            }
            else
            {
                _logger.LogWarning($"❌ CheckMacValue 驗證失敗!");
                _logger.LogWarning($"接收到的: {receivedCheckMac}");
                _logger.LogWarning($"計算出的: {localCheckMac}");
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