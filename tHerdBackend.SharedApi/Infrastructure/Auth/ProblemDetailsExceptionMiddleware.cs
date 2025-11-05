using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
namespace tHerdBackend.SharedApi.Infrastructure.Auth
{
	public class ProblemDetailsExceptionMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<ProblemDetailsExceptionMiddleware> _logger;
		private readonly IHostEnvironment _env;

		public ProblemDetailsExceptionMiddleware(
			RequestDelegate next,
			ILogger<ProblemDetailsExceptionMiddleware> logger,
			IHostEnvironment env)
		{
			_next = next;
			_logger = logger;
			_env = env;
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (Exception ex)
			{
				await Handle(context, ex);
			}
		}

		private async Task Handle(HttpContext ctx, Exception ex)
		{
			var status = ex switch
			{
				UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
				KeyNotFoundException => StatusCodes.Status404NotFound,
				ArgumentException => StatusCodes.Status400BadRequest,
				DbUpdateException => StatusCodes.Status409Conflict,
				_ => StatusCodes.Status500InternalServerError
			};

			var title = status switch
			{
				StatusCodes.Status401Unauthorized => "Unauthorized",
				StatusCodes.Status404NotFound => "Not Found",
				StatusCodes.Status400BadRequest => "Bad Request",
				StatusCodes.Status409Conflict => "Conflict",
				_ => "Server Error"
			};

			var type = status switch
			{
				StatusCodes.Status401Unauthorized => "https://httpstatuses.io/401",
				StatusCodes.Status404NotFound => "https://httpstatuses.io/404",
				StatusCodes.Status400BadRequest => "https://httpstatuses.io/400",
				StatusCodes.Status409Conflict => "https://httpstatuses.io/409",
				_ => "https://httpstatuses.io/500"
			};

			var pd = new ProblemDetails
			{
				Status = status,
				Title = title,
				Type = type,
				Detail = _env.IsDevelopment() ? ex.Message : null,
				Instance = ctx.Request.Path
			};
			pd.Extensions["traceId"] = ctx.TraceIdentifier;

			if (status >= 500) _logger.LogError(ex, "Unhandled error");
			else _logger.LogWarning(ex, "Handled {Status} error", status);

			ctx.Response.ContentType = "application/problem+json; charset=utf-8";
			ctx.Response.StatusCode = status;

			await ctx.Response.WriteAsync(JsonSerializer.Serialize(pd, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			}));
		}
	}
}
