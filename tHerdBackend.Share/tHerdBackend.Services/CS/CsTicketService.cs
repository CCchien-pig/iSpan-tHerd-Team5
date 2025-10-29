using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Infra.Repositories.Interfaces.CS;

namespace tHerdBackend.Services.CS
{
	public class CsTicketService
	{
		private readonly ICsTicketRepository _repo;

		public CsTicketService(ICsTicketRepository repo)
		{
			_repo = repo;
		}

		public async Task<IEnumerable<TicketsDto>> GetTicketsAsync()
		{
			return await _repo.GetAllAsync();
		}

		public async Task<TicketOut> CreateAsync(TicketIn input)
		{
			var newId = await _repo.CreateAsync(input);

			// 轉換輸出 DTO（簡化版）
			return new TicketOut
			{
				TicketId = newId,
				Subject = input.Subject,
				CategoryName = GetCategoryName(input.CategoryId),
				PriorityText = GetPriorityText(input.Priority),
				CreatedDate = DateTime.Now
			};
		}

		private string GetPriorityText(int p) => p switch
		{
			1 => "高",
			2 => "中",
			3 => "低",
			_ => "中"
		};

		private string GetCategoryName(int? categoryId)
		{
			return categoryId switch
			{
				1001 => "訂單問題",
				1002 => "付款問題",
				1003 => "運送問題",
				_ => "未分類"
			};
		}
	}
}
