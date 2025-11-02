namespace tHerdBackend.Core.DTOs.USER;

public class NotificationDto
{
	public int NotificationId { get; set; }
	public string NotificationType { get; set; } = default!;
	public bool IsActive { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime? RevisedDate { get; set; }
}
