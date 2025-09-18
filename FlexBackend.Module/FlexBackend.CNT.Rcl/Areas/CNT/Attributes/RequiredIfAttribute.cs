using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FlexBackend.CNT.Rcl.Areas.CNT.Attributes
{
	/// <summary>
	/// 條件必填：只有當某個布林屬性為 true 時，這個欄位才必填
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class RequiredIfAttribute : ValidationAttribute
	{
		private readonly string _conditionProperty;

		public RequiredIfAttribute(string conditionProperty, string errorMessage)
		{
			_conditionProperty = conditionProperty;
			ErrorMessage = errorMessage;
		}

		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			var property = validationContext.ObjectType.GetProperty(_conditionProperty, BindingFlags.Public | BindingFlags.Instance);

			if (property == null)
			{
				return new ValidationResult($"找不到屬性: {_conditionProperty}");
			}

			var conditionValue = property.GetValue(validationContext.ObjectInstance) as bool? ?? false;

			if (conditionValue && value == null)
			{
				return new ValidationResult(ErrorMessage);
			}

			return ValidationResult.Success;
		}
	}
}
