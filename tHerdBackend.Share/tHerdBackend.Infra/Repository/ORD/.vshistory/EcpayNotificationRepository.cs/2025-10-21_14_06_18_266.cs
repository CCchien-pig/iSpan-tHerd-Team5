using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.ORD
{
    public class EcpayNotificationRepository : IEcpayNotificationRepository
    {
        private readonly tHerdDBContext _context;

        public EcpayNotificationRepository(tHerdDBContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(EcpayNotificationDto dto)
        {
            var entity = new OrdEcpayReturnNotification
            {
                ReceivedDate = DateTime.Now,
                MerchantId = dto.MerchantID,
                PlatformId = dto.PlatformID,
                StoreId = dto.StoreID,
                MerchantTradeNo = dto.MerchantTradeNo,
                TradeNo = dto.TradeNo,
                RtnCode = dto.RtnCode,
                RtnMsg = dto.RtnMsg,
                TradeAmt = dto.TradeAmt,
                PaymentType = dto.PaymentType,
                PaymentTypeChargeFee = dto.PaymentTypeChargeFee,
                TradeDate = ParseECPayDateTime(dto.TradeDate),
                PaymentDate = string.IsNullOrEmpty(dto.PaymentDate)
                    ? null
                    : ParseECPayDateTime(dto.PaymentDate),
                SimulatePaid = dto.SimulatePaid == 1,
                CustomField1 = dto.CustomField1,
                CustomField2 = dto.CustomField2,
                CustomField3 = dto.CustomField3,
                CustomField4 = dto.CustomField4,
                CheckMacValue = dto.CheckMacValue,
                RawBody = dto.RawBody,
                RawHeaders = dto.RawHeaders,
                FailReason = dto.FailReason
            };

            _context.OrdEcpayReturnNotifications.Add(entity);
            await _context.SaveChangesAsync();
        }

        private DateTime ParseECPayDateTime(string dateTimeStr)
        {
            if (string.IsNullOrEmpty(dateTimeStr))
                return DateTime.Now;

            return DateTime.ParseExact(
                dateTimeStr,
                "yyyy/MM/dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture
            );
        }
    }
}