using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using tHerdBackend.Core.Interfaces.ORD;

namespace tHerdBackend.Infra.Repository.ORD
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<PaymentRepository> _logger;

        public PaymentRepository(
            IConfiguration configuration,
            ILogger<PaymentRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found");
            _logger = logger;
        }

        /// <summary>
        /// 建立付款記錄,回傳付款編號 (PaymentId)
        /// </summary>
        public async Task<int> CreatePaymentAsync(
            int orderId,
            int paymentConfigId,
            int amount,
            string status,
            string merchantTradeNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var sql = @"
                    INSERT INTO [dbo].[ORD_Payment] 
                        ([OrderId], [PaymentConfigId], [Amount], [Status], [MerchantTradeNo], [CreatedDate])
                    VALUES 
                        (@OrderId, @PaymentConfigId, @Amount, @Status, @MerchantTradeNo, GETDATE());
                    
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var paymentId = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    OrderId = orderId,
                    PaymentConfigId = paymentConfigId,
                    Amount = amount,
                    Status = status,
                    MerchantTradeNo = merchantTradeNo
                });

                _logger.LogInformation($"建立付款記錄成功: PaymentId={paymentId}, OrderId={orderId}");
                return paymentId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"建立付款記錄失敗: OrderId={orderId}");
                throw;
            }
        }

        /// <summary>
        /// 根據 MerchantTradeNo 更新付款狀態
        /// </summary>
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
                using var connection = new SqlConnection(_connectionString);

                var sql = @"
                    UPDATE [dbo].[ORD_Payment]
                    SET 
                        [TradeNo] = @TradeNo,
                        [Status] = @Status,
                        [TradeDate] = @PaymentDate,
                        [RtnCode] = @RtnCode,
                        [RtnMsg] = @RtnMsg
                    WHERE [MerchantTradeNo] = @MerchantTradeNo";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    MerchantTradeNo = merchantTradeNo,
                    TradeNo = tradeNo,
                    Status = status,
                    PaymentDate = paymentDate,
                    RtnCode = rtnCode,
                    RtnMsg = rtnMsg
                });

                if (rowsAffected == 0)
                {
                    _logger.LogWarning($"找不到付款記錄: MerchantTradeNo={merchantTradeNo}");
                    return false;
                }
                else
                {
                    _logger.LogInformation($"更新付款狀態成功: MerchantTradeNo={merchantTradeNo}, TradeNo={tradeNo}, Status={status}, RowsAffected={rowsAffected}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新付款狀態失敗: MerchantTradeNo={merchantTradeNo}");
                throw;
            }
        }
    }
}