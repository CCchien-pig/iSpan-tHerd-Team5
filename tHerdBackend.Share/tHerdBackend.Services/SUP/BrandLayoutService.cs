using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.DTOs.SUP.BrandLayout;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.Services.SUP;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.SUP
{
	public class BrandLayoutService : IBrandLayoutService
	{
		private readonly IBrandLayoutRepository _repo;
		private readonly JsonSerializerOptions _jsonOptions = new()
		{
			// 啟用 CamelCase (Props -> props)
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false // 儲存時不用排版，節省空間
		};
		private readonly tHerdDBContext _context; // 【新增】用於資料庫交易
		private readonly IContentService _contentService; // 【使用的通用服務】

		public BrandLayoutService(
			IBrandLayoutRepository repo,
			tHerdDBContext context,
			IContentService contentService)
		{
			_repo = repo;
			_context = context;
			_contentService = contentService;
		}

		// 品牌版面

		#region 品牌版面 - 查詢與CRUD (呼叫 Repository)

		// 查詢
		public Task<IEnumerable<BrandLayoutDto>> GetLayoutsByBrandIdAsync(int brandId)
			=> _repo.GetLayoutsByBrandIdAsync(brandId);

		public async Task<BrandLayoutDto?> GetLayoutByLayoutIdAsync(int layoutId)
			=> await _repo.GetLayoutByLayoutIdAsync(layoutId);

		public Task<BrandLayoutDto?> GetActiveLayoutAsync(int brandId)
			=> _repo.GetActiveLayoutAsync(brandId);

		public async Task<int?> GetActiveLayoutIdAsync(int brandId)
			=> await _repo.GetActiveLayoutIdAsync(brandId);

		// 新增
		public Task<int> CreateLayoutAsync(int brandId, BrandLayoutCreateDto dto)
			=> _repo.AddLayoutAsync(brandId, dto); // 呼叫 Repository 的 AddLayoutAsync

		// 更新
		public Task<bool> UpdateLayoutAsync(int layoutId, BrandLayoutUpdateDto dto)
			=> _repo.UpdateLayoutAsync(layoutId, dto);

		#endregion

		#region 品牌版面 - 狀態管理 (業務邏輯實作) 啟用、軟刪除

		// **業務邏輯：啟用指定 Layout**
		public async Task<bool> ActivateLayoutAsync(int layoutId, int reviserId)
		{
			// 1. 業務判斷：檢查目標 Layout 資訊 (使用 Repository 提供的 DTO 查詢)
			var targetInfo = await _repo.GetActivationInfoByIdAsync(layoutId);

			if (targetInfo == null)
			{
				return false; // 找不到指定 Layout
			}

			var brandId = targetInfo.BrandId;
			var now = DateTime.Now;

			// 2. 業務規則：先停用同一品牌下所有舊版
			if (targetInfo.IsActive == false)
			{
				// 使用 Repository 底層的高效批量停用操作
				await _repo.BulkDeactivateAllActiveByBrandIdAsync(brandId, reviserId, now);
			}

			// 3. 業務規則：啟用指定的 Layout
			// 使用 Repository 底層的高效啟用操作
			var updatedRows = await _repo.BulkActivateLayoutAsync(layoutId, reviserId, now);

			return updatedRows > 0;
		}

		// **業務邏輯：軟刪除 Layout**
		public Task<bool> SoftDeleteLayoutAsync(int layoutId, int reviserId)
		{
			// 直接呼叫 Repository 的高效軟刪除操作
			var now = DateTime.Now;
			return _repo.SoftDeleteLayoutAsync(layoutId, reviserId, now);
		}


		#endregion

		#region 品牌版面 - 驗證 (Validation)

		/// <summary>
		/// 檢查指定品牌的版面版本號是否已存在。
		/// </summary>
		public async Task<bool> VersionExistsAsync(int brandId, string version, int? currentLayoutId)
		{
			// 直接將呼叫轉發給 Repository 層處理
			return await _repo.VersionExistsAsync(brandId, version, currentLayoutId);
		}

		#endregion

		#region  品牌版面 - JSON 處理

		public List<BaseLayoutBlockDto> DeserializeLayout(string layoutJson)
		{
			if (string.IsNullOrWhiteSpace(layoutJson))
				return new List<BaseLayoutBlockDto>();

			// 【核心修正】檢查並移除多餘的跳脫字元
			// 這是因為資料庫/EF Core 傳輸時將標準 JSON 字符串中的 " 轉換成了 \"
			// 這裡我們需要將其反轉，讓 JsonSerializer 能夠正確解析。
			string cleanJson = layoutJson.Replace("\\\"", "\"");

			try
			{					
				// 重新使用 JsonSerializer 進行反序列化
				var blocks = JsonSerializer.Deserialize<List<BaseLayoutBlockDto>>(cleanJson, _jsonOptions)
					?? new List<BaseLayoutBlockDto>();

				// 遍歷所有反序列化後的區塊，並強制賦予一個新的、唯一的 ID。
				foreach (var block in blocks)
				{
					// 使用 Guid 或其他唯一字串生成器確保 ID 的唯一性
					block.Id = Guid.NewGuid().ToString("N").Substring(0, 12);
				}

				// 【核心修正點】過濾掉所有反序列化失敗或為 null 的元素，確保陣列乾淨
				return blocks.Where(b => b != null).ToList();
			}
			catch (JsonException ex)
			{
				// 拋出一個更清晰的異常，以便追蹤
				throw new Exception($"版面設定 JSON 格式錯誤，無法解析。原始錯誤: {ex.Message}。處理後的 JSON 開頭: {layoutJson.Substring(0, Math.Min(layoutJson.Length, 50))}", ex);
			}
		}


		public string SerializeLayout(List<BaseLayoutBlockDto> blocks)
		{
			if (blocks == null || !blocks.Any())
				return "[]";

			// 將 List<BaseLayoutBlockDto> 序列化成 JSON 字串
			string json = JsonSerializer.Serialize(blocks, _jsonOptions);
			
			return json;
		}

		#endregion

		#region 舊資料讀取 (Legacy Data Loading)

		/// <summary>
		/// 處理一個品牌首次進入新版面系統的「初始狀態」。
		/// 【新增實作】獲取舊的 Accordion 內容並轉換為 BaseLayoutBlockDto 列表。
		/// 此方法會在找不到新的 LayoutJson 時被 Controller 呼叫。
		/// </summary>
		public async Task<List<BaseLayoutBlockDto>> GetLegacyAccordionBlocksAsync(int brandId)
		{
			// 1. 呼叫 Repository 獲取已排序的舊資料 DTO 列表
			var legacyContentDtos = await _repo.GetLegacyAccordionContentAsync(brandId);

			if (legacyContentDtos == null || !legacyContentDtos.Any())
			{
				return new List<BaseLayoutBlockDto>();
			}

			// 2. 將舊資料結構 (BrandAccordionContentDto) 手動映射到新的區塊 DTO 結構 (BaseLayoutBlockDto)
			var blocks = legacyContentDtos.Select(dto => new BaseLayoutBlockDto
			{
				// 為舊資料生成一個唯一的 ID，供前端 Vue 使用
				Id = $"legacy-accordion-{dto.ContentId}",
				Type = "Accordion", // 類型固定為 Accordion

				// 將完整的 DTO 作為 Props，前端 Vue可以直接使用
				Props = new AccordionPropsDto
				{
					ContentId = dto.ContentId, // 傳遞 ContentId，雖然在純 JSON 模式下可能不會直接使用
					ContentTitle = dto.ContentTitle,
					Content = dto.Content,
					ImgId = dto.ImgId
				}
			}).ToList();

			return blocks;
		}

		#endregion

		#region 處理所有儲存邏輯

		/// <summary>
		/// 【混合模式】儲存完整的版面配置。
		/// 此方法會啟動一個資料庫交易，將每個區塊的內容儲存到各自的資料表中，
		/// 然後將版面的結構（骨架）儲存到 SUP_BrandLayoutConfig 的 LayoutJson 欄位。
		/// </summary>
		/// <param name="dto">包含完整內容 JSON 的輸入 DTO。</param>
		/// <param name="reviserId">執行操作的使用者 ID。</param>
		/// <returns>最終儲存或更新的 Layout ID。</returns>
		public async Task<int> SaveHybridLayoutAsync(BrandLayoutSaveInputDto dto, int reviserId)
		{
			// 1. 啟動資料庫交易，確保所有操作的原子性
			await using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				// 2. 反序列化前端傳來的、包含完整內容的 JSON
				// 【核心修正點】直接使用 DTO 傳來的列表，不再需要 DeserializeLayout
				var fullBlocks = dto.FullLayoutJson;
				var cleanLayoutItems = new List<object>(); // 用於儲存乾淨的 JSON 骨架

				// 3. 使用 for 迴圈代替 foreach，以獲取索引作為 OrderSeq
				for (int i = 0; i < fullBlocks.Count; i++)
				{
					var block = fullBlocks[i];
					var propsJson = JsonSerializer.Serialize(block.Props);
					int orderSeq = i; // 陣列的索引就是我們的 OrderSeq

					try
					{
						switch (block.Type.ToLower())
						{
							case "accordion":
								var accordionDto = JsonSerializer.Deserialize<BrandAccordionContentDto>(propsJson, _jsonOptions);

								// 【修正】新增 null 檢查
								if (accordionDto == null)
								{
									// 雖然已經有 JSON 錯誤捕獲，但我們還是保留此檢查
									Console.Error.WriteLine($"[JSON ERROR] Accordion DTO is null for block ID: {block.Id}");
									break; // 跳出 switch，不執行 Upsert
								}

								// 【核心修正點 A】在呼叫 Service 前，確保 DTO 包含 FK, Creator/Reviser 和 OrderSeq
								accordionDto.BrandId = dto.BrandId; // 賦予 BrandId
								accordionDto.OrderSeq = orderSeq;   // 賦予 OrderSeq
								accordionDto.Creator = reviserId;   // 賦予 Creator/Reviser ID

								// 呼叫 Service
								var contentId = await _contentService.UpsertContentAsync(accordionDto, dto.BrandId, reviserId, orderSeq);
								cleanLayoutItems.Add(new { type = "Accordion", contentId });
								break;

							case "banner":
								var bannerDto = JsonSerializer.Deserialize<BannerDto>(propsJson, _jsonOptions);

								// 【修正】新增 null 檢查
								if (bannerDto == null)
								{
									// 雖然已經有 JSON 錯誤捕獲，但我們還是保留此檢查
									Console.Error.WriteLine($"[JSON ERROR] Banner DTO is null for block ID: {block.Id}");
									break; // 跳出 switch，不執行 Upsert
								}

								// 1. 【處理業務規則】Banner 必須有 FileUrl 才能儲存
								if (string.IsNullOrWhiteSpace(bannerDto.FileUrl))
								{
									Console.Error.WriteLine($"[Banner Validation Error] BrandId {dto.BrandId}: Banner 區塊缺少 FileUrl，跳過儲存。");
									break; // 跳出 switch，不執行 Upsert
								}

								// 2. 賦予追蹤欄位 (Creator/Reviser)
								bannerDto.Creator = reviserId;
								bannerDto.Reviser = reviserId;

								// 3. 呼叫 Service
								// 由於 Banner 不使用 BrandId/OrderSeq，我們傳遞 0 作為佔位符
								var fileId = await _contentService.UpsertContentAsync(bannerDto, 0, reviserId, 0);

								// 4. 只有 FileId > 0 (成功儲存) 才將其加入骨架
								if (fileId > 0)
								{
									// 【核心修正點】在 cleanLayoutItems 中，同時儲存 contentId 和 linkUrl
									cleanLayoutItems.Add(new
									{
										type = "Banner",
										contentId = fileId,
										linkUrl = bannerDto.LinkUrl // 將 linkUrl 寫入 JSON 骨架
									});
								}
								break;

							case "article":
								var articleDto = JsonSerializer.Deserialize<BrandArticleDto>(propsJson, _jsonOptions);

								// 【修正】新增 null 檢查
								if (articleDto == null)
								{
									// 雖然已經有 JSON 錯誤捕獲，但我們還是保留此檢查
									Console.Error.WriteLine($"[JSON ERROR] Article DTO is null for block ID: {block.Id}");
									break; // 跳出 switch，不執行 Upsert
								}

								// 【核心修正點】為 Article DTO 賦值：BrandId, OrderSeq, Creator/Reviser
								articleDto.BrandId = dto.BrandId; // 賦予 BrandId
								articleDto.OrderSeq = orderSeq;   // 賦予 OrderSeq
								articleDto.Creator = reviserId;   // 賦予 Creator/Reviser ID (Update 時 Service 層會處理 Reviser)

								// 呼叫 Service
								var articleId = await _contentService.UpsertContentAsync(articleDto, dto.BrandId, reviserId, orderSeq);
								cleanLayoutItems.Add(new { type = "Article", contentId = articleId });
								break;
						}
					}
					catch (JsonException ex)
					{
						// 捕獲 JSON 轉換錯誤，記錄到 Console，並跳過此區塊
						Console.Error.WriteLine($"[JSON CONFLICT] 無法解析區塊 {block.Type} (ID: {block.Id}) 的內容。錯誤: {ex.Message}");
						continue; // 跳過此區塊，繼續下一個迴圈
					}
					catch (Exception ex)
					{
						// 捕獲任何其他非預期錯誤，例如 Upsert 邏輯中的 DB 錯誤
						Console.Error.WriteLine($"[CONTENT SAVE ERROR] 儲存區塊 {block.Type} 失敗。錯誤: {ex.Message}");
						continue; // 跳過此區塊
					}
				}

				// 4. 序列化乾淨的、只含 ID 的 JSON 骨架
				string cleanLayoutJson = JsonSerializer.Serialize(cleanLayoutItems);
				int finalLayoutId;

				// 5. 將 JSON 骨架儲存到 SUP_BrandLayoutConfig
				if (dto.ActiveLayoutId.HasValue && dto.ActiveLayoutId.Value > 0)
				{
					// 更新現有的 Layout 紀錄
					await _repo.UpdateLayoutJsonAsync(dto.ActiveLayoutId.Value, cleanLayoutJson, dto.LayoutVersion, reviserId);
					finalLayoutId = dto.ActiveLayoutId.Value;
				}
				else
				{
					// 建立一筆新的 Layout 紀錄
					finalLayoutId = await _repo.CreateLayoutJsonAsync(dto.BrandId, cleanLayoutJson, dto.LayoutVersion, reviserId);
				}

				// 6. 如果所有操作都成功，提交交易
				await transaction.CommitAsync();
				return finalLayoutId;
			}
			catch (Exception)
			{
				// 7. 如果任何一步失敗，回滾所有資料庫操作
				await transaction.RollbackAsync();
				throw; // 向上拋出異常，讓 Controller 知道發生了錯誤
			}
		}

		#endregion
	}
}
