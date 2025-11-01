using Microsoft.AspNetCore.Http;
using tHerdBackend.Core.DTOs.CS;

namespace tHerdBackend.Infra.Repositories.Interfaces.CS
{
	public interface ICsTicketRepository
	{
		Task<IEnumerable<TicketsDto>> GetAllAsync();
		Task<int> CreateAsync(TicketIn dto);
		Task AddHistoryAsync(int ticketId, string action, int? fromAssigneeId, int? toAssigneeId,
							byte? oldStatus, byte? newStatus, string note, int changedBy);
		Task AddMessageAsync(int ticketId, byte senderType, string messageText, string? attachmentUrl = null);
		Task<int?> AddAttachmentAsync(IFormFile file, CancellationToken ct = default);
		Task UpdateImgIdAsync(int ticketId, int fileId);
		Task<IEnumerable<TicketsDto>> GetByUserIdAsync(int userId);
	}
}

