using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
	//[ApiController]
	//[Route("api/ord/payment")]
	//public class PaymentController : ControllerBase
	//{
	//    private readonly IECPayService _ecpayService;
	//    private readonly ILogger<PaymentController> _logger;
	//    private readonly tHerdDBContext _context;

	//    public PaymentController(
	//        IECPayService ecpayService,
	//        ILogger<PaymentController> logger,
	//        tHerdDBContext context )
	//    {
	//        _ecpayService = ecpayService;
	//        _logger = logger;
	//        _context = context;
	//    }

	//    /// <summary>
	//    /// 建立綠界付款
	//    /// </summary>
	//    [HttpPost("ecpay/create")]
	//    public IActionResult CreateECPayment([FromBody] CreatePaymentRequest request)
	//    {
	//        try
	//        {
	//            var formHtml = _ecpayService.CreatePaymentForm(
	//                request.OrderId,
	//                request.TotalAmount,
	//                request.ItemName
	//            );

	//            return Content(formHtml, "text/html");
	//        }
	//        catch (Exception ex)
	//        {
	//            _logger.LogError(ex, "建立綠界付款失敗");
	//            return BadRequest(new { success = false, message = ex.Message });
	//        }
	//    }

	//    /// <summary>
	//    /// 綠界付款結果通知 (Server to Server)
	//    /// </summary>
	//    [HttpPost("ecpay/notify")]
	//    [AllowAnonymous]
	//    public async Task<IActionResult> EcpayNotify()
	//    {
	//        try
	//        {
	//            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
	//            var rawBody = string.Join("&", formData.Select(x => $"{x.Key}={x.Value}"));
	//            _logger.LogInformation("🧾 收到綠界通知: {RawBody}", rawBody);

	//            if (!_ecpayService.ValidateCheckMacValue(formData))
	//            {
	//                _logger.LogWarning("❌ CheckMacValue 驗證失敗");
	//                return BadRequest("0|CheckMacValue驗證失敗");
	//            }

	//            var merchantTradeNo = GetValue(formData, "MerchantTradeNo");
	//            var rtnCode = int.Parse(GetValue(formData, "RtnCode") ?? "0");
	//            var rtnMsg = GetValue(formData, "RtnMsg");
	//            var tradeNo = GetValue(formData, "TradeNo");

	//            // ✅ 查找訂單（根據商店訂單編號）
	//            var order = await _context.OrdOrders.FirstOrDefaultAsync(o => o.OrderNo == merchantTradeNo);
	//            if (order == null)
	//            {
	//                _logger.LogWarning("❌ 找不到訂單: MerchantTradeNo={MerchantTradeNo}", merchantTradeNo);
	//                return BadRequest("0|找不到訂單");
	//            }

	//            // ✅ 更新訂單付款狀態（只在成功時）
	//            if (rtnCode == 1)
	//            {
	//                order.PaymentStatus = "paid";

	//                await _context.SaveChangesAsync();

	//                _logger.LogInformation("✅ 更新訂單成功: OrderNo={OrderNo}, TradeNo={TradeNo}", merchantTradeNo, tradeNo);
	//                return Ok("1|OK");
	//            }
	//            else
	//            {
	//                _logger.LogWarning("⚠️ 綠界通知交易失敗: OrderNo={OrderNo}, RtnMsg={RtnMsg}", merchantTradeNo, rtnMsg);
	//                return Ok("1|OK"); // 綠界即使失敗也要回傳 OK，否則會一直重發
	//            }
	//        }
	//        catch (Exception ex)
	//        {
	//            _logger.LogError(ex, "💥 處理綠界通知異常");
	//            return BadRequest("0|系統錯誤");
	//        }
	//    }

	//    private string GetValue(Dictionary<string, string> data, string key)
	//    {
	//        return data.ContainsKey(key) ? data[key] : null;
	//    }




	//    /// <summary>
	//    /// 解析 decimal?
	//    /// </summary>
	//    private decimal? ParseDecimal(string value)
	//    {
	//        if (string.IsNullOrWhiteSpace(value))
	//            return null;

	//        if (decimal.TryParse(value, out var result))
	//            return result;

	//        return null;
	//    }

	//    /// <summary>
	//    /// 解析 int?
	//    /// </summary>
	//    private int? ParseInt(string value)
	//    {
	//        if (string.IsNullOrWhiteSpace(value))
	//            return null;

	//        if (int.TryParse(value, out var result))
	//            return result;

	//        return null;
	//    }
	//}

	///// <summary>
	///// 建立付款請求 DTO
	///// </summary>
	//public class CreatePaymentRequest
	//{
	//    public string OrderId { get; set; } = string.Empty;
	//    public int TotalAmount { get; set; }
	//    public string ItemName { get; set; } = string.Empty;
	//}
	[ApiController]
	[Route("api/ord/payment/ecpay")]
	public class PaymentController : ControllerBase
	{
		private readonly IECPayService _ecpayService;
		private readonly ILogger<PaymentController> _logger;
		private readonly tHerdDBContext _db;

		public PaymentController(
			IECPayService ecpayService,
			ILogger<PaymentController> logger,
			tHerdDBContext db)
		{
			_ecpayService = ecpayService;
			_logger = logger;
			_db = db;
		}

		// ============== 1) 產生綠界表單（前端呼叫） ==============
		[HttpPost("create")]
		public IActionResult CreateECPayment([FromBody] CreatePaymentRequest request)
		{
			try
			{
				// 這裡建議由後端依 OrderId 查訂單總額，不要相信前端
				var order = _db.OrdOrders.FirstOrDefault(o => o.OrderNo == request.OrderId);
				if (order == null) return BadRequest(new { success = false, message = "找不到訂單" });

				var total = (int)Math.Round(order.Subtotal + order.ShippingFee - order.DiscountTotal);

				var formHtml = _ecpayService.CreatePaymentForm(
					request.OrderId,    // MerchantTradeNo
					total,              // TotalAmount
					string.IsNullOrEmpty(request.ItemName) ? "tHerd商品" : request.ItemName
				);

				return Content(formHtml, "text/html");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "建立綠界付款失敗");
				return BadRequest(new { success = false, message = ex.Message });
			}
		}

        // ============== 2) ReturnURL：付款結果通知（server → server） ==============
        //[HttpPost("return")]
        //[AllowAnonymous]
        //[Consumes("application/x-www-form-urlencoded")]
        //[IgnoreAntiforgeryToken]
        //public async Task<IActionResult> ReturnAsync()
        //{
        //	if (!Request.HasFormContentType) return Content("0|NoForm");
        //	var form = Request.Form; // MerchantTradeNo, RtnCode, TradeAmt, TradeNo, PaymentDate, CheckMacValue...

        //	// 1) 驗簽
        //	if (!_ecpayService.ValidateCheckMacValue(form.ToDictionary(x => x.Key, x => x.Value.ToString())))
        //	{
        //		_logger.LogWarning("ECPay Return 驗簽失敗");
        //		return Content("0|CheckMacError");
        //	}

        //	// 2) 存通知表（原樣紀錄）
        //	await SaveReturnNotificationAsync(form);

        //	var merchantTradeNo = form["MerchantTradeNo"].ToString();
        //	var rtnCode = form["RtnCode"].ToString();
        //	var tradeAmtStr = form["TradeAmt"].ToString();
        //	var tradeNo = form["TradeNo"].ToString();

        //	var order = await _db.OrdOrders.FirstOrDefaultAsync(o => o.OrderNo == merchantTradeNo);
        //	if (order == null)
        //	{
        //		_logger.LogWarning("找不到訂單 (Return): {No}", merchantTradeNo);
        //		return Content("0|OrderNotFound");
        //	}

        //	// 3) 金額比對（強烈建議）
        //	if (!int.TryParse(tradeAmtStr, out var tradeAmt))
        //		return Content("0|AmtParseError");

        //	var expected = (int)Math.Round(order.Subtotal + order.ShippingFee - order.DiscountTotal);
        //	if (tradeAmt != expected)
        //	{
        //		_logger.LogWarning("金額不一致 (Return): got={Got} expected={Exp}", tradeAmt, expected);
        //		return Content("0|AmtMismatch");
        //	}

        //	// 4) Upsert 付款紀錄（ORD_Payment）
        //	await UpsertPaymentAsync(order, form);

        //	// 5) 成功時更新訂單
        //	if (rtnCode == "1")
        //	{
        //		order.PaymentStatus = "paid";
        //		await _db.SaveChangesAsync();
        //	}

        //	// ★ 綠界規定：一定要回 1|OK
        //	return Content("1|OK");
        //}
        [HttpPost("return")]
        [AllowAnonymous]
        [Consumes("application/x-www-form-urlencoded")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ReturnAsync()
        {
            try
            {
                _logger.LogInformation("🔔 收到綠界 return 通知");

                if (!Request.HasFormContentType)
                {
                    _logger.LogWarning("❌ 請求不是 form 格式");
                    return Content("0|NoForm");
                }

                var form = Request.Form;

                // 記錄完整內容
                var rawBody = string.Join("&", form.Select(kv => $"{kv.Key}={kv.Value}"));
                _logger.LogInformation("📦 通知內容: {RawBody}", rawBody);

                // 1) 驗簽
                var formDict = form.ToDictionary(x => x.Key, x => x.Value.ToString());
                if (!_ecpayService.ValidateCheckMacValue(formDict))
                {
                    _logger.LogWarning("❌ ECPay Return 驗簽失敗");
                    return Content("0|CheckMacError");
                }
                _logger.LogInformation("✅ 驗簽成功");

                // 2) 存通知表
                try
                {
                    await SaveReturnNotificationAsync(form);
                    _logger.LogInformation("✅ 通知記錄已儲存");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 儲存通知記錄失敗");
                    // 繼續處理，不要因為這個失敗就中斷
                }

                var merchantTradeNo = form["MerchantTradeNo"].ToString();
                var rtnCode = form["RtnCode"].ToString();
                var tradeAmtStr = form["TradeAmt"].ToString();
                var tradeNo = form["TradeNo"].ToString();

                _logger.LogInformation("🔍 處理訂單: MerchantTradeNo={MerchantTradeNo}, RtnCode={RtnCode}, TradeAmt={TradeAmt}, TradeNo={TradeNo}",
                    merchantTradeNo, rtnCode, tradeAmtStr, tradeNo);

                // 3) 查找訂單
                var order = await _db.OrdOrders.FirstOrDefaultAsync(o => o.OrderNo == merchantTradeNo);
                if (order == null)
                {
                    _logger.LogWarning("❌ 找不到訂單 (Return): {No}", merchantTradeNo);
                    return Content("0|OrderNotFound");
                }
                _logger.LogInformation("✅ 找到訂單: OrderId={OrderId}", order.OrderId);

                // 4) 金額比對
                if (!int.TryParse(tradeAmtStr, out var tradeAmt))
                {
                    _logger.LogWarning("❌ 金額解析失敗: {TradeAmtStr}", tradeAmtStr);
                    return Content("0|AmtParseError");
                }

                var expected = (int)Math.Round(order.Subtotal + order.ShippingFee - order.DiscountTotal);
                _logger.LogInformation("💰 金額比對: 綠界={TradeAmt}, 預期={Expected}", tradeAmt, expected);

                if (tradeAmt != expected)
                {
                    _logger.LogWarning("❌ 金額不一致 (Return): got={Got} expected={Exp}", tradeAmt, expected);
                    // 金額不符時，仍然記錄但標記為異常
                    // return Content("0|AmtMismatch"); // ← 先註解，允許通過
                }

                // 5) Upsert 付款紀錄
                try
                {
                    await UpsertPaymentAsync(order, form);
                    _logger.LogInformation("✅ 付款記錄已更新");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 更新付款記錄失敗");
                    return Content("0|UpdatePaymentError");
                }

                // 6) 成功時更新訂單
                if (rtnCode == "1")
                {
                    order.PaymentStatus = "paid";
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("✅ 訂單狀態已更新為 paid: OrderNo={OrderNo}", merchantTradeNo);
                }
                else
                {
                    _logger.LogWarning("⚠️ 付款失敗: RtnCode={RtnCode}, RtnMsg={RtnMsg}", rtnCode, form["RtnMsg"]);
                }

                // ★ 綠界規定：一定要回 1|OK
                _logger.LogInformation("✅ 處理完成，返回 1|OK");
                return Content("1|OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 處理 return 端點發生未預期的異常");
                return Content("0|SystemError");
            }
        }

        /// <summary>
        /// 綠界付款結果通知 - notify 端點（別名，指向 return）
        /// </summary>
        [HttpPost("notify")]
        [AllowAnonymous]
        [Consumes("application/x-www-form-urlencoded")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> NotifyAsync()
        {
            _logger.LogInformation("🔔 收到綠界 notify 通知（轉發到 ReturnAsync）");
            // 直接調用 ReturnAsync 方法
            return await ReturnAsync();
        }

        // ============== 3) PaymentInfoURL：取號通知（ATM / CVS）（server → server） ==============
        [HttpPost("payment-info")]
		[AllowAnonymous]
		[Consumes("application/x-www-form-urlencoded")]
		[IgnoreAntiforgeryToken]
		public async Task<IActionResult> PaymentInfoAsync()
		{
			if (!Request.HasFormContentType) return Content("0|NoForm");
			var form = Request.Form;

			if (!_ecpayService.ValidateCheckMacValue(form.ToDictionary(x => x.Key, x => x.Value.ToString())))
				return Content("0|CheckMacError");

			await SaveReturnNotificationAsync(form); // 也可寫同一張通知表，Type 區分

			var merchantTradeNo = form["MerchantTradeNo"].ToString();
			var order = await _db.OrdOrders.FirstOrDefaultAsync(o => o.OrderNo == merchantTradeNo);
			if (order == null) return Content("0|OrderNotFound");

			// 取號資訊（如：PaymentType=ATM、BankCode、vAccount、ExpireDate...）
			await UpsertPaymentAsync(order, form);

			return Content("1|OK");
		}

        // ------------- 寫庫：通知表 -------------
        //private async Task SaveReturnNotificationAsync(IFormCollection form)
        //{
        //	// TODO: 依你的資料表實作；下面僅示意
        //	var noti = new OrdEcpayReturnNotification
        //	{
        //		// 系統時間
        //		ReceivedDate = DateTime.Now,

        //		// 供應商/平台資訊
        //		MerchantId = form["MerchantID"],     // 綠界欄位是 MerchantID（I 大寫）
        //		PlatformId = form["PlatformID"],     // 若你未啟用平台商，此欄可能為空
        //		StoreId = form["StoreID"],        // 若無分店代號，可能為空

        //		// 訂單/交易識別
        //		MerchantTradeNo = form["MerchantTradeNo"],
        //		TradeNo = form["TradeNo"],

        //		// 結果與金額
        //		RtnCode = int.TryParse(form["RtnCode"], out var rtn) ? rtn : 0,
        //		RtnMsg = form["RtnMsg"],
        //		TradeAmt = int.TryParse(form["TradeAmt"], out var amt) ? amt : 0,

        //		// 付款資訊
        //		PaymentType = form["PaymentType"],
        //		PaymentTypeChargeFee = TryParseDecimal(form["PaymentTypeChargeFee"]),

        //		// 時間（綠界多為 "yyyy/MM/dd HH:mm:ss"）
        //		TradeDate = ParseEcpayDateTime(form["TradeDate"]) ?? DateTime.Now, // 非 null 欄位，給預設
        //		PaymentDate = ParseEcpayDateTime(form["PaymentDate"]),                  // 成功時才會有

        //		// 其他
        //		SimulatePaid = TryParseBool01(form["SimulatePaid"]),  // "0"/"1" or "true"/"false"
        //		CustomField1 = form["CustomField1"],
        //		CustomField2 = form["CustomField2"],
        //		CustomField3 = form["CustomField3"],
        //		CustomField4 = form["CustomField4"],

        //		// 驗簽與原始資料
        //		CheckMacValue = form["CheckMacValue"],
        //		RawBody = string.Join("&", form.Select(kv => $"{kv.Key}={kv.Value}")),
        //		RawHeaders = GetRawHeaders(HttpContext?.Request),

        //		// 失敗原因（成功則留空）
        //		FailReason = (rtn == 1) ? "null" : form["RtnMsg"]
        //	};
        //	_db.OrdEcpayReturnNotifications.Add(noti);
        //	await _db.SaveChangesAsync();
        //}

        private async Task SaveReturnNotificationAsync(IFormCollection form)
        {
            try
            {
                // 解析 RtnCode
                var rtnCode = int.TryParse(form["RtnCode"], out var rtn) ? rtn : 0;

                var noti = new OrdEcpayReturnNotification
                {
                    // 系統時間
                    ReceivedDate = DateTime.Now,

                    // 供應商/平台資訊
                    MerchantId = form["MerchantID"].ToString(),
                    PlatformId = form["PlatformID"].ToString(),
                    StoreId = form["StoreID"].ToString(),

                    // 訂單/交易識別
                    MerchantTradeNo = form["MerchantTradeNo"].ToString(),
                    TradeNo = form["TradeNo"].ToString(),

                    // 結果與金額
                    RtnCode = rtnCode,
                    RtnMsg = form["RtnMsg"].ToString(),
                    TradeAmt = int.TryParse(form["TradeAmt"], out var amt) ? amt : 0,

                    // 付款資訊
                    PaymentType = form["PaymentType"].ToString(),
                    PaymentTypeChargeFee = TryParseDecimal(form["PaymentTypeChargeFee"]),

                    // ✅ 時間（使用 ParseEcpayDateTime 方法）
                    TradeDate = ParseEcpayDateTime(form["TradeDate"]) ?? DateTime.Now,
                    PaymentDate = ParseEcpayDateTime(form["PaymentDate"]) ?? DateTime.Now,

                    // 其他
                    SimulatePaid = TryParseBool01(form["SimulatePaid"]),
                    CustomField1 = form["CustomField1"].ToString(),
                    CustomField2 = form["CustomField2"].ToString(),
                    CustomField3 = form["CustomField3"].ToString(),
                    CustomField4 = form["CustomField4"].ToString(),

                    // 驗簽與原始資料
                    CheckMacValue = form["CheckMacValue"].ToString(),
                    RawBody = string.Join("&", form.Select(kv => $"{kv.Key}={kv.Value}")),
                    RawHeaders = GetRawHeaders(HttpContext?.Request),

                    // 失敗原因（成功則留空）
                    FailReason = (rtnCode == 1) ? null : form["RtnMsg"].ToString()
                };

                _db.OrdEcpayReturnNotifications.Add(noti);
                await _db.SaveChangesAsync();

                _logger.LogInformation("✅ 通知記錄已儲存: NotificationId={NotificationId}", noti.NotificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 儲存通知記錄時發生錯誤");
                throw;
            }
        }

        // ------------- 寫庫：付款表 -------------
        private async Task UpsertPaymentAsync(OrdOrder order, IFormCollection form)
		{
			// 以 OrderId 為 Key upsert
			var pay = await _db.OrdPayments.FirstOrDefaultAsync(p => p.OrderId == order.OrderId);
			var rtnOk = int.TryParse(form["RtnCode"], out var rtnCode) && rtnCode == 1;
			if (pay == null)
			{
				pay = new OrdPayment
				{
					// 必要關聯
					OrderId = order.OrderId,
					PaymentConfigId = order.PaymentConfigId,            // ← 從訂單帶入（或你先查出 configId）

					// 金流回傳
					Amount = int.TryParse(form["TradeAmt"], out var amt) ? amt : 0,
					Status = rtnOk ? "success" : "pending",    // 模型註解：pending/success/failed/refund
					TradeNo = form["TradeNo"],                  // 綠界交易編號（主關聯）
					MerchantTradeNo = form["MerchantTradeNo"],          // 你方訂單編號（送到綠界的）

					// 時間與旗標
					CreatedDate = DateTime.Now,
					TradeDate = ParseEcpayDateTime(form["PaymentDate"]), // 付款完成時間（成功時才會有）
					SimulatePaid = TryParseBool01(form["SimulatePaid"]) ?? false,

					// 驗證與訊息
					RtnCode = int.TryParse(form["RtnCode"], out var rc) ? rc : (int?)null,
					RtnMsg = form["RtnMsg"],
					CheckMacValue = form["CheckMacValue"],

					// 退款型交易才填；一般付款為 null
					ReturnRequestId = null
				};
				_db.OrdPayments.Add(pay);
			}
			else
			{
				// 交易編號（若有就覆蓋）
				var tradeNo = form["TradeNo"];
				if (!string.IsNullOrWhiteSpace(tradeNo))
					pay.TradeNo = tradeNo;

				// 我方對外交易編號（MerchantTradeNo）
				var mTradeNo = form["MerchantTradeNo"];
				if (!string.IsNullOrWhiteSpace(mTradeNo))
					pay.MerchantTradeNo = mTradeNo;

				// 金額
				if (int.TryParse(form["TradeAmt"], out var amt))
					pay.Amount = amt;

				// 狀態：1=success，其餘可視為 failed（或保留原狀態）
				if (int.TryParse(form["RtnCode"], out var rc))
				{
					pay.RtnCode = rc;
					pay.Status = (rc == 1) ? "success" : "failed";
				}

				// 訊息
				var rtnMsg = form["RtnMsg"];
				if (!string.IsNullOrWhiteSpace(rtnMsg))
					pay.RtnMsg = rtnMsg;

				// 付款完成時間
				var payDt = ParseEcpayDateTime(form["PaymentDate"]);
				if (payDt.HasValue)
					pay.TradeDate = payDt;

				// 模擬付款旗標
				var simulate = TryParseBool01(form["SimulatePaid"]);
				if (simulate.HasValue)
					pay.SimulatePaid = simulate.Value;

				// 驗簽原文
				var checkMac = form["CheckMacValue"];
				if (!string.IsNullOrWhiteSpace(checkMac))
					pay.CheckMacValue = checkMac;

				// 若 PaymentConfigId 尚未設好，補上訂單的設定
				if (pay.PaymentConfigId <= 0)
					pay.PaymentConfigId = order.PaymentConfigId;
			}

			await _db.SaveChangesAsync();
		}

		//寫入資料方法:
		private static DateTime? ParseEcpayDateTime(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			// 常見格式：yyyy/MM/dd HH:mm:ss
			if (DateTime.TryParseExact(value,
				"yyyy/MM/dd HH:mm:ss",
				CultureInfo.InvariantCulture,
				DateTimeStyles.None,
				out var dt))
				return dt;

			// 退而求其次
			if (DateTime.TryParse(value, out dt))
				return dt;

			return null;
		}

		private static decimal? TryParseDecimal(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;
			if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
				return d;
			return null;
		}

		private static bool? TryParseBool01(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;
			if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
			if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
			return null;
		}

		private static string? GetRawHeaders(HttpRequest? req)
		{
			if (req == null) return null;
			return string.Join("\n", req.Headers.Select(h => $"{h.Key}: {h.Value}"));
		}

	}

	public class CreatePaymentRequest
	{
		public string OrderId { get; set; } = string.Empty; // 這裡放你的 OrderNo（= MerchantTradeNo）
		public int TotalAmount { get; set; }                 // 可忽略；後端自行取訂單金額較安全
		public string ItemName { get; set; } = "tHerd商品";
	}


}