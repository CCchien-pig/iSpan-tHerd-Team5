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
	/// 客服工單 Repository：負責資料庫 CRUD 與資料映射（DTO）
	/// </summary>
	public class CsTicketRepository : ICsTicketRepository
	{
		private readonly tHerdDBContext _db;
		private readonly ISysAssetFileRepository _assetRepo; // 共用圖片模組（上傳附件）

		public CsTicketRepository(tHerdDBContext db, ISysAssetFileRepository assetRepo)
		{
			_db = db;
			_assetRepo = assetRepo;
		}

		// =====================================================
		// 🟩 1️⃣ 後台：取得全部客服工單清單
		// =====================================================
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
					t.UserId,
					t.Email,
					t.Subject,
					CategoryName = c.CategoryName,
					t.Status,
					t.Priority,
					t.CreatedDate
				}
			).AsNoTracking().ToListAsync();

			// 將狀態、優先順序轉成中文描述
			return list.Select(x => new TicketsDto
			{
				TicketId = x.TicketId,
				UserId = x.UserId,
				Email = x.Email,
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

		// =====================================================
		// 🟩 2️⃣ 前台：建立新客服工單（含 Email / 圖片）
		// =====================================================
		public async Task<int> CreateAsync(TicketIn dto)
		{
			try
			{
				// 主表：CS_Ticket
				var entity = new CsTicket
				{
					UserId = dto.UserId,
					CategoryId = dto.CategoryId > 0 ? dto.CategoryId : null,
					Subject = dto.Subject,
					Email = dto.Email,
					Status = 1, // 1 = 待處理
					Priority = dto.Priority,
					CreatedDate = DateTime.Now
				};

				_db.CsTickets.Add(entity);
				await _db.SaveChangesAsync();

				// 第一筆歷程紀錄：使用者建立工單
				var history = new CsTicketHistory
				{
					TicketId = entity.TicketId,
					Action = "建立",
					OldStatus = null,
					NewStatus = 1,
					Note = "使用者建立工單",
					ChangedBy = dto.UserId,
					ChangedDate = DateTime.Now
				};
				_db.CsTicketHistories.Add(history);

				// 第一則訊息：使用者的提問內容
				var message = new CsTicketMessage
				{
					TicketId = entity.TicketId,
					SenderType = 1, // 1 = 客戶
					MessageText = dto.MessageText,
					CreatedDate = DateTime.Now
				};
				_db.CsTicketMessages.Add(message);

				await _db.SaveChangesAsync();
				return entity.TicketId;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
				throw;
			}
		}

		// =====================================================
		// 🟩 3️⃣ 後台：取得單筆工單詳情（含留言 + Email）
		// =====================================================
		public async Task<TicketOut?> GetByIdAsync(int ticketId)
		{
			return await (
				from t in _db.CsTickets
				join c in _db.CsFaqCategories on t.CategoryId equals c.CategoryId into cat
				from c in cat.DefaultIfEmpty()
				where t.TicketId == ticketId
				select new TicketOut
				{
					TicketId = t.TicketId,
					UserId = t.UserId,   // ✅ 顯示會員編號
					Subject = t.Subject,
					CategoryName = c.CategoryName ?? "未分類",
					StatusText = t.Status == 1 ? "待處理" :
								 t.Status == 2 ? "處理中" :
								 t.Status == 3 ? "已回覆" : "已結案",
					PriorityText = t.Priority.ToString(),
					CreatedDate = t.CreatedDate,
					Email = t.Email,     // ✅ 顯示信箱
										 // ✅ 顯示使用者留言
					UserMessage = _db.CsTicketMessages
						.Where(m => m.TicketId == t.TicketId && m.SenderType == 1)
						.OrderBy(m => m.CreatedDate)
						.Select(m => m.MessageText)
						.FirstOrDefault()
				}
			).AsNoTracking().FirstOrDefaultAsync();
		}


		// =====================================================
		// 🟩 4️⃣ 前台：取得該會員的全部工單清單
		// =====================================================
		public async Task<IEnumerable<TicketsDto>> GetByUserIdAsync(int userId)
		{
			return await (
				from t in _db.CsTickets
				join c in _db.CsFaqCategories on t.CategoryId equals c.CategoryId into cat
				from c in cat.DefaultIfEmpty()
				where t.UserId == userId
				orderby t.CreatedDate descending
				select new TicketsDto
				{
					TicketId = t.TicketId,
					Subject = t.Subject,
					CategoryName = c.CategoryName ?? "未分類",
					StatusText = t.Status == 0 ? "未處理" :
								 t.Status == 1 ? "處理中" :
								 t.Status == 2 ? "已回覆" : "已結案",
					PriorityText = t.Priority.ToString(),
					CreatedDate = t.CreatedDate,
					// 抓使用者的第一則留言
					UserMessage = _db.CsTicketMessages
						.Where(m => m.TicketId == t.TicketId && m.SenderType == 1)
						.OrderBy(m => m.CreatedDate)
						.Select(m => m.MessageText)
						.FirstOrDefault()
				}
			).ToListAsync();
		}

		// =====================================================
		// 🟩 5️⃣ 新增歷程紀錄（轉單、狀態變更）
		// =====================================================
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

		// =====================================================
		// 🟩 6️⃣ 新增一則訊息（客服或客戶留言）
		// =====================================================
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


		// =====================================================
		// 🟩 7️⃣ 更新工單狀態（例如已回覆 / 結案）
		// =====================================================
		public async Task UpdateStatusAsync(int ticketId, int newStatus)
		{
			var ticket = await _db.CsTickets.FindAsync(ticketId);
			if (ticket == null) return;

			ticket.Status = newStatus;
			ticket.RevisedDate = DateTime.Now;
			await _db.SaveChangesAsync();
		}

		// =====================================================
		// 8上傳一張圖片並回傳 FileId（前台附檔）
		// =====================================================
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

			// 呼叫共用模組上傳
			var result = await _assetRepo.AddFilesAsync(uploadDto, ct);

			// dynamic 方式取出回傳資料中的 FileId
			var data = result as dynamic;
			var fileId = data?.data?.FirstOrDefault()?.FileId;
			return (int?)fileId;
		}

		// =====================================================
		// 9 更新附件圖片的 FileId（有上傳時用）
		// =====================================================
		public async Task UpdateImgIdAsync(int ticketId, int fileId)
		{
			var ticket = await _db.CsTickets.FirstOrDefaultAsync(t => t.TicketId == ticketId);
			if (ticket == null) return;

			ticket.ImgId = fileId;
			await _db.SaveChangesAsync();
		}


		// =====================================================
		// 10 取得單筆工單的留言清單（前台、後台共用）
		// =====================================================
		public async Task<IEnumerable<TicketMessageDto>> GetMessagesAsync(int ticketId)
		{
			return await (
				from m in _db.CsTicketMessages
				where m.TicketId == ticketId
				orderby m.CreatedDate
				select new TicketMessageDto
				{
					MessageId = m.MessageId,
					SenderType = m.SenderType,
					MessageText = m.MessageText,
					CreatedDate = m.CreatedDate
				}
			).ToListAsync();
		}
	}
}
