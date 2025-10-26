using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using System.Linq;

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
        /// 測試端點 - 檢查 API 是否正常運作
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            try
            {
                _logger.LogInformation("Payment Test API called from: {Host}", Request.Host.ToString());

                var response = new
                {
                    success = true,
                    message = "Payment API is working",
                    timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                    server = new
                    {
                        host = Request.Host.ToString(),
                        scheme = Request.Scheme,
                        path = Request.Path.ToString(),
                        method = Request.Method
                    },
                    headers = new
                    {
                        forwardedHost = Request.Headers.ContainsKey("X-Forwarded-Host")
                            ? Request.Headers["X-Forwarded-Host"].ToString()
                            : null,
                        forwardedProto = Request.Headers.ContainsKey("X-Forwarded-Proto")
                            ? Request.Headers["X-Forwarded-Proto"].ToString()
                            : null,
                        ngrokTraceId = Request.Headers.ContainsKey("ngrok-trace-id")
                            ? Request.Headers["ngrok-trace-id"].ToString()
                            : null,
                        userAgent = Request.Headers.ContainsKey("User-Agent")
                            ? Request.Headers["User-Agent"].ToString()
                            : null
                    },
                    routes = new[]
                    {
                        "/api/ord/payment/test (GET) - 測試端點",
                        "/api/ord/payment/ecpay/create (POST) - 建立付款",
                        "/api/ord/payment/ecpay/notify (POST) - 綠界通知"
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Payment Test API");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 建立綠界付款
        /// </summary>
        [HttpPost("ecpay/create")]
        public IActionResult CreateECPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Creating ECPay payment: OrderId={OrderId}, Amount={Amount}",
                    request.OrderId,
                    request.TotalAmount);

                var formHtml = _ecpayService.CreatePaymentForm(
                    request.OrderId,
                    request.TotalAmount,
                    request.ItemName
                );

                return Content(formHtml, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立綠界付款失敗: OrderId={OrderId}", request?.OrderId);
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
                _logger.LogInformation("Received ECPay notification from: {Host}", Request.Host.ToString());

                var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
                var rawBody = string.Join("&", formData.Select(x => $"{x.Key}={x.Value}"));

                _logger.LogDebug("ECPay notification data: {RawBody}", rawBody);

                if (!_ecpayService.ValidateCheckMacValue(formData))
                {
                    _logger.LogWarning("CheckMacValue 驗證失敗: {RawBody}", rawBody);
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

                _logger.LogInformation(
                    "Processing ECPay notification: TradeNo={TradeNo}, RtnCode={RtnCode}",
                    dto.TradeNo,
                    dto.RtnCode);

                var result = await _ecpayService.ProcessPaymentNotificationAsync(dto);

                if (result)
                {
                    _logger.LogInformation("ECPay notification processed successfully: TradeNo={TradeNo}", dto.TradeNo);
                    return Ok("1|OK");
                }
                else
                {
                    _logger.LogWarning("ECPay notification processing failed: TradeNo={TradeNo}", dto.TradeNo);
                    return BadRequest("0|處理失敗");
                }
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