namespace tHerdBackend.Core.DTOs.CS
{
	/// <summary>
	/// 用於前端傳入新增工單資料
	/// </summary>
	public class TicketIn
	{
		public int UserId { get; set; }
		public int? CategoryId { get; set; }
		public string Subject { get; set; } = string.Empty;
		public int Priority { get; set; } = 2; // 預設中
		public string MessageText { get; set; } = string.Empty; // 新增：第一則訊息內容
        public string? Email { get; set; } 
    }
}
