using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Dynamic;
using System.Text.Json;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.Repositories.Interfaces.CS;

namespace tHerdBackend.Services.CS
{
	/// <summary>客服工單業務邏輯層</summary>
	public class CsTicketService : ICsTicketService
	{
		private readonly ICsTicketRepository _repo;
		private readonly ISysAssetFileRepository _assetRepo;
        private readonly IEmailSender _email;
        public CsTicketService(ICsTicketRepository repo, ISysAssetFileRepository assetRepo, IEmailSender email)
        {
			_repo = repo;
			_assetRepo = assetRepo; //注入共用圖片模組
            _email = email;

        }

		/// <summary>取得全部工單清單</summary>
		public async Task<IEnumerable<TicketsDto>> GetAllAsync()
		{
			return await _repo.GetAllAsync();
		}

		/// <summary>建立新工單，回傳 TicketId</summary>
		/// <summary>建立新工單（可附一張圖片）</summary>
		public async Task<int> CreateAsync(TicketIn input, IFormFile? image = null)
		{
			int? fileId = null;

			// ✅ 若有上傳圖片，就呼叫共用模組上傳
			if (image != null && image.Length > 0)
			{
				var uploadDto = new AssetFileUploadDto
				{
					ModuleId = "CS",
					ProgId = "Ticket",
					Meta = new List<AssetFileDetailsDto>
			{
				new AssetFileDetailsDto
				{
					File = image,
					AltText = "客服工單附件",
					Caption = "客戶上傳圖片"
				}
			}
				};

				var result = await _assetRepo.AddFilesAsync(uploadDto);
				string json = JsonSerializer.Serialize(result);
				dynamic data = JsonSerializer.Deserialize<ExpandoObject>(json);

				JsonElement dataElement = (JsonElement)data.data;
				var first = dataElement.EnumerateArray().FirstOrDefault();
				fileId = (int?)first.GetProperty("FileId").GetInt64();
			}

			// ✅ 呼叫原本 Repository 建立工單
			var ticketId = await _repo.CreateAsync(input);

			// ✅ 如果有圖片，更新工單的 ImgId 欄位
			if (fileId.HasValue)
			{
				await _repo.UpdateImgIdAsync(ticketId, fileId.Value);
			}

			return ticketId;
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
		/// <summary>取得某會員工單清單</summary>
		public async Task<IEnumerable<TicketsDto>> GetByUserIdAsync(int userId)
		{
			return await _repo.GetByUserIdAsync(userId);
		}
        /// <summary>客服回覆（儲存留言 + 更新狀態 + 寄信）</summary>
        public async Task AddReplyAsync(int ticketId, string messageText)
        {
            // ✅ 1️⃣ 新增客服留言
            await _repo.AddMessageAsync(ticketId, messageText, senderType: 2); // 2 = 客服

            // ✅ 2️⃣ 更新工單狀態為已回覆
            await _repo.UpdateStatusAsync(ticketId, 2);

            // ✅ 3️⃣ 撈取工單資料（含 Email）
            var ticket = await _repo.GetByIdAsync(ticketId);
            if (ticket == null || string.IsNullOrEmpty(ticket.Email))
                return; // 若沒有信箱就不寄

            // ✅ 4️⃣ 組信件內容
            var subject = $"tHerd 客服中心 - 您的問題已有回覆 (#{ticket.TicketId})";
            var html = $@"
<p>親愛的會員您好：</p>
<p>您提交的問題「<b>{ticket.Subject}</b>」已有回覆：</p>
<div style='background:#f8f9fa;padding:1rem;border-radius:8px;border:1px solid #ddd;'>
    {messageText}
</div>
<p>如需進一步協助，請直接回覆此信件，我們將盡快為您服務。</p>
<p style='color:#888;font-size:0.9em'>tHerd 客服中心 敬上</p>";

            // ✅ 5️⃣ 寄信
            await _email.SendEmailAsync(ticket.Email, subject, html);
        }
    }
}

