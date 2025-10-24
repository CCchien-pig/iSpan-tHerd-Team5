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
        /// <param name="orderNo">訂單編號 (例如: 20251024000001)</param>
        /// <param name="totalAmount">訂單金額</param>
        /// <param name="itemName">商品名稱</param>
        public string CreatePaymentForm(string orderNo, int totalAmount, string itemName)
        {
            // ✅ 修正 1: MerchantTradeNo 只用時間戳,確保不超過 20 字元
            string merchantTradeNo = DateTime.Now.ToString("yyyyMMddHHmmss"); // 14 字元

            // ✅ 修正 2: 準備所有必要參數
            var parameters = new Dictionary<string, string>
            {
                { "MerchantID", MerchantID },
                { "MerchantTradeNo", merchantTradeNo },
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", totalAmount.ToString() },
                { "TradeDesc", "tHerd Order Payment" },
                { "ItemName", SanitizeItemName(itemName) },
                { "ReturnURL", "https://yourdomain.com/api/ord/payment/notify" }, // ⚠️ 記得改成你的網址
                { "ChoosePayment", "Credit" }, // 信用卡
                { "EncryptType", "1" } // SHA256
            };

            // ✅ 修正 3: 計算 CheckMacValue
            string checkMacValue = GenerateCheckMacValue(parameters);
            parameters.Add("CheckMacValue", checkMacValue);

            // ✅ 修正 4: 產生 HTML 表單 (不加 auto-submit script,讓前端控制)
            var form = new StringBuilder();
            form.AppendLine($"<form id='ecpayForm' method='post' action='{ActionUrl}'>");

            foreach (var param in parameters)
            {
                form.AppendLine($"  <input type='hidden' name='{param.Key}' value='{param.Value}' />");
            }

            form.AppendLine("</form>");

            // ❌ 不要加這行,讓前端控制提交
            // form.AppendLine("<script>document.getElementById('ecpayForm').submit();</script>");

            _logger.LogInformation($"✅ 產生綠界表單: OrderNo={orderNo}, MerchantTradeNo={merchantTradeNo}, Total={totalAmount}");
            _logger.LogInformation($"📋 CheckMacValue={checkMacValue}");

            return form.ToString();
        }

        /// <summary>
        /// 計算 CheckMacValue (綠界檢查碼)
        /// </summary>
        private string GenerateCheckMacValue(Dictionary<string, string> parameters)
        {
            // 1. 依照 Key 排序 (不分大小寫)
            var sortedParams = parameters
                .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                .Select(p => $"{p.Key}={p.Value}");

            // 2. 組合字串: HashKey={Key}&param1=value1&param2=value2&HashIV={IV}
            string rawString = $"HashKey={HashKey}&{string.Join("&", sortedParams)}&HashIV={HashIV}";

            _logger.LogDebug($"原始字串: {rawString}");

            // 3. URL Encode
            string encodedString = UrlEncode(rawString);

            _logger.LogDebug($"編碼後: {encodedString}");

            // 4. 轉小寫
            encodedString = encodedString.ToLower();

            // 5. SHA256 雜湊
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(encodedString));
                string checkMacValue = BitConverter.ToString(hash).Replace("-", "").ToUpper();

                _logger.LogDebug($"CheckMacValue: {checkMacValue}");
                return checkMacValue;
            }
        }

        /// <summary>
        /// URL Encode (綠界專用規則)
        /// </summary>
        private string UrlEncode(string value)
        {
            var encoded = new StringBuilder();

            foreach (char c in value)
            {
                if ((c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= 'a' && c <= 'z') ||
                    c == '-' || c == '_' || c == '.' || c == '~')
                {
                    // 不需要編碼的字元
                    encoded.Append(c);
                }
                else if (c == ' ')
                {
                    // 空格轉為 +
                    encoded.Append('+');
                }
                else
                {
                    // 其他字元轉為 %XX
                    byte[] bytes = Encoding.UTF8.GetBytes(c.ToString());
                    foreach (byte b in bytes)
                    {
                        encoded.Append($"%{b:X2}");
                    }
                }
            }

            return encoded.ToString();
        }

        /// <summary>
        /// 清理商品名稱 (避免特殊字元導致錯誤)
        /// </summary>
        private string SanitizeItemName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return "商品";

            // 移除可能造成問題的字元
            itemName = itemName.Replace("'", "")
                               .Replace("\"", "")
                               .Replace("<", "")
                               .Replace(">", "");

            // 限制長度 (綠界限制 200 字元)
            if (itemName.Length > 200)
                itemName = itemName.Substring(0, 200);

            return itemName;
        }

        /// <summary>
        /// 驗證 CheckMacValue (用於接收綠界回傳)
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