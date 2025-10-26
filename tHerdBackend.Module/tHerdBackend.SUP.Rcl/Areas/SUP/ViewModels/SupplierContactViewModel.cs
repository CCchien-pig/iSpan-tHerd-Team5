using System.ComponentModel.DataAnnotations;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels
{
	public class SupplierContactViewModel
	{
		public int SupplierId { get; set; }

		[Display(Name = "供應商名稱")]
		[Required(ErrorMessage = "請填入供應商名稱")]
		[StringLength(100)]
		public string SupplierName { get; set; }

		[Display(Name = "聯絡人姓名")]
		[Required(ErrorMessage = "請填入聯絡人姓名")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "姓名最少需要兩個字元")]
		public string ContactName { get; set; }

		[Display(Name = "聯絡電話")]
		[Required(ErrorMessage = "請填入聯絡電話")]
		[RegularExpression(@"^(0(2|3|4|5|6|7|8)\d{7,8}|09\d{8})$",
			ErrorMessage = "請輸入正確的台灣電話號碼（市話或手機)，不含符號")]
		public string Phone { get; set; }


		[Display(Name = "電子郵件")]
		[Required(ErrorMessage = "請填入聯絡電子郵件")]
		[EmailAddress(ErrorMessage = "電子郵件格式錯誤")]
		[StringLength(100)]
		public string Email { get; set; }

		[Display(Name = "啟用狀態")]
		public bool IsActive { get; set; }

		// 唯讀欄位

		[Display(Name = "建檔人員ID")]
		public int? Creator { get; set; }

		[Display(Name = "建檔時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateTime CreatedDate { get; set; }

		[Display(Name = "最後異動人員ID")]
		public int? Reviser { get; set; }

		[Display(Name = "最後異動時間")]
		[DisplayFormat(DataFormatString = "{0:yyyy/MM/dd tt hh:mm:ss}", ApplyFormatInEditMode = true)]
		public DateTime? RevisedDate { get; set; }


		[Display(Name = "名下品牌名稱")]
		public List<string> BrandNames { get; set; } = new();

		/*
		// 台灣時間顯示
		[Display(Name = "建檔時間 (台灣)")]
		public string CreatedDate => CreatedDate.ToLocalTime().AddHours(8).ToString("yyyy/MM/dd HH:mm");

		[Display(Name = "最後異動時間 (台灣)")]
		public string? RevisedDate => RevisedDate.HasValue
			? RevisedDate.Value.ToLocalTime().AddHours(8).ToString("yyyy/MM/dd HH:mm")
			: null;
		*/
	}
}
