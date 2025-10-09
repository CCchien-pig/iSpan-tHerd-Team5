using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels
{
	public class TagEditVM
	{
		public int TagId { get; set; }

		[Required(ErrorMessage = "請輸入標籤名稱")]
		[StringLength(255, ErrorMessage = "標籤名稱不能超過 255 個字")]
		public string TagName { get; set; } = string.Empty;

		public bool IsActive { get; set; } = true;

		// 編輯者資訊
		public string? Revisor { get; set; }
	}
}

