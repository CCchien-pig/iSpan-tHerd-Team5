using Microsoft.AspNetCore.Http;
using tHerdBackend.Core.DTOs.CS;

namespace tHerdBackend.Core.Interfaces.CS
{
	/// <summary>客服工單服務層介面</summary>
	public interface ICsTicketService
	{
		/// <summary>取得全部工單清單</summary>
		Task<IEnumerable<TicketsDto>> GetAllAsync();

		/// <summary>建立新工單</summary>
		Task<int> CreateAsync(TicketIn input, IFormFile? image = null);

		/// <summary>查單筆工單（用於回傳建立結果）</summary>
		Task<TicketOut?> GetTicketByIdAsync(int ticketId);

        /// <summary>取得某會員工單清單</summary>
        Task<IEnumerable<TicketsDto>> GetByUserIdAsync(int userNumberId);
    }

}


