using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	/// <summary>
	/// 物流商API
	/// </summary>
	[ApiController]
	[Route("api/sup/[controller]")]
	//[Authorize] // 預設授權
	[AllowAnonymous]
	public class LogisticsController : ControllerBase
	{
		private readonly ILogisticsService _service;
		// 注入新的物流綠界服務
		private readonly IECPayLogisticsService _ecpayLogisticsService;

		public LogisticsController(
			ILogisticsService service,
			IECPayLogisticsService ecpayLogisticsService)
		{
			_service = service;
			_ecpayLogisticsService = ecpayLogisticsService;
		}

		#region 查物流

		/// <summary>
		/// 取得所有物流商
		/// GET /api/sup/logistics
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var list = await _service.GetAllAsync();
				return Ok(ApiResponse<object>.Ok(list, "查詢成功"));
			}
			catch (Exception ex)
			{
				return Ok(ApiResponse<object>.Fail("系統錯誤：" + ex.Message));
			}
		}

		/// <summary>
		/// 取得所有啟用物流商
		/// GET /api/sup/logistics/active
		/// </summary>
		[HttpGet("active")]
		public async Task<IActionResult> GetActiveLogistics()
		{
			try
			{
				var list = await _service.GetActiveAsync();
				return Ok(ApiResponse<object>.Ok(list, "查詢成功"));
			}
			catch (Exception ex)
			{
				return Ok(ApiResponse<object>.Fail("系統錯誤：" + ex.Message));
			}
		}

		/// <summary>
		/// 取得單一物流商
		/// GET /api/sup/logistics/{id}
		/// </summary>
		[HttpGet("{id}")]
		public async Task<IActionResult> Get(int id)
		{
			try
			{
				var log = await _service.GetByIdAsync(id);
				if (log == null)
					return Ok(ApiResponse<object>.Fail("找不到該物流商"));
				return Ok(ApiResponse<object>.Ok(log, "查詢成功"));
			}
			catch (Exception ex)
			{
				return Ok(ApiResponse<object>.Fail("系統錯誤：" + ex.Message));
			}
		}

		#endregion

		#region 物流綠界
		/// <summary>
		/// [前端呼叫] 取得綠界電子地圖跳轉表單 HTML
		/// POST /api/sup/logistics/map
		/// </summary>
		[HttpPost("map")]
		[AllowAnonymous] // 測試時方便，正式上線建議需授權
		public IActionResult GetMapHtml([FromBody] MapRequest request)
		{
			try
			{
				// 定義綠界選完門市後要回傳給你的 API 網址 (必須是外網可存取)
				// 建議從 Config 讀取 Host，這裡先寫死範例
				// 如果您用 ngrok，請換成 ngrok 的 https 網址
				//var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/sup/logistics/map-reply";
				var callbackUrl = "https://nonforming-morbifically-heriberto.ngrok-free.dev/api/sup/logistics/map-reply";
				
				var html = _ecpayLogisticsService.CreateMapForm(
					request.LogisticsSubType,
					request.IsCollection,
					callbackUrl,
					request.Device
				);

				// 回傳 HTML 給前端，前端可以直接渲染或放入 iframe
				return Content(html, "text/html");
			}
			catch (Exception ex)
			{
				return BadRequest($"建立地圖失敗: {ex.Message}");
			}
		}

		/// <summary>
		/// [綠界呼叫] 電子地圖選擇完成後的回傳接收點 (Server-to-Server POST)
		/// POST /api/sup/logistics/map-reply
		/// </summary>
		[HttpPost("map-reply")]
		[AllowAnonymous] // 必須允許匿名，因為是綠界 Server 打過來的
		[Consumes("application/x-www-form-urlencoded")] // 重要：明確指定接收 Form 格式
		public IActionResult OnMapReply([FromForm] IFormCollection formData)
		{
			try
			{
				// 將 FormCollection 轉為 Dictionary 方便處理
				var data = formData.ToDictionary(k => k.Key, v => v.Value.ToString());

				// 取得關鍵門市資訊
				var storeId = data.ContainsKey("CVSStoreID") ? data["CVSStoreID"] : "";
				var storeName = data.ContainsKey("CVSStoreName") ? data["CVSStoreName"] : "";
				var address = data.ContainsKey("CVSAddress") ? data["CVSAddress"] : "";
				var logisticsSubType = data.ContainsKey("LogisticsSubType") ? data["LogisticsSubType"] : "";

				// *重要*：此時使用者的瀏覽器停留在這個 POST 請求上。
				// 您必須回傳一個 302 Redirect，把使用者帶回您的前端網站。
				// 這裡示範帶參數回前端 (實際應用可用 Cache/Session 暫存，避免網址過長或洩漏資訊)

				// 假設您的前端接收頁面是 http://localhost:5173/checkout/callback
				var frontendCallbackUrl = "http://localhost:5173/checkout/callback";
				var redirectUrl = $"{frontendCallbackUrl}?storeId={storeId}&storeName={System.Net.WebUtility.UrlEncode(storeName)}&address={System.Net.WebUtility.UrlEncode(address)}&type={logisticsSubType}";

				return Redirect(redirectUrl);
			}
			catch (Exception ex)
			{
				// log error
				return BadRequest($"處理門市回傳失敗: {ex.Message}");
			}
		}

		// DTO for Map Request
		public class MapRequest
		{
			/// <summary>
			/// 物流子類型 (e.g., UNIMARTC2C, FAMIC2C)
			/// </summary>
			public string LogisticsSubType { get; set; } = "UNIMARTC2C";
			public bool IsCollection { get; set; } = false;
			public int Device { get; set; } = 0; // 0:PC, 1:Mobile
		}
	}

	#endregion

}

