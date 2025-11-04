namespace tHerdBackend.Core.DTOs.USER;

public class UserDetailDto
{
	public string Id { get; set; } = default!;
	public int UserNumberId { get; set; }
	public string Email { get; set; } = default!;
	public string LastName { get; set; } = default!;
	public string FirstName { get; set; } = default!;
	public string Name { get; set; } = default!;
	public int? ImgId { get; set; }
	public string Gender { get; set; } = default!;
	public DateTime? BirthDate { get; set; }
	public string? Address { get; set; }
	public string MemberRankId { get; set; } = default!;
	public string? ReferralCode { get; set; }
	public string? UsedReferralCode { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime? RevisedDate { get; set; }
	public DateTime? LastLoginDate { get; set; }
	public bool IsActive { get; set; }
	public DateTime? ActivationDate { get; set; }
	public string? PhoneNumber { get; set; }
	public bool EmailConfirmed { get; set; }
	public bool TwoFactorEnabled { get; set; }
}
