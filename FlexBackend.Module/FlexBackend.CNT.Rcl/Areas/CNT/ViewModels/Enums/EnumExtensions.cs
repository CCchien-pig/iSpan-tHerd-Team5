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

		public static string ToDisplayName(this ActionType actionType) =>
	   actionType switch
	   {
		   ActionType.PublishPage => "📢 發布文章",
		   ActionType.UnpublishPage => "📥 下架文章",
		   ActionType.PublishCoupon => "🎟️ 發布優惠券",
		   ActionType.Featured => "⭐ 精選文章",
		   ActionType.Unfeatured => "❌ 取消精選",
		   ActionType.ClearAllSchedules => "\U0001f9f9 清空所有排程",
		   _ => "未知"
	   };
	}
}
