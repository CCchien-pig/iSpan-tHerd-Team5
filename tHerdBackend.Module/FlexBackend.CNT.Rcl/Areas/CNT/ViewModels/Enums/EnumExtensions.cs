using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums
{
	public static class EnumExtensions
	{
		public static string GetDisplayName(this Enum enumValue)
		{
			var member = enumValue.GetType()
				.GetMember(enumValue.ToString())
				.FirstOrDefault();

			if (member != null)
			{
				var displayAttr = member.GetCustomAttribute<DisplayAttribute>();
				if (displayAttr != null)
				{
					return displayAttr.Name ?? enumValue.ToString();
				}
			}

			return enumValue.ToString();
		}
	}
}
