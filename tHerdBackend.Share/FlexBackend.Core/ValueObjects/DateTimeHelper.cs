using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.Core.ValueObjects
{
	public static class DateTimeHelper
	{
        /// <summary>
        /// 將 DateTime? 轉成指定格式的字串，若為 null 則回傳空字串
        /// </summary>
        public static string ToDateTimeString(
            DateTime? value,
            string format = "yyyy/MM/dd HH:mm:ss",
            params DateTime[] alsoEmpty)
        {
            if (!value.HasValue) return string.Empty;

            var dt = value.Value;
            if (dt == DateTime.MinValue || (alsoEmpty?.Contains(dt) ?? false))
                return string.Empty;

            return dt.ToString(format);
        }
    }
}
