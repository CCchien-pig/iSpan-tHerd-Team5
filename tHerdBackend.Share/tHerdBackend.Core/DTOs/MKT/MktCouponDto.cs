using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.MKT
{
    public class MktCouponDto
    {
        // <summary>
    /// 優惠券Id
    /// </summary>
    public int CouponId { get; set; }

        /// <summary>
        /// 關聯活動Id
        /// </summary>
        public int CampaignId { get; set; }

        /// <summary>
        /// 關聯規則Id
        /// </summary>
        public int RuleId { get; set; }

        /// <summary>
        /// 優惠券名稱
        /// </summary>
        public required string CouponName { get; set; }

        /// <summary>
        /// 優惠券代碼
        /// </summary>
        public required string CouponCode { get; set; }

        /// <summary>
        /// 狀態（有效、停用）
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// 發放開始日期
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 發放結束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 折扣金額
        /// </summary>
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// 折扣百分比
        /// </summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>
        /// 總發放數量
        /// </summary>
        public int TotQty { get; set; }

        /// <summary>
        /// 剩餘數量
        /// </summary>
        public int LeftQty { get; set; }

        /// <summary>
        /// 每人可領取上限
        /// </summary>
        public int UserLimit { get; set; }

        /// <summary>
        /// 有效時長（小時）
        /// </summary>
        public int ValidHours { get; set; }

        /// <summary>
        /// 是否啟用（0=否,1=是）
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 建檔人員
        /// </summary>
        public int Creator { get; set; }

        /// <summary>
        /// 建檔時間
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// 使用者是否已領取（非資料表欄位）
        /// </summary>
        public bool IsReceived { get; set; }  // 🆕 新增
    }
}
