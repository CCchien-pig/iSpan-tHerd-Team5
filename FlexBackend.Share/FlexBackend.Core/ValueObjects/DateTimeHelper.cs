using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.Core.ValueObjects
{
	public  class DateTimeHelper
	{
		/// <summary>
		/// 將 DateTime? 轉成指定格式的字串，若為 null 則回傳空字串
		/// </summary>
		public static string ToDateTimeString(DateTime? value, string format = "yyyy-MM-dd HH:mm:ss")
		{
			return value.HasValue ? value.Value.ToString(format) : string.Empty;
		}
	}
}
