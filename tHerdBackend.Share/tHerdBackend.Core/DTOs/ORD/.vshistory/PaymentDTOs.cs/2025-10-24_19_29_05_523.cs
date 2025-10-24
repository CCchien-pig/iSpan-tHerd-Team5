using System;

namespace tHerdBackend.Core.DTOs.ORD
{
    /// <summary>
    /// 建立付款記錄 DTO
    /// </summary>
    public class PaymentRecordCreateDto
    {
        /// <summary>
        /// 付款明細 ID
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// 訂單 ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// 付款方式 ID (1000 = 信用卡)
        /// </summary>
        public int PaymentConfigId { get; set; }

        /// <summary>
        /// 付款金額 (整數)
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// 付款狀態 (預設 "pending")
        /// </summary>
        public string Status { get; set; } = "pending";

        /// <summary>
        /// 商戶訂單編號 (格式: 訂單號_時間戳)
        /// </summary>
        public string? MerchantTradeNo { get; set; }
    }
}