using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace tHerdBackend.Services.ORD
{
    /// <summary>
    /// ORD 模組診斷中介軟體
    /// 用於處理 ngrok 轉發請求和記錄診斷資訊
    /// </summary>
    public class ORDDiagnosticMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ORDDiagnosticMiddleware> _logger;

        public ORDDiagnosticMiddleware(
            RequestDelegate next,
            ILogger<ORDDiagnosticMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 只處理 ORD Payment 相關的請求
            if (context.Request.Path.StartsWithSegments("/api/ord/payment"))
            {
                // 記錄請求資訊
                _logger.LogInformation(
                    "ORD Payment Request: {Method} {Path} from {Host}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.Host);

                // 處理 ngrok 轉發的標頭
                if (context.Request.Headers.ContainsKey("X-Forwarded-Host"))
                {
                    var forwardedHost = context.Request.Headers["X-Forwarded-Host"].ToString();
                    _logger.LogInformation("X-Forwarded-Host: {ForwardedHost}", forwardedHost);
                }

                if (context.Request.Headers.ContainsKey("X-Forwarded-Proto"))
                {
                    var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString();
                    _logger.LogInformation("X-Forwarded-Proto: {ForwardedProto}", forwardedProto);
                }

                // 記錄所有標頭（僅開發環境）
                if (context.Request.Headers.ContainsKey("ngrok-trace-id"))
                {
                    var ngrokTraceId = context.Request.Headers["ngrok-trace-id"].ToString();
                    _logger.LogInformation("Ngrok-Trace-Id: {NgrokTraceId}", ngrokTraceId);
                }

                // 確保回應標頭設定正確
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey("Content-Type"))
                    {
                        context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
                    }
                    return Task.CompletedTask;
                });
            }

            await _next(context);
        }
    }

    /// <summary>
    /// ORD 診斷中介軟體擴展方法
    /// </summary>
    public static class ORDDiagnosticMiddlewareExtensions
    {
        /// <summary>
        /// 使用 ORD 診斷中介軟體
        /// </summary>
        public static IApplicationBuilder UseORDDiagnostic(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ORDDiagnosticMiddleware>();
        }
    }
}