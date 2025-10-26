using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.ORD;
using tHerdBackend.Core.Interfaces.ORD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
    [ApiController]
    [Route("api/ord/payment")]
    [EnableCors] // 啟用 CORS
    [Produces("application/json")] // 明確指定回應格式
    public class PaymentController : ControllerBase
    {
        private readonly IECPayService _ecpayService;
        private readonly ILogger<PaymentController> _logger;
        private readonly tHerdDBContext _context;

        public PaymentController(
            IECPayService ecpayService,
            ILogger<PaymentController> logger,
            tHerdDBContext context)
        {
            _ecpayService = ecpayService;
            _logger = logger;
            _context = context;
        }

        [HttpGet("test")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AllowAnonymous]
        public IActionResult Test()
        {
            try
            {
                // 記錄請求資訊以便診斷
                _logger.LogInformation("Payment Test API called from: {Host}",
                    Request.Host.ToString());
                _logger.LogInformation("X-Forwarded-Host: {ForwardedHost}",
                    Request.Headers["X-Forwarded-Host"].ToString());

                var response = new
                {
                    success = true,
                    message = "Payment API is working",
                    timestamp = DateTime.Now,
                    routes = new[]
                    {
                        "/api/ord/payment/test (GET) - 測試端點",
                        "/api/ord/payment/ecpay/create (POST) - 建立付款",
                        "/api/ord/payment/ecpay/notification (POST) - 綠界通知"
                    },
                    requestInfo = new
                    {
                        host = Request.Host.ToString(),
                        scheme = Request.Scheme,
                        forwardedHost = Request.Headers["X-Forwarded-Host"].ToString(),
                        forwardedProto = Request.Headers["X-Forwarded-Proto"].ToString()
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

        [HttpPost("ecpay/notify")]
        [AllowAnonymous]
        public async Task<IActionResult> EcpayNotify()
        {
            try
            {
                _logger.LogInformation("=================================================");
                _logger.LogInformation("收到綠界付款通知");
                _logger.LogInformation($"時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"請求方法: {Request.Method}");
                _logger.LogInformation($"請求路徑: {Request.Path}");

                var formData = new Dictionary<string, string>();
                foreach (var key in Request.Form.Keys)
                {
                    var value = Request.Form[key].ToString();
                    formData[key] = value;
                    _logger.LogInformation($"  {key} = {value}");
                }

                if (formData.Count == 0)
                {
                    _logger.LogWarning("沒有收到任何表單資料");
                    return Content("0|No Data");
                }

                _logger.LogInformation("=================================================");

                var formDataForValidation = new Dictionary<string, string>(formData);
                if (!_ecpayService.ValidateCheckMacValue(formDataForValidation))
                {
                    _logger.LogWarning("CheckMacValue 驗證失敗");
                    return Content("0|CheckMacValue Error");
                }

                _logger.LogInformation("CheckMacValue 驗證成功");

                // 儲存原始通知記錄
                try
                {
                    var rawBody = string.Join("&", formData.Select(x => $"{x.Key}={x.Value}"));

                    var notification = new OrdEcpayReturnNotification();

                    SetPropertyValue(notification, "ReceivedDate", DateTime.Now);
                    SetPropertyValue(notification, "MerchantId", GetValue(formData, "MerchantID"));
                    SetPropertyValue(notification, "MerchantTradeNo", GetValue(formData, "MerchantTradeNo"));
                    SetPropertyValue(notification, "StoreId", GetValue(formData, "StoreID"));
                    SetPropertyValue(notification, "PlatformId", GetValue(formData, "PlatformID"));
                    SetPropertyValue(notification, "RtnCode", GetValue(formData, "RtnCode"));
                    SetPropertyValue(notification, "RtnMsg", GetValue(formData, "RtnMsg"));
                    SetPropertyValue(notification, "TradeNo", GetValue(formData, "TradeNo"));
                    SetPropertyValue(notification, "TradeAmt", GetValue(formData, "TradeAmt"));
                    SetPropertyValue(notification, "PaymentDate", GetValue(formData, "PaymentDate"));
                    SetPropertyValue(notification, "PaymentType", GetValue(formData, "PaymentType"));
                    SetPropertyValue(notification, "PaymentTypeChargeFee", GetValue(formData, "PaymentTypeChargeFee"));
                    SetPropertyValue(notification, "TradeDate", GetValue(formData, "TradeDate"));
                    SetPropertyValue(notification, "SimulatePaid", GetValue(formData, "SimulatePaid") == "1" ? 1 : 0);
                    SetPropertyValue(notification, "CheckMacValue", GetValue(formData, "CheckMacValue"));
                    SetPropertyValue(notification, "CustomField1", GetValue(formData, "CustomField1"));
                    SetPropertyValue(notification, "CustomField2", GetValue(formData, "CustomField2"));
                    SetPropertyValue(notification, "CustomField3", GetValue(formData, "CustomField3"));
                    SetPropertyValue(notification, "CustomField4", GetValue(formData, "CustomField4"));
                    SetPropertyValue(notification, "RawBody", rawBody);

                    _context.OrdEcpayReturnNotifications.Add(notification);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"通知記錄已儲存");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "儲存通知記錄失敗");
                }

                // 更新付款記錄
                var merchantTradeNo = GetValue(formData, "MerchantTradeNo");
                var payment = await _context.OrdPayments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.MerchantTradeNo == merchantTradeNo);

                if (payment == null)
                {
                    _logger.LogWarning($"找不到付款記錄: {merchantTradeNo}");
                    return Content("1|OK");
                }

                var rtnCode = GetValue(formData, "RtnCode");
                var tradeNo = GetValue(formData, "TradeNo");

                payment.TradeNo = tradeNo;

                // 修正:RtnCode 轉換為 int
                if (int.TryParse(rtnCode, out var rtnCodeInt))
                {
                    payment.RtnCode = rtnCodeInt;
                }

                payment.RtnMsg = GetValue(formData, "RtnMsg");
                payment.CheckMacValue = GetValue(formData, "CheckMacValue");

                var paymentDateStr = GetValue(formData, "PaymentDate");
                if (!string.IsNullOrEmpty(paymentDateStr) && DateTime.TryParse(paymentDateStr, out var paymentDate))
                {
                    payment.TradeDate = paymentDate;
                }

                if (rtnCode == "1")
                {
                    payment.Status = "paid";
                    payment.SimulatePaid = true;  // 修正:改為 true

                    if (payment.Order != null)
                    {
                        payment.Order.PaymentStatus = "paid";
                        payment.Order.OrderStatusId = "paid";
                        payment.Order.RevisedDate = DateTime.Now;

                        _logger.LogInformation($"訂單 {payment.Order.OrderNo} 付款成功");
                    }
                }
                else
                {
                    payment.Status = "failed";

                    if (payment.Order != null)
                    {
                        payment.Order.PaymentStatus = "failed";
                    }

                    _logger.LogWarning($"付款失敗: RtnCode={rtnCode}");
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("付款狀態已更新");
                _logger.LogInformation("=================================================");

                return Content("1|OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理綠界通知時發生錯誤");
                _logger.LogError($"錯誤訊息: {ex.Message}");
                _logger.LogError($"堆疊追蹤: {ex.StackTrace}");
                return Content("0|Error");
            }
        }

        private void SetPropertyValue(object obj, string propertyName, object value)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (value == null || string.IsNullOrEmpty(value.ToString()))
                    {
                        property.SetValue(obj, null);
                        return;
                    }

                    object convertedValue = null;

                    if (propertyType == typeof(int))
                    {
                        if (int.TryParse(value.ToString(), out var intValue))
                            convertedValue = intValue;
                    }
                    else if (propertyType == typeof(decimal))
                    {
                        if (decimal.TryParse(value.ToString(), out var decimalValue))
                            convertedValue = decimalValue;
                    }
                    else if (propertyType == typeof(DateTime))
                    {
                        if (DateTime.TryParse(value.ToString(), out var dateValue))
                            convertedValue = dateValue;
                    }
                    else if (propertyType == typeof(bool))
                    {
                        convertedValue = value.ToString() == "1" || value.ToString().ToLower() == "true";
                    }
                    else
                    {
                        convertedValue = value;
                    }

                    property.SetValue(obj, convertedValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"無法設定屬性 {propertyName}: {ex.Message}");
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