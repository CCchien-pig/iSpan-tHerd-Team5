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
		public string? AssigneeName { get; set; } // 之後可 join 員工表顯示客服名稱

        public string? UserMessage { get; set; } //某會員的工單清單（含使用者留言）


    }
}

