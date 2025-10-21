using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
    [ApiController]
    [Route("api/ord/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IECPayService _ecpayService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IECPayService ecpayService,
            ILogger<PaymentController> logger)
        {
            _ecpayService = ecpayService;
            _logger = logger;
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

                if (!_ecpayService.ValidateCheckMacValue(formData))
                {
                    _logger.LogWarning("CheckMacValue 驗證失敗");
                    return BadRequest("0|CheckMacValue驗證失敗");
                }

                var dto = new EcpayNotificationDto
                {
                    MerchantID = GetValue(formData, "MerchantID"),
                    TradeNo = GetValue(formData, "TradeNo"),
                    RtnCode = int.Parse(GetValue(formData, "RtnCode") ?? "0"),
                    RtnMsg = GetValue(formData, "RtnMsg"),
                    TradeAmt = int.Parse(GetValue(formData, "TradeAmt") ?? "0"),
                    PaymentType = GetValue(formData, "PaymentType"),
                    TradeDate = GetValue(formData, "TradeDate"),
                    PaymentDate = GetValue(formData, "PaymentDate"),
                    CheckMacValue = GetValue(formData, "CheckMacValue"),
                    RawBody = rawBody
                };

                var result = await _ecpayService.ProcessPaymentNotificationAsync(dto);
                return result ? Ok("1|OK") : BadRequest("0|處理失敗");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理綠界通知失敗");
                return BadRequest("0|系統錯誤");
            }
        }

        private string GetValue(Dictionary<string, string> data, string key)
        {
            return data.ContainsKey(key) ? data[key] : null;
        }
    }

    public class CreatePaymentRequest
    {
        public string OrderId { get; set; }
        public int TotalAmount { get; set; }
        public string ItemName { get; set; }
    }
}