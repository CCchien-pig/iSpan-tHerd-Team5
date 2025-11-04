namespace tHerdBackend.Core.DTOs.CS
{
	public class TicketMessageDto
	{
        public int MessageId { get; set; }
        public int TicketId { get; set; }
        public byte SenderType { get; set; } // 1=客戶, 2=客服
		public string MessageText { get; set; } = string.Empty;
		public string? AttachmentUrl { get; set; }
		public DateTime CreatedDate { get; set; }
	}
}
