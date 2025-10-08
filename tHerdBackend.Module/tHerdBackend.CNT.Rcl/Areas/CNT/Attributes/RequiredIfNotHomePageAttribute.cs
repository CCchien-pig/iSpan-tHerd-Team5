using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.Attributes
{
	/// <summary>
	/// 非首頁必填（首頁可留空）
	/// </summary>
	public class RequiredIfNotHomePageAttribute : ValidationAttribute
	{
		public int HomePageTypeId { get; }

		public RequiredIfNotHomePageAttribute(int homePageTypeId)
		{
			HomePageTypeId = homePageTypeId;
		}

		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			var pageTypeProp = validationContext.ObjectType.GetProperty("PageTypeId");
			if (pageTypeProp == null) return ValidationResult.Success;

			var pageTypeId = (int)(pageTypeProp.GetValue(validationContext.ObjectInstance) ?? 0);
			var isHomePage = pageTypeId == HomePageTypeId;

			var list = value as System.Collections.ICollection;

			if (!isHomePage && (list == null || list.Count == 0))
			{
				return new ValidationResult(ErrorMessage ?? "標籤必填");
			}

			return ValidationResult.Success;
		}
	}
}
