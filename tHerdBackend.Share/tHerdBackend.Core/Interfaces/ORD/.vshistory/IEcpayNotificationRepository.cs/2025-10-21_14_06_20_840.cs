using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.ORD;

namespace tHerdBackend.Core.Interfaces.ORD
{
    public interface IEcpayNotificationRepository
    {
        Task CreateAsync(EcpayNotificationDto dto);
    }

    public interface IPaymentRepository
    {
        Task<int> CreatePaymentAsync(string orderId, int amount, string merchantTradeNo);
        Task UpdatePaymentByTradeNoAsync(string tradeNo, string status, DateTime? paymentDate, int rtnCode, string rtnMsg);
    }
}
