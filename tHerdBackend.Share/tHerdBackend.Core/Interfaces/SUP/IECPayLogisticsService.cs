namespace tHerdBackend.Core.Interfaces.SUP
{
	public interface IECPayLogisticsService
	{
		/// <summary>
		/// 建立綠界電子地圖選擇門市的 HTML 表單
		/// </summary>
		/// <param name="logisticsSubType">物流子類型 (e.g., UNIMARTC2C, FAMIC2C)</param>
		/// <param name="isCollection">是否代收貨款 (Y/N)</param>
		/// <param name="serverReplyUrl">使用者選完門市後，綠界 POST 回來的後端網址</param>
		/// <param name="device">裝置類型 (0: PC, 1: Mobile)</param>
		/// <returns>自動送出的 HTML Form 字串</returns>
		string CreateMapForm(string logisticsSubType, bool isCollection, string serverReplyUrl, int device = 0);

		/// <summary>
		/// (未來擴充) 驗證物流相關的 CheckMacValue (使用 MD5)
		/// </summary>
		bool ValidateLogisticsCheckMacValue(Dictionary<string, string> parameters);
	}
}