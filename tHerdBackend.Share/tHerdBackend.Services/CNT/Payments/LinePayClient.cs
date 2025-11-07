using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace tHerdBackend.Services.Payments
{
	/// <summary>
	/// appsettings.json 中的 LinePay 設定
	/// </summary>
	public class LinePayOptions
	{
		public string ChannelId { get; set; } = string.Empty;
		public string ChannelSecret { get; set; } = string.Empty;
		public string BaseUrl { get; set; } = string.Empty;
		public string ConfirmUrl { get; set; } = string.Empty;
		public string CancelUrl { get; set; } = string.Empty;
	}

	public record LinePayRequestResult(
		string TransactionId,
		string PaymentUrl
	);

	/// <summary>
	/// 專門跟 LINE Pay 溝通的 HttpClient 包裝
	/// </summary>
	public class LinePayClient
	{
		private readonly HttpClient _http;
		private readonly LinePayOptions _opt;

		public LinePayClient(HttpClient http, IOptions<LinePayOptions> opt)
		{
			_http = http;
			_opt = opt.Value;

			if (!string.IsNullOrWhiteSpace(_opt.BaseUrl))
			{
				_http.BaseAddress = new Uri(_opt.BaseUrl);
			}
		}

		/// <summary>
		/// 建立 LINE Pay 付款請求，回傳交易編號 + 付款網址
		/// </summary>
		public async Task<LinePayRequestResult> RequestPaymentAsync(
			int purchaseId,
			int pageId,
			string title,
			decimal amount,
			CancellationToken ct = default)
		{
			const string uri = "/v3/payments/request";

			var bodyObj = new
			{
				amount = (int)amount,
				currency = "TWD",
				orderId = $"ARTICLE-{pageId}-PUR-{purchaseId}",
				packages = new[]
				{
					new
					{
						id = "1",
						amount = (int)amount,
						name = title,
						products = new[]
						{
							new { name = title, quantity = 1, price = (int)amount }
						}
					}
				},
				redirectUrls = new
				{
					confirmUrl = $"{_opt.ConfirmUrl}?purchaseId={purchaseId}",
					cancelUrl = $"{_opt.CancelUrl}?purchaseId={purchaseId}"
				}
			};

			var bodyJson = JsonSerializer.Serialize(bodyObj);
			var nonce = Guid.NewGuid().ToString("N");
			var signature = BuildSignature(uri, bodyJson, nonce);

			using var msg = new HttpRequestMessage(HttpMethod.Post, uri);
			msg.Headers.Add("X-LINE-ChannelId", _opt.ChannelId);
			msg.Headers.Add("X-LINE-Authorization-Nonce", nonce);
			msg.Headers.Add("X-LINE-Authorization", signature);
			msg.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

			var resp = await _http.SendAsync(msg, ct);
			resp.EnsureSuccessStatusCode();

			var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

			var returnCode = json.GetProperty("returnCode").GetString();
			if (returnCode != "0000")
			{
				throw new InvalidOperationException($"LINE Pay request failed, returnCode={returnCode}");
			}

			var info = json.GetProperty("info");
			var transactionId = info.GetProperty("transactionId").GetInt64().ToString();
			var paymentUrl = info.GetProperty("paymentUrl").GetProperty("web").GetString()
							 ?? throw new InvalidOperationException("LINE Pay 回傳沒有 paymentUrl.web");

			return new LinePayRequestResult(transactionId, paymentUrl);
		}

		/// <summary>
		/// 付款完成後用來確認交易；這部分你之後在 Confirm Endpoint 再來用
		/// </summary>
		public async Task<string> ConfirmAsync(
			string transactionId,
			decimal amount,
			CancellationToken ct = default)
		{
			var uri = $"/v3/payments/{transactionId}/confirm";

			var bodyObj = new
			{
				amount = (int)amount,
				currency = "TWD"
			};

			var bodyJson = JsonSerializer.Serialize(bodyObj);
			var nonce = Guid.NewGuid().ToString("N");
			var signature = BuildSignature(uri, bodyJson, nonce);

			using var msg = new HttpRequestMessage(HttpMethod.Post, uri);
			msg.Headers.Add("X-LINE-ChannelId", _opt.ChannelId);
			msg.Headers.Add("X-LINE-Authorization-Nonce", nonce);
			msg.Headers.Add("X-LINE-Authorization", signature);
			msg.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

			var resp = await _http.SendAsync(msg, ct);
			resp.EnsureSuccessStatusCode();

			var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
			return json.GetProperty("returnCode").GetString() ?? "";
		}

		private string BuildSignature(string uri, string body, string nonce)
		{
			// 簽章規則：ChannelSecret + uri + body + nonce 做 HMAC-SHA256 再 base64
			var data = _opt.ChannelSecret + uri + body + nonce;
			using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_opt.ChannelSecret));
			var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
			return Convert.ToBase64String(hash);
		}
	}
}
