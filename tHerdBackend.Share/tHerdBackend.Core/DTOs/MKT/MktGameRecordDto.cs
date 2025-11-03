using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.MKT
{
    public class MktGameRecordDto
    {
        /// <summary>
        /// 主鍵，自動遞增 ID
        /// </summary>
        public int GameRecordId { get; set; }

        /// <summary>
        /// 會員 ID (FK)
        /// </summary>
        public int UserNumberId { get; set; }

        /// <summary>
        /// 當局得分
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 兌換金額
        /// </summary>
        public int CouponAmount { get; set; }

        /// <summary>
        /// 遊戲日期（同會員每日僅一筆）
        /// </summary>
        public DateTime PlayedDate { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
