using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using tHerdBackend.Core.DTOs.ORD; // 假設共用 Config DTO
using tHerdBackend.Core.Interfaces.SUP;

namespace tHerdBackend.Services.SUP
{
	public class ECPayLogisticsService : IECPayLogisticsService
	{
		private readonly ECPayConfigDTO _config;
		private readonly ILogger<ECPayLogisticsService> _logger;

		// 物流 API 介接網址 (測試環境)
		// 正式環境請改為: https://logistics.ecpay.com.tw/Express/map
		private const string MAP_URL_STAGE = "https://logistics-stage.ecpay.com.tw/Express/map";

		public ECPayLogisticsService(
			IOptions<ECPayConfigDTO> config,
			ILogger<ECPayLogisticsService> logger)
		{
			_config = config.Value;
			_logger = logger;
		}

		/// <summary>
		/// 建立電子地圖表單
		/// </summary>
		public string CreateMapForm(string logisticsSubType, bool isCollection, string serverReplyUrl, int device = 0)
		{
			// 1. 準備參數
			var parameters = new Dictionary<string, string>
			{
				{ "MerchantID", _config.MerchantID },
				{ "LogisticsType", "CVS" },              // 固定為超商取貨
                { "LogisticsSubType", logisticsSubType }, // e.g., UNIMARTC2C
                { "IsCollection", isCollection ? "Y" : "N" },
				{ "ServerReplyURL", serverReplyUrl },
                // 額外參數
                { "Device", device.ToString() }
			};

			// *注意*：單純開啟電子地圖通常「不需要」CheckMacValue，
			// 若綠界未來改版需要，再解除下方註解並加入計算。
			// parameters.Add("CheckMacValue", GenerateCheckMacValueMD5(new SortedDictionary<string, string>(parameters)));

			// 2. 組建 HTML Form
			var formHtml = new StringBuilder();
			formHtml.AppendLine($"<form id='ecpayLogisticsForm' method='POST' action='{MAP_URL_STAGE}'>");
			foreach (var param in parameters)
			{
				formHtml.AppendLine($"<input type='hidden' name='{param.Key}' value='{param.Value}' />");
			}
			// 自動送出 script
			formHtml.AppendLine("<script>document.getElementById('ecpayLogisticsForm').submit();</script>");
			formHtml.AppendLine("</form>");

			_logger.LogInformation($"建立物流地圖表單: Type={logisticsSubType}, ReplyURL={serverReplyUrl}");

			return formHtml.ToString();
		}

		/// <summary>
		/// [物流專用] 產生 MD5 CheckMacValue
		/// </summary>
		private string GenerateCheckMacValueMD5(SortedDictionary<string, string> parameters)
		{
			// 1️⃣ 組原始字串
			var raw = $"HashKey={_config.HashKey}&{string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"))}&HashIV={_config.HashIV}";

			// 2️⃣ URL Encode (物流通常也需要轉小寫)
			string encoded = HttpUtility.UrlEncode(raw).ToLower();

			// 3️⃣ 官方特殊字元還原 (與金流相同)
			encoded = encoded
				.Replace("%2d", "-")
				.Replace("%5f", "_")
				.Replace("%2e", ".")
				.Replace("%21", "!")
				.Replace("%2a", "*")
				.Replace("%28", "(")
				.Replace("%29", ")");

			// 4️⃣ 使用 MD5 (物流核心差異!)
			using var md5 = MD5.Create();
			var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(encoded));
			var checkMac = BitConverter.ToString(bytes).Replace("-", "").ToUpper();

			return checkMac;
		}

		public bool ValidateLogisticsCheckMacValue(Dictionary<string, string> parameters)
		{
			if (!parameters.ContainsKey("CheckMacValue")) return false;

			var receivedMac = parameters["CheckMacValue"];
			parameters.Remove("CheckMacValue"); // 驗證時要移除自己

			var sortedParams = new SortedDictionary<string, string>(parameters);
			var calculatedMac = GenerateCheckMacValueMD5(sortedParams);

			return receivedMac == calculatedMac;
		}
	}
}