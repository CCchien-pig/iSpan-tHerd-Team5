using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.ORD
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly tHerdDBContext _context;
        private readonly ILogger<PaymentRepository> _logger;

        public PaymentRepository(
            tHerdDBContext context,
            ILogger<PaymentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> CreatePaymentAsync(
        int orderId, int paymentConfigId, int amount, string status, string merchantTradeNo)
        {
            var sql = @"
                INSERT INTO ORD_Payment
                (OrderId, PaymentConfigId, Amount, Status, MerchantTradeNo, CreatedDate, SimulatePaid)
                VALUES (@p0, @p1, @p2, @p3, @p4, SYSDATETIME(), 0);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var result = await _context.Database
                .SqlQueryRaw<int>(sql,
                    orderId,
                    paymentConfigId,
                    amount,
                    status,
                    merchantTradeNo)
                .ToListAsync();

            return result.FirstOrDefault();
        }


        public async Task<bool> UpdatePaymentByMerchantTradeNoAsync(
            string merchantTradeNo,
            string tradeNo,
            string status,
            DateTime? paymentDate,
            int? rtnCode,
            string? rtnMsg)
        {
            try
            {
                var sql = @"
                    UPDATE ORD_Payment 
                    SET TradeNo = @p1, 
                        Status = @p2, 
                        TradeDate = @p3, 
                        RtnCode = @p4, 
                        RtnMsg = @p5
                    WHERE MerchantTradeNo = @p0";

                var affectedRows = await _context.Database.ExecuteSqlRawAsync(sql,
                    merchantTradeNo,
                    tradeNo ?? "",
                    status,
                    paymentDate ?? (object)DBNull.Value,
                    rtnCode ?? (object)DBNull.Value,
                    rtnMsg ?? (object)DBNull.Value
                );

                if (affectedRows > 0)
                {
                    _logger.LogInformation($"✅ 更新付款狀態: MerchantTradeNo={merchantTradeNo}, Status={status}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"⚠️ 找不到付款記錄: MerchantTradeNo={merchantTradeNo}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ 更新付款狀態失敗: MerchantTradeNo={merchantTradeNo}");
                return false;
            }
        }
    }
}