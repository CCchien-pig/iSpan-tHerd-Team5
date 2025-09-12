using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.Core.DTOs.MKT
{
    public partial class MKTCampaignDto
    {
        /// <summary>
        /// 活動Id
        /// </summary>
        public int CampaignId { get; set; }

        /// <summary>
        /// 活動名稱
        /// </summary>
        public string CampaignName { get; set; }

        /// <summary>
        /// 活動類型（例：滿額折扣、全館折扣）
        /// </summary>
        public string CampaignType { get; set; }

        /// <summary>
        /// 活動描述
        /// </summary>
        public string CampaignDescription { get; set; }

        /// <summary>
        /// 適用商品類型
        /// </summary>
        public string ProductType { get; set; }

        /// <summary>
        /// 活動狀態（Active, Inactive）
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 活動開始日期
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 活動結束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 活動圖片路徑
        /// </summary>
        public int? ImgId { get; set; }

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
        /// 異動人員
        /// </summary>
        public int? Reviser { get; set; }

        /// <summary>
        /// 異動時間
        /// </summary>
        public DateTime? RevisedDate { get; set; }
    }
}
