using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.ORD;

namespace tHerdBackend.Core.Interfaces.ORD
{
    /// <summary>
    /// 綠界通知記錄 Repository (稽核用)
    /// </summary>
    public interface IEcpayNotificationRepository
    {
        /// <summary>
        /// 儲存綠界通知記錄
        /// </summary>
        Task CreateAsync(EcpayNotificationDto dto);
    }
}