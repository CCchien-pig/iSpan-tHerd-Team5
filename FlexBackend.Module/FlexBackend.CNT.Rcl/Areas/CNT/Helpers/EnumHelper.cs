using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace FlexBackend.CNT.Rcl.Helpers
{
	public static class EnumHelper
	{
		/// <summary>
		/// 輸入 Enum 值，取出 Display(Name)
		/// </summary>
		public static string GetDisplayName<TEnum>(TEnum value) where TEnum : struct, Enum
		{
			var member = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
			if (member != null)
			{
				var display = member.GetCustomAttribute<DisplayAttribute>();
				if (display != null)
				{
					return display.Name ?? value.ToString();
				}
			}
			return value.ToString();
		}

		/// <summary>
		/// 輸入字串 (數字或 Enum 名稱)，轉換後取出 Display(Name)
		/// </summary>
		public static string GetDisplayName<TEnum>(string value) where TEnum : struct, Enum
		{
			// 如果是數字字串
			if (int.TryParse(value, out int intValue))
			{
				if (Enum.IsDefined(typeof(TEnum), intValue))
				{
					var enumValue = (TEnum)Enum.ToObject(typeof(TEnum), intValue);
					return GetDisplayName(enumValue);
				}
			}
			// 如果是 Enum 名稱
			else if (Enum.TryParse<TEnum>(value, out var enumValue))
			{
				return GetDisplayName(enumValue);
			}

			return "未知";
		}
	}
}
