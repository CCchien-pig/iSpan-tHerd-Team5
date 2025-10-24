using System;
using System.Threading.Tasks;

namespace tHerdBackend.Core.Interfaces.ORD
{
    /// <summary>
    /// 付款交易 Repository (業務邏輯)
    /// </summary>
    public interface IPaymentRepository
    {
        /// <summary>
        /// 建立付款記錄
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