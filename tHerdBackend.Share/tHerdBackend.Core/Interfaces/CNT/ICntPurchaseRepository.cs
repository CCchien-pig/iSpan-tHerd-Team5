using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.CNT;

namespace tHerdBackend.Core.Interfaces.CNT
{
	public interface ICntPurchaseRepository
	{
		/// <summary>
		/// 取得某會員對某篇文章的最新購買紀錄（或 null）
		/// </summary>
		Task<PurchaseSummaryDto?> GetByUserAndPageAsync(
			int userNumberId,
			int pageId,
			CancellationToken ct = default);

		/// <summary>
		/// 建立一筆「待付款」的購買紀錄，回傳摘要（含 PurchaseId）
		/// </summary>
		Task<PurchaseSummaryDto> CreateAsync(
			int userNumberId,
			int pageId,
			decimal unitPrice,
			string paymentMethod,
			CancellationToken ct = default);

		/// <summary>
		/// 更新付款結果（例如金流回傳後）
		/// </summary>
		Task UpdatePaymentAsync(
			int purchaseId,
			bool isPaid,
			string paymentStatus,
			string? gatewayTransactionId,
			CancellationToken ct = default);

		/// <summary>
		/// 會員中心：「我買過的文章」列表
		/// </summary>
		Task<IReadOnlyList<PurchasedArticleDto>> GetPaidByUserAsync(
			int userNumberId,
			CancellationToken ct = default);
	}
}
