using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FlexBackend.Core.DTOs.USER
{
	public class ApplicationRole:IdentityRole
	{
		[MaxLength(100)]
		public string? Description { get; set; }
		public DateTime CreatedDate { get; set; } = DateTime.Now;
		public DateTime? RevisedDate { get; set; }
		public int Creator { get; set; }
		public int? Reviser { get; set; }
	}
}
