﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ECPayService> _logger;

        public ECPayService(
            IOptions<ECPayConfig> config,
            IEcpayNotificationRepository notificationRepo,
            IPaymentRepository paymentRepo,
            ILogger<ECPayService> logger)
        {
            _config = config.Value;
            _notificationRepo = notificationRepo;
            _paymentRepo = paymentRepo;
            _logger = logger;
        }

        /// <summary>
        /// 建立綠界付款表單 HTML
        /// </summary>
        public string CreatePaymentForm(string orderId, int totalAmount, string itemName)
        {
            var merchantTradeNo = orderId;
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

            _logger.LogInformation($"建立付款表單: MerchantTradeNo={merchantTradeNo}, Amount={totalAmount}");
            return formHtml.ToString();
        }

        /// <summary>
        /// 產生 CheckMacValue
        /// </summary>
        private string GenerateCheckMacValue(SortedDictionary<string, string> parameters)
        {
            var hashKey = _config.HashKey;
            var hashIV = _config.HashIV;

            // 1️⃣ 組合原始字串 (未編碼)
            var raw = new StringBuilder();
            raw.Append($"HashKey={hashKey}");
            foreach (var p in parameters)
            {
                raw.Append($"&{p.Key}={p.Value}");
            }
            raw.Append($"&HashIV={hashIV}");

            // 2️⃣ URL Encode (ECPay 要求空白轉 +)
            string encoded = System.Web.HttpUtility.UrlEncode(raw.ToString()).ToLower();

            // 3️⃣ 依官方規則替換保留字元
            encoded = encoded
                .Replace("%2d", "-").Replace("%5f", "_").Replace("%2e", ".")
                .Replace("%21", "!").Replace("%2a", "*").Replace("%28", "(").Replace("%29", ")");

            // 4️⃣ 轉 MD5 → 大寫
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(encoded));
            var checkMac = string.Concat(hash.Select(b => b.ToString("X2")));

            _logger.LogInformation($"✅ CheckMac 原始: {raw}");
            _logger.LogInformation($"✅ CheckMac Encode: {encoded}");
            _logger.LogInformation($"✅ CheckMac 結果: {checkMac}");

            return checkMac;
        }



        /// <summary>
        /// 驗證 CheckMacValue
        /// </summary>
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

        /// <summary>
        /// 處理綠界付款通知
        /// </summary>
        public async Task<bool> ProcessPaymentNotificationAsync(EcpayNotificationDto dto)
        {
            try
            {
                // 1. 儲存通知記錄到 ORD_EcpayReturnNotification
                await _notificationRepo.CreateAsync(dto);
                _logger.LogInformation($"儲存綠界通知記錄成功: MerchantTradeNo={dto.MerchantTradeNo}");

                // 2. 更新 ORD_Payment 狀態
                // 解析 PaymentDate (成功時才有)
                DateTime? paymentDate = null;
                if (!string.IsNullOrEmpty(dto.PaymentDate))
                {
                    if (DateTime.TryParseExact(dto.PaymentDate, "yyyy/MM/dd HH:mm:ss",
                        null, System.Globalization.DateTimeStyles.None, out var parsedPaymentDate))
                    {
                        paymentDate = parsedPaymentDate;
                    }
                    else
                    {
                        _logger.LogWarning($"無法解析 PaymentDate: {dto.PaymentDate}");
                    }
                }

                // ✅ 根據您的介面,只有 6 個參數 (沒有 tradeDate)
                if (dto.RtnCode == 1) // 付款成功
                {
                    var updateResult = await _paymentRepo.UpdatePaymentByMerchantTradeNoAsync(
                        dto.MerchantTradeNo,    // merchantTradeNo
                        dto.TradeNo,            // tradeNo
                        "success",              // status
                        paymentDate,            // paymentDate (這個會更新到 TradeDate 欄位)
                        dto.RtnCode,            // rtnCode
                        dto.RtnMsg              // rtnMsg
                    );

                    if (updateResult)
                    {
                        _logger.LogInformation($"付款成功: MerchantTradeNo={dto.MerchantTradeNo}, TradeNo={dto.TradeNo}");
                    }
                    else
                    {
                        _logger.LogWarning($"找不到對應的付款記錄: MerchantTradeNo={dto.MerchantTradeNo}");
                    }
                }
                else // 付款失敗
                {
                    var updateResult = await _paymentRepo.UpdatePaymentByMerchantTradeNoAsync(
                        dto.MerchantTradeNo,    // merchantTradeNo
                        dto.TradeNo,            // tradeNo
                        "failed",               // status
                        null,                   // paymentDate
                        dto.RtnCode,            // rtnCode
                        dto.RtnMsg              // rtnMsg
                    );

                    if (updateResult)
                    {
                        _logger.LogWarning($"付款失敗: MerchantTradeNo={dto.MerchantTradeNo}, RtnCode={dto.RtnCode}, RtnMsg={dto.RtnMsg}");
                    }
                    else
                    {
                        _logger.LogWarning($"找不到對應的付款記錄: MerchantTradeNo={dto.MerchantTradeNo}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"處理綠界通知失敗: MerchantTradeNo={dto.MerchantTradeNo}, 錯誤: {ex.Message}");
                return false;
            }
        }
    }
}