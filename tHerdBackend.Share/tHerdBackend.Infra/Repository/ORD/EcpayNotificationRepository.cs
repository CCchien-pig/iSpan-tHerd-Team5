using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;

namespace tHerdBackend.Infra.Repository.ORD
{
    public class EcpayNotificationRepository : IEcpayNotificationRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<EcpayNotificationRepository> _logger;

        public EcpayNotificationRepository(
            IConfiguration configuration,
            ILogger<EcpayNotificationRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found");
            _logger = logger;
        }

        /// <summary>
        /// 儲存綠界付款通知記錄
        /// </summary>
        public async Task CreateAsync(EcpayNotificationDto dto)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var sql = @"
                    INSERT INTO [dbo].[ORD_EcpayReturnNotification]
                    (
                        [MerchantId],
                        [PlatformId],
                        [StoreId],
                        [MerchantTradeNo],
                        [TradeNo],
                        [RtnCode],
                        [RtnMsg],
                        [TradeAmt],
                        [PaymentType],
                        [PaymentTypeChargeFee],
                        [TradeDate],
                        [PaymentDate],
                        [SimulatePaid],
                        [CheckMacValue],
                        [RawBody],
                        [RawHeaders],
                        [FailReason],
                        [CustomField1],
                        [CustomField2],
                        [CustomField3],
                        [CustomField4],
                        [ReceivedDate]
                    )
                    VALUES
                    (
                        @MerchantID,
                        @PlatformID,    -- DTO 屬性名稱
                        @StoreID,       -- DTO 屬性名稱
                        @MerchantTradeNo,
                        @TradeNo,
                        @RtnCode,
                        @RtnMsg,
                        @TradeAmt,
                        @PaymentType,
                        @PaymentTypeChargeFee,
                        @TradeDate,
                        @PaymentDate,
                        @SimulatePaid,
                        @CheckMacValue,
                        @RawBody,
                        @RawHeaders,
                        @FailReason,
                        @CustomField1,
                        @CustomField2,
                        @CustomField3,
                        @CustomField4,
                        GETDATE()
                    )";

                await connection.ExecuteAsync(sql, dto);

                _logger.LogInformation($"儲存綠界通知成功: MerchantTradeNo={dto.MerchantTradeNo}, TradeNo={dto.TradeNo}, RtnCode={dto.RtnCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"儲存綠界通知失敗: MerchantTradeNo={dto.MerchantTradeNo}, 錯誤: {ex.Message}");
                throw;
            }
        }
    }
}