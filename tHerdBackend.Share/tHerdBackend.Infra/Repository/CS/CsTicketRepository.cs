using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Repositories.Interfaces.CS;

namespace tHerdBackend.Infra.Repositories.CS
{
	/// <summary>
	/// Repository 層：只負責資料庫取出與映射成 DTO
	/// </summary>
	public class CsTicketRepository : ICsTicketRepository
	{
		private readonly tHerdDBContext _db;

		public CsTicketRepository(tHerdDBContext db)
		{
			_db = db;
		}

		public async Task<IEnumerable<TicketsDto>> GetAllAsync()
		{
			var list = await (
				from t in _db.CsTickets
				join c in _db.CsFaqCategories on t.CategoryId equals c.CategoryId into cat
				from c in cat.DefaultIfEmpty()
				orderby t.CreatedDate descending
				select new
				{
					t.TicketId,
					t.Subject,
					CategoryName = c.CategoryName,
					t.Status,
					t.Priority,
					t.CreatedDate
				}
			).AsNoTracking().ToListAsync();

			// 這裡才做 switch（在 C# 層）
			return list.Select(x => new TicketsDto
			{
				TicketId = x.TicketId,
				Subject = x.Subject,
				CategoryName = x.CategoryName ?? "未分類",
				StatusText = x.Status switch
				{
					1 => "待處理",
					2 => "處理中",
					3 => "已回覆",
					4 => "已結案",
					_ => "未知"
				},
				PriorityText = x.Priority switch
				{
					1 => "高",
					2 => "中",
					3 => "低",
					_ => "中"
				},
				CreatedDate = x.CreatedDate
			});
		}




		public async Task<int> CreateAsync(TicketIn dto)
		{
			var entity = new CsTicket
			{
				UserId = dto.UserId,
				//0 轉成 null，避免違反外鍵 FK_Ticket_CategoryId
				CategoryId = dto.CategoryId == 0 ? null : dto.CategoryId,
				Subject = dto.Subject,
				Status = 0,
				Priority = dto.Priority,
				CreatedDate = DateTime.Now
			};

			_db.CsTickets.Add(entity);
			await _db.SaveChangesAsync();
			return entity.TicketId; // 回傳新ID
		}

	} 
}
