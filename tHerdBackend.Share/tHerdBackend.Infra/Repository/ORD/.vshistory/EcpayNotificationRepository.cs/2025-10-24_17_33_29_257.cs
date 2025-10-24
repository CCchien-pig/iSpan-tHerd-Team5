using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.ORD
{
    public class EcpayNotificationRepository : IEcpayNotificationRepository
    {
        private readonly tHerdDBContext _context;
        private readonly ILogger<EcpayNotificationRepository> _logger;

        public EcpayNotificationRepository(
            tHerdDBContext context,
            ILogger<EcpayNotificationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateAsync(EcpayNotificationDto dto)
        {
            try
            {
                var sql = @"
                    INSERT INTO ORD_EcpayReturnNotification 
                    (ReceivedDate, MerchantId, TradeNo, RtnCode, RtnMsg, TradeAmt, 
                     PaymentType, TradeDate, PaymentDate, CheckMacValue, RawBody)
                    VALUES 
                    (SYSDATETIME(), @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9)";

                await _context.Database.ExecuteSqlRawAsync(sql,
                    dto.MerchantID ?? "",
                    dto.TradeNo ?? "",
                    dto.RtnCode,
                    dto.RtnMsg ?? "",
                    dto.TradeAmt,
                    dto.PaymentType ?? "Credit",
                    string.IsNullOrEmpty(dto.TradeDate)
                        ? DateTime.Now
                        : DateTime.ParseExact(dto.TradeDate, "yyyy/MM/dd HH:mm:ss", null),
                    string.IsNullOrEmpty(dto.PaymentDate)
                        ? (object)DBNull.Value
                        : DateTime.ParseExact(dto.PaymentDate, "yyyy/MM/dd HH:mm:ss", null),
                    dto.CheckMacValue ?? "",
                    dto.RawBody ?? ""
                );

                _logger.LogInformation($"✅ 儲存綠界通知: TradeNo={dto.TradeNo}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ 儲存綠界通知失敗: TradeNo={dto.TradeNo}");
                throw;
            }
        }
    }
}