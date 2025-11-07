namespace tHerdBackend.Core.DTOs.SUP.Brand
{
	public class ECPayLogisticsConfig
	{
		public string MerchantID { get; set; }
		public string HashKey { get; set; }
		public string HashIV { get; set; }
		// 注意：物流的網址參數名稱可能跟金流不一樣，依您需求設計
		public string MapUrl { get; set; }     // 電子地圖網址
		public string CreateUrl { get; set; }  // 建立訂單網址
	}
}
