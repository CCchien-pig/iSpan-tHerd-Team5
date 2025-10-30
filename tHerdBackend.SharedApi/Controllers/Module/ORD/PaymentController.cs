using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
    [ApiController]
    [Route("api/ord/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IECPayService _ecpayService;
        private readonly ILogger<PaymentController> _logger;
        private readonly tHerdDBContext _context;

        public PaymentController(
            IECPayService ecpayService,
            ILogger<PaymentController> logger,
            tHerdDBContext context )
        {
            _ecpayService = ecpayService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// 建立綠界付款
        /// </summary>
        [HttpPost("ecpay/create")]
        public IActionResult CreateECPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var formHtml = _ecpayService.CreatePaymentForm(
                    request.OrderId,
                    request.TotalAmount,
                    request.ItemName
                );

                return Content(formHtml, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立綠界付款失敗");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 綠界付款結果通知 (Server to Server)
        /// </summary>
        [HttpPost("ecpay/notify")]
        [AllowAnonymous]
        public async Task<IActionResult> EcpayNotify()
        {
            try
            {
                var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
                var rawBody = string.Join("&", formData.Select(x => $"{x.Key}={x.Value}"));
                _logger.LogInformation("🧾 收到綠界通知: {RawBody}", rawBody);

                if (!_ecpayService.ValidateCheckMacValue(formData))
                {
                    _logger.LogWarning("❌ CheckMacValue 驗證失敗");
                    return BadRequest("0|CheckMacValue驗證失敗");
                }

                var merchantTradeNo = GetValue(formData, "MerchantTradeNo");
                var rtnCode = int.Parse(GetValue(formData, "RtnCode") ?? "0");
                var rtnMsg = GetValue(formData, "RtnMsg");
                var tradeNo = GetValue(formData, "TradeNo");

                // ✅ 查找訂單（根據商店訂單編號）
                var order = await _context.OrdOrders.FirstOrDefaultAsync(o => o.OrderNo == merchantTradeNo);
                if (order == null)
                {
                    _logger.LogWarning("❌ 找不到訂單: MerchantTradeNo={MerchantTradeNo}", merchantTradeNo);
                    return BadRequest("0|找不到訂單");
                }

                // ✅ 更新訂單付款狀態（只在成功時）
                if (rtnCode == 1)
                {
                    order.PaymentStatus = "paid";

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("✅ 更新訂單成功: OrderNo={OrderNo}, TradeNo={TradeNo}", merchantTradeNo, tradeNo);
                    return Ok("1|OK");
                }
                else
                {
                    _logger.LogWarning("⚠️ 綠界通知交易失敗: OrderNo={OrderNo}, RtnMsg={RtnMsg}", merchantTradeNo, rtnMsg);
                    return Ok("1|OK"); // 綠界即使失敗也要回傳 OK，否則會一直重發
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 處理綠界通知異常");
                return BadRequest("0|系統錯誤");
            }
        }

        private string GetValue(Dictionary<string, string> data, string key)
        {
            return data.ContainsKey(key) ? data[key] : null;
        }




        /// <summary>
        /// 解析 decimal?
        /// </summary>
        private decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (decimal.TryParse(value, out var result))
                return result;

            return null;
        }

        /// <summary>
        /// 解析 int?
        /// </summary>
        private int? ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, out var result))
                return result;

            return null;
        }
    }

    /// <summary>
    /// 建立付款請求 DTO
    /// </summary>
    public class CreatePaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public int TotalAmount { get; set; }
        public string ItemName { get; set; } = string.Empty;
    }
}