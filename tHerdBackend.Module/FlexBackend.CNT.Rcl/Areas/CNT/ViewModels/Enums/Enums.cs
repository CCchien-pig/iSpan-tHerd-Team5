using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums
{
	public enum PageStatus
	{
		Draft = 0,      // 草稿
		Published = 1,  // 已發布
		Archived = 2,   // 下架/封存
		Deleted = 9     // 刪除
	}
}
