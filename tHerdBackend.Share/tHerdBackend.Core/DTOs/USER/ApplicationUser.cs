using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tHerdBackend.Core.DTOs.USER
{
	public class ApplicationUser:IdentityUser
	{

		public int UserNumberId { get; set; }
		[MaxLength(20)]
		public string LastName { get; set; }
		[MaxLength(20)]
		public string FirstName { get; set; }

		// 你也可以選擇性地保留一個 FullName 屬性來方便使用
		[NotMapped] // 這個屬性不會在資料庫中產生欄位
		public string FullName => $"{LastName} {FirstName}";
		public int? ImgId { get; set; }
		[MaxLength(10)]
		public string Gender { get; set; }
		public DateTime? BirthDate { get; set; }
		[MaxLength(100)]
		public string? Address { get; set; }
		[MaxLength(10)]
		public string MemberRankId { get; set; } = "MR001";
		[MaxLength(20)]
		public string? ReferralCode { get; set; }
		[MaxLength(20)]
		public string? UsedReferralCode { get; set; }
		public DateTime CreatedDate { get; set; } = DateTime.Now;
		public DateTime? RevisedDate { get; set; }
		public DateTime? LastLoginDate { get; set; }
		public bool IsActive{ get; set; } = true;
		public DateTime? ActivationDate { get; set; }
		//[MaxLength(500)]
		public int? Creator { get; set; }
		//[MaxLength(500)]
		public int? Reviser{ get; set; }
	}
}
