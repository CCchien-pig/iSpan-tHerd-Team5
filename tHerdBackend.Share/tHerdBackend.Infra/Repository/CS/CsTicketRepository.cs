using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.SYS;
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
		private readonly ISysAssetFileRepository _assetRepo; //注入共用圖片模組

		public CsTicketRepository(tHerdDBContext db, ISysAssetFileRepository assetRepo)
		{
			_db = db;
			_assetRepo = assetRepo;

		}

		/// <summary>
		/// 取得所有客服工單清單 JOIN FAQ 分類名稱、轉成 DTO
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
					CategoryId = dto.CategoryId > 0 ? dto.CategoryId : null,
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
			

			// 第一則訊息
			var message = new CsTicketMessage
			{
				TicketId = entity.TicketId,
				SenderType = 1, // 1=客戶
				MessageText = dto.MessageText,
				CreatedDate = DateTime.Now
			};
				_db.CsTicketHistories.Add(history);
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
		/// <summary>
		/// 上傳一張圖片到 Cloudinary / 本地，並回傳 FileId
		/// </summary>
		public async Task<int?> AddAttachmentAsync(IFormFile file, CancellationToken ct = default)
		{
			if (file == null || file.Length == 0)
				return null;

			// 設定上傳的模組代號與子程式代號
			var uploadDto = new AssetFileUploadDto
			{
				ModuleId = "CS",
				ProgId = "Ticket",
				Meta = new List<AssetFileDetailsDto>
		  {
			  new AssetFileDetailsDto
			  {
				  File = file,
				  AltText = "客服工單附件",
				  Caption = "客戶上傳圖片"
			  }
		  }
			};

			var result = await _assetRepo.AddFilesAsync(uploadDto, ct);
			  
			// dynamic 方式取出回傳資料
			var data = result as dynamic;
			var fileId = data?.data?.FirstOrDefault()?.FileId;
			return (int?)fileId;

		}
		public async Task UpdateImgIdAsync(int ticketId, int fileId)
		{
			var ticket = await _db.CsTickets.FirstOrDefaultAsync(t => t.TicketId == ticketId);
			if (ticket == null) return;

			ticket.ImgId = fileId;
			await _db.SaveChangesAsync();
		}
        /// <summary>
        /// /// 取得指定使用者的客服工單清單
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TicketsDto>> GetByUserIdAsync(int userId)
        {
            return await (
                from t in _db.CsTickets
                join c in _db.CsFaqCategories on t.CategoryId equals c.CategoryId into cat
                from c in cat.DefaultIfEmpty()

                    // 🔹 撈使用者第一則留言（SenderType=1）
                let firstMsg = _db.CsTicketMessages
                    .Where(m => m.TicketId == t.TicketId && m.SenderType == 1)
                    .OrderBy(m => m.CreatedDate)
                    .Select(m => m.MessageText)
                    .FirstOrDefault()

                where t.UserId == userId
                orderby t.CreatedDate descending

                select new TicketsDto
                {
                    TicketId = t.TicketId,
                    Subject = t.Subject,
                    CategoryName = c.CategoryName ?? "未分類",
                    // 🔹 狀態轉中文
                    StatusText = t.Status == 0 ? "未處理" :
                                 t.Status == 1 ? "處理中" :
                                 t.Status == 2 ? "已回覆" : "已結案",
                    PriorityText = t.Priority.ToString(),
                    CreatedDate = t.CreatedDate,
                    // 🔹 新增這行：使用者留言文字
                    UserMessage = firstMsg
                }
            ).ToListAsync();
        }
		/// <summary>
		/// 工單詳情
		/// </summary>
		/// <param name="ticketId"></param>
		/// <returns></returns>
        public async Task<TicketsDto?> GetByIdAsync(int ticketId)
        {
            return await (
                from t in _db.CsTickets
                join c in _db.CsFaqCategories on t.CategoryId equals c.CategoryId into cat
                from c in cat.DefaultIfEmpty()
                where t.TicketId == ticketId
                select new TicketsDto
                {
                    TicketId = t.TicketId,
                    Subject = t.Subject,
                    CategoryName = c.CategoryName ?? "未分類",
                    StatusText = t.Status == 0 ? "未處理" :
                                 t.Status == 1 ? "處理中" :
                                 t.Status == 2 ? "已回覆" : "已結案",
                    PriorityText = t.Priority.ToString(),
                    CreatedDate = t.CreatedDate
                }
            ).FirstOrDefaultAsync();
        }
        public async Task AddMessageAsync(int ticketId, string messageText, int senderType)
        {
            var msg = new CsTicketMessage
            {
                TicketId = ticketId,
                SenderType = (byte)senderType,
                MessageText = messageText,
                CreatedDate = DateTime.Now
            };
            _db.CsTicketMessages.Add(msg);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int ticketId, int newStatus)
        {
            var ticket = await _db.CsTickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.Status = newStatus;
                await _db.SaveChangesAsync();
            }
        }



    }
}

