using tHerdBackend.Core.DTOs.CS;

namespace tHerdBackend.Infra.Repositories.Interfaces.CS
{
	public interface ICsTicketRepository
	{
		Task<IEnumerable<TicketsDto>> GetAllAsync();
		Task<int> CreateAsync(TicketIn dto);
	}
}
