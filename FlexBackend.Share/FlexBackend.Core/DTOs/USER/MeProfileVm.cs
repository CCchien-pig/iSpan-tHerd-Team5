namespace FlexBackend.Core.DTOs.USER
{
	public class MeProfileVm
	{
		public string Id { get; set; } = "";
		public int UserNumberId { get; set; }   // ← 改這裡
		public string? LastName { get; set; }
		public string? FirstName { get; set; }
		public string? Email { get; set; }
		public string? PhoneNumber { get; set; }
		public string Gender { get; set; } = "N/A";
		public DateTime? BirthDate { get; set; }
		public string? Address { get; set; }
		public bool IsActive { get; set; }
	}
}