namespace tHerdBackend.Core.DTOs.CS
{
	/// <summary>
	/// 用於回傳到前端的客服工單資料
	/// </summary>
	public class TicketsDto
	{
		public int TicketId { get; set; }
		public string Subject { get; set; } = string.Empty;
		public string? CategoryName { get; set; }
		public string StatusText { get; set; } = "待處理"; // 轉中文顯示
		public string PriorityText { get; set; } = "中";
		public DateTime CreatedDate { get; set; }
		
	
	
}
}

