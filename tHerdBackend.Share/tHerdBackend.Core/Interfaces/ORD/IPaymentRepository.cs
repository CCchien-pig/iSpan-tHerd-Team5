using System;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.ORD;

namespace tHerdBackend.Core.Interfaces.ORD
{
    /// <summary>
    /// 綠界通知 Repository 介面
    /// </summary>
    public interface IEcpayNotificationRepository
    {
        /// <summary>
        /// 儲存綠界付款通知記錄
        /// </summary>
        Task CreateAsync(EcpayNotificationDto dto);
    }

    /// <summary>
    /// 付款 Repository 介面
    /// </summary>
    public interface IPaymentRepository
    {
        /// <summary>
        /// 建立付款記錄,回傳付款編號 (PaymentId)
        /// </summary>
        Task<int> CreatePaymentAsync(
            int orderId,
            int paymentConfigId,
            int amount,
            string status,
            string merchantTradeNo);

        /// <summary>
        /// 根據 MerchantTradeNo 更新付款狀態
        /// </summary>
        Task<bool> UpdatePaymentByMerchantTradeNoAsync(
            string merchantTradeNo,
            string tradeNo,
            string status,
            DateTime? paymentDate,
            int? rtnCode,
            string? rtnMsg);
    }
}