using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Infra.Repositories.Interfaces.CS;

namespace tHerdBackend.Services.CS
{
	/// <summary>客服工單業務邏輯層</summary>
	public class CsTicketService : ICsTicketService
	{
		private readonly ICsTicketRepository _repo;

		public CsTicketService(ICsTicketRepository repo)
		{
			_repo = repo;
		}

		/// <summary>取得全部工單清單</summary>
		public async Task<IEnumerable<TicketsDto>> GetAllAsync()
		{
			return await _repo.GetAllAsync();
		}

		/// <summary>建立新工單，回傳 TicketId</summary>
		public async Task<int> CreateAsync(TicketIn input)
		{
			return await _repo.CreateAsync(input);
		}

		/// <summary>查單筆工單（建立後回傳）</summary>
		public async Task<TicketOut?> GetTicketByIdAsync(int ticketId)
		{
			var tickets = await _repo.GetAllAsync();
			var found = tickets.FirstOrDefault(x => x.TicketId == ticketId);
			return found == null ? null : new TicketOut
			{
				TicketId = found.TicketId,
				Subject = found.Subject,
				CategoryName = found.CategoryName ?? "未分類",
				PriorityText = found.PriorityText,
				CreatedDate = found.CreatedDate,
				StatusText = found.StatusText
			};
		}
	}
}
