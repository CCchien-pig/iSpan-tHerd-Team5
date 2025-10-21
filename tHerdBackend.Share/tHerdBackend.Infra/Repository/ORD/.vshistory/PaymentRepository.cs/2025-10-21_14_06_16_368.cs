using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.ORD
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly tHerdDBContext _context;

        public PaymentRepository(tHerdDBContext context)
        {
            _context = context;
        }

        public async Task<int> CreatePaymentAsync(string orderId, int amount, string merchantTradeNo)
        {
            var payment = new OrdPayment
            {
                OrderId = int.Parse(orderId),
                PaymentConfigId = 1, // 假設 1 是信用卡
                Amount = amount,
                Status = "pending",
                MerchantTradeNo = merchantTradeNo,
                SimulatePaid = true, // 學術環境
                CreatedDate = DateTime.Now
            };

            _context.OrdPayments.Add(payment);
            await _context.SaveChangesAsync();

            return payment.PaymentId;
        }

        public async Task UpdatePaymentByTradeNoAsync(
            string tradeNo,
            string status,
            DateTime? paymentDate,
            int rtnCode,
            string rtnMsg)
        {
            var payment = await _context.OrdPayments
                .FirstOrDefaultAsync(p => p.TradeNo == tradeNo);

            if (payment != null)
            {
                payment.Status = status;
                payment.TradeDate = paymentDate;
                payment.RtnCode = rtnCode;
                payment.RtnMsg = rtnMsg;

                await _context.SaveChangesAsync();
            }
        }
    }
}