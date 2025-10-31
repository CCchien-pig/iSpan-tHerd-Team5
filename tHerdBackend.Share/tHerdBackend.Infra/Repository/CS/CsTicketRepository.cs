using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Repositories.Interfaces.CS;

namespace tHerdBackend.Infra.Repositories.CS
{
	/// <summary>
	/// Repository 層：負責資料庫取出、寫入、映射成 DTO
	/// </summary>
	public class CsTicketRepository : ICsTicketRepository
	{
		private readonly tHerdDBContext _db;

		public CsTicketRepository(tHerdDBContext db)
		{
			_db = db;
		}

		/// <summary>
		/// 取得所有客服工單清單（含分類名稱）
		/// </summary>
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
					t.CreatedDate,
					t.AssigneeId
				}
			).AsNoTracking().ToListAsync();

			// 在 C# 層做 switch 對應文字
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

		/// <summary>
		/// 建立新客服工單（同時寫入歷程與第一則訊息）
		/// </summary>
		public async Task<int> CreateAsync(TicketIn dto)
		{
			try
			{
				// 主表
				var entity = new CsTicket
			{
				UserId = dto.UserId,
				CategoryId = dto.CategoryId == 0 ? null : dto.CategoryId,
				Subject = dto.Subject,
				Status = 1, // 待處理
				Priority = dto.Priority,
				CreatedDate = DateTime.Now
			};

			_db.CsTickets.Add(entity);
			await _db.SaveChangesAsync();

			// 歷程紀錄
			var history = new CsTicketHistory
			{
				TicketId = entity.TicketId,
				Action = "建立",
				FromAssigneeId = null,
				ToAssigneeId = null,
				OldStatus = null,
				NewStatus = 1,
				Note = "使用者建立工單",
				ChangedBy = dto.UserId,
				ChangedDate = DateTime.Now
			};
			_db.CsTicketHistories.Add(history);

			// 第一則訊息
			var message = new CsTicketMessage
			{
				TicketId = entity.TicketId,
				SenderType = 1, // 1=客戶
				MessageText = dto.MessageText,
				CreatedDate = DateTime.Now
			};
			_db.CsTicketMessages.Add(message);

			await _db.SaveChangesAsync();
			return entity.TicketId;
		}
    catch (Exception ex)
    {
        // 在除錯輸出完整錯誤
        Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
        throw;
    }
}

/// <summary>
/// 新增工單歷程（轉單、狀態變更等）
/// </summary>
public async Task AddHistoryAsync(int ticketId, string action, int? fromAssigneeId, int? toAssigneeId,
										  byte? oldStatus, byte? newStatus, string note, int changedBy)
		{
			var history = new CsTicketHistory
			{
				TicketId = ticketId,
				Action = action,
				FromAssigneeId = fromAssigneeId,
				ToAssigneeId = toAssigneeId,
				OldStatus = oldStatus,
				NewStatus = newStatus,
				Note = note,
				ChangedBy = changedBy,
				ChangedDate = DateTime.Now
			};

			_db.CsTicketHistories.Add(history);
			await _db.SaveChangesAsync();
		}

		/// <summary>
		/// 新增工單訊息（客服或客戶回覆）
		/// </summary>
		public async Task AddMessageAsync(int ticketId, byte senderType, string messageText, string? attachmentUrl = null)
		{
			var msg = new CsTicketMessage
			{
				TicketId = ticketId,
				SenderType = senderType,
				MessageText = messageText,
				AttachmentUrl = attachmentUrl,
				CreatedDate = DateTime.Now
			};

			_db.CsTicketMessages.Add(msg);
			await _db.SaveChangesAsync();
		}
	}
}
