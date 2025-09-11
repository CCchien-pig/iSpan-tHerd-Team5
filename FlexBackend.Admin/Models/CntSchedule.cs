using System;
using System.Collections.Generic;

namespace FlexBackend.Admin.Models;

/// <summary>
/// 頁面排程
/// </summary>
public partial class CntSchedule
{
    /// <summary>
    /// 排程 ID
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// 頁面 ID
    /// </summary>
    public int PageId { get; set; }

    /// <summary>
    /// 排程動作
    /// </summary>
    public string ActionType { get; set; } = null!;

    /// <summary>
    /// 排程時間
    /// </summary>
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// 排程狀態
    /// </summary>
    public string Status { get; set; } = null!;

    public virtual CntPage Page { get; set; } = null!;
}
