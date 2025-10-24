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
            // ✅ 使用 Console.WriteLine 確保一定會輸出
            Console.WriteLine("========================================");
            Console.WriteLine("🔥 開始產生綠界付款表單");
            Console.WriteLine($"訂單編號: {orderNo}");
            Console.WriteLine($"訂單金額: {totalAmount}");
            Console.WriteLine($"商品名稱: {itemName}");
            Console.WriteLine("========================================");

            try
            {
                // 產生唯一交易編號 (14 字元)
                string merchantTradeNo = DateTime.Now.ToString("yyyyMMddHHmmss");
                string merchantTradeDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                // 清理商品名稱
                itemName = CleanItemName(itemName);

                Console.WriteLine($"交易編號: {merchantTradeNo}");
                Console.WriteLine($"交易時間: {merchantTradeDate}");
                Console.WriteLine($"清理後商品名稱: {itemName}");

                // 準備參數 (不包含 CheckMacValue)
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

                // 計算 CheckMacValue
                string checkMacValue = CalculateCheckMacValue(parameters);

                Console.WriteLine("--- 表單參數 (依字母排序) ---");
                foreach (var p in parameters.OrderBy(x => x.Key))
                {
                    Console.WriteLine($"{p.Key} = {p.Value}");
                }
                Console.WriteLine($"CheckMacValue = {checkMacValue}");
                Console.WriteLine("========================================");

                // 產生 HTML 表單
                var html = new StringBuilder();
                html.AppendLine($"<form id='ecpayForm' method='post' action='{ActionUrl}'>");

                foreach (var p in parameters)
                {
                    html.AppendLine($"  <input type='hidden' name='{p.Key}' value='{p.Value}' />");
                }

                html.AppendLine($"  <input type='hidden' name='CheckMacValue' value='{checkMacValue}' />");
                html.AppendLine("</form>");

                string formHtml = html.ToString();
                Console.WriteLine($"✅ 表單產生成功,長度: {formHtml.Length} 字元");

                return formHtml;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 產生表單失敗: {ex.Message}");
                Console.WriteLine($"堆疊追蹤: {ex.StackTrace}");
                _logger.LogError(ex, "產生綠界表單失敗");
                throw;
            }
        }

        /// <summary>
        /// 計算 CheckMacValue
        /// </summary>
        private string CalculateCheckMacValue(Dictionary<string, string> parameters)
        {
            Console.WriteLine("--- 開始計算 CheckMacValue ---");

            // Step 1: 排序參數
            var sortedList = parameters
                .OrderBy(p => p.Key)
                .Select(p => $"{p.Key}={p.Value}")
                .ToList();

            // Step 2: 組合字串
            string step1 = $"HashKey={HashKey}&{string.Join("&", sortedList)}&HashIV={HashIV}";
            Console.WriteLine($"Step 1 原始字串: {step1}");

            // Step 3: URL Encode
            string step2 = UrlEncode(step1);
            Console.WriteLine($"Step 2 URL編碼: {step2}");

            // Step 4: 轉小寫
            string step3 = step2.ToLower();
            Console.WriteLine($"Step 3 轉小寫: {step3}");

            // Step 5: SHA256 雜湊
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(step3));
                string step4 = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
                Console.WriteLine($"Step 4 SHA256: {step4}");

                return step4;
            }
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
                if (IsUnreserved(c))
                {
                    result.Append(c);
                }
                else if (c == ' ')
                {
                    result.Append('+');
                }
                else
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(c.ToString());
                    foreach (byte b in bytes)
                    {
                        result.AppendFormat("%{0:X2}", b);
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 判斷字元是否不需編碼
        /// </summary>
        private bool IsUnreserved(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z') ||
                   c == '-' || c == '_' || c == '.' || c == '~';
        }

        /// <summary>
        /// 清理商品名稱
        /// </summary>
        private string CleanItemName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return "Order Items";

            // 移除危險字元
            itemName = itemName
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("&", "and")
                .Replace("#", "No.")
                .Replace("\r", "")
                .Replace("\n", " ")
                .Trim();

            // 限制長度
            if (itemName.Length > 200)
            {
                itemName = itemName.Substring(0, 197) + "...";
            }

            return itemName;
        }

        /// <summary>
        /// 驗證 CheckMacValue
        /// </summary>
        public bool ValidateCheckMacValue(Dictionary<string, string> parameters)
        {
            if (!parameters.ContainsKey("CheckMacValue"))
            {
                Console.WriteLine("❌ 缺少 CheckMacValue");
                return false;
            }

            var receivedMac = parameters["CheckMacValue"];
            parameters.Remove("CheckMacValue");

            var calculatedMac = CalculateCheckMacValue(parameters);
            bool isValid = calculatedMac.Equals(receivedMac, StringComparison.OrdinalIgnoreCase);

            if (isValid)
            {
                Console.WriteLine("✅ CheckMacValue 驗證成功");
            }
            else
            {
                Console.WriteLine($"❌ CheckMacValue 驗證失敗!");
                Console.WriteLine($"接收: {receivedMac}");
                Console.WriteLine($"計算: {calculatedMac}");
            }

            return isValid;
        }

        /// <summary>
        /// 處理付款通知
        /// </summary>
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