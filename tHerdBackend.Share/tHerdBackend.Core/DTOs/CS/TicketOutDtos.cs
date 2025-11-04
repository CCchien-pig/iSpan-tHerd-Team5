namespace tHerdBackend.Core.DTOs.CS
{
	/// <summary>
	/// 用於回傳新增後的客服工單資料
	/// </summary>
	public class TicketOut
	{
		public int TicketId { get; set; }
		public string Subject { get; set; } = string.Empty;
		public string CategoryName { get; set; } = string.Empty;
		public string PriorityText { get; set; } = "中";
		public DateTime CreatedDate { get; set; }
		public string StatusText { get; set; } = "待處理"; // 新增：狀態文字
        public string? Email { get; set; }
    }
}
