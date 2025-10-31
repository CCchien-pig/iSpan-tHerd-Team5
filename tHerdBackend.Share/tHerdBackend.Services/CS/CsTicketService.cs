using Microsoft.AspNetCore.Http;
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

		public CsTicketService(ICsTicketRepository repo, ISysAssetFileRepository assetRepo)
		{
			_repo = repo;
			_assetRepo = assetRepo; //注入共用圖片模組
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

	}
}
