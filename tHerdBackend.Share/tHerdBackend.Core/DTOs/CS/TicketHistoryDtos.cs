namespace tHerdBackend.Core.DTOs.CS
{
	/// <summary>工單歷程</summary>
	public class TicketHistoryDto
	{
		public string Action { get; set; } = string.Empty;
		public string? FromAssignee { get; set; }
		public string? ToAssignee { get; set; }
		public string? Note { get; set; }
		public string? ChangedByName { get; set; }
		public DateTime ChangedDate { get; set; }
	}
}
