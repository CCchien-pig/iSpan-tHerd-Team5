using System;
using System.Collections.Generic;

namespace FlexBackend.Admin.Models;

/// <summary>
/// 頁面分類
/// </summary>
public partial class CntPageType
{
    /// <summary>
    /// 分類 ID
    /// </summary>
    public int PageTypeId { get; set; }

    /// <summary>
    /// 分類名稱
    /// </summary>
    public string TypeName { get; set; } = null!;

    public virtual ICollection<CntPage> CntPages { get; set; } = new List<CntPage>();
}
