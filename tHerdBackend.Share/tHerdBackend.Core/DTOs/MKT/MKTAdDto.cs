using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.DTOs.MKT
{
    public class MKTAdDto
    {
        /// <summary>
        /// 廣告編號
        /// </summary>
        public int AdId { get; set; }

        /// <summary>
        /// 廣告標題
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 廣告內容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 廣告圖片路徑
        /// </summary>
        public int? ImgId { get; set; }

        /// <summary>
        /// 開始日期
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 結束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 狀態（上架/下架）
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 是否啟用（0=否，1=是）
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
