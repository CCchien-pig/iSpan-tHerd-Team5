using System.Diagnostics;
using System.Text.Json;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.DTOs.SUP.BrandLayoutBlocks;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.Services.SUP;

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

		public BrandLayoutService(IBrandLayoutRepository repo)
		{
			_repo = repo;
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

			try
			{
				// 【核心修正】檢查並移除多餘的跳脫字元
				// 這是因為資料庫/EF Core 傳輸時將標準 JSON 字符串中的 " 轉換成了 \"
				// 這裡我們需要將其反轉，讓 JsonSerializer 能夠正確解析。
				string cleanJson = layoutJson.Replace("\\\"", "\"");

				// 如果你的資料庫儲存是 [{\"id\":...}] 
				// 則需要將開頭的 [\{ 換成 [{
				// 但通常 Replace("\\\"", "\"") 已經足夠修正 JSON 內容。

				// 測試：如果你的 JSON 仍然以 [\{ 開頭 (可能是\\被轉義兩次)，你需要更強硬的替換
				if (cleanJson.StartsWith("[\\{") || cleanJson.StartsWith("[\\{"))
				{
					// 假設是資料庫傳輸的雙層跳脫問題，例如 [\" 的問題
					cleanJson = layoutJson.Replace("\\", ""); // 暴力移除所有 \
				}

				// 由於我們不能使用暴力移除，我們採用最精確的修正:
				cleanJson = layoutJson.Replace("\\\"", "\"");

				// 確保陣列開始標記沒有被錯誤處理 (如果 JSON 字串被包在額外的引號中)

				// 重新使用 JsonSerializer 進行反序列化
				var blocks = JsonSerializer.Deserialize<List<BaseLayoutBlockDto>>(cleanJson, _jsonOptions)
					?? new List<BaseLayoutBlockDto>();

				// 遍歷所有反序列化後的區塊，並強制賦予一個新的、唯一的 ID。
				// 這可以解決前端因重複 key 導致的所有連動問題。
				foreach (var block in blocks)
				{
					// 使用 Guid 或其他唯一字串生成器確保 ID 的唯一性
					block.Id = Guid.NewGuid().ToString("N").Substring(0, 12);
				}

				return blocks;
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

			// 注意：EF Core/SQL Server 在儲存字串時，如果 JSON 內包含雙引號，
			// 則 ORM/DB Provider 會自動處理跳脫。
			// 所以這裡不需要手動將 " 替換為 \"，直接回傳標準 JSON 字串即可。
			return json;
		}

		#endregion

	}
}
