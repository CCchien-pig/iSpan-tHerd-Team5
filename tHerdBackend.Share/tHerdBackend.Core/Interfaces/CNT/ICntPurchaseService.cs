using tHerdBackend.Core.DTOs.CNT;


namespace tHerdBackend.Core.Interfaces.CNT
{
	public interface ICntPurchaseService
	{
		Task<PurchaseSummaryDto> CreateOrReusePurchaseAsync(
			int pageId,
			int userNumberId,
			string paymentMethod,
			CancellationToken ct = default);

		Task<IReadOnlyList<PurchasedArticleDto>> GetPurchasedArticlesAsync(
			int userNumberId,
			CancellationToken ct = default);

		/// <summary>把某筆購買紀錄標記為已付款</summary>
		Task MockPayAsync(
			int purchaseId,
			string gatewayTransactionId = "MOCK",
			CancellationToken ct = default);
	}
}
