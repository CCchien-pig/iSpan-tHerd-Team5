using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Models;
using tHerdBackend.Services.Payments;

namespace tHerdBackend.Services.CNT
{
	/// <summary>
	/// 付費文章 – 購買流程 Service
	/// </summary>
	public class CntPurchaseService : ICntPurchaseService
	{
		private readonly ICntPurchaseRepository _purchaseRepo;
		private readonly tHerdDBContext _db;
		private readonly LinePayClient _linePay;   // ⭐ 新增

		public CntPurchaseService(ICntPurchaseRepository purchaseRepo, tHerdDBContext db, LinePayClient linePay)
		{
			_purchaseRepo = purchaseRepo;
			_db = db;
			_linePay = linePay;


		}

		/// <summary>
		/// 建立或重用購買紀錄
		/// 1. 若會員對該文章已有紀錄：
		///    - 已付款：直接回傳（IsPaid = true）
		///    - 未付款：沿用該筆訂單（IsPaid = false）
		/// 2. 若沒有紀錄：建立一筆新的 pending 訂單
		/// </summary>
		public async Task<PurchaseSummaryDto> CreateOrReusePurchaseAsync(
			int pageId,
			int userNumberId,
			string paymentMethod,
			CancellationToken ct = default)
		{
			// 0) 先把文章抓出來，拿 Title / Price / IsPaidContent
			var page = await _db.CntPages
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.PageId == pageId, ct);

			if (page == null)
				throw new InvalidOperationException($"找不到 PageId={pageId} 的文章");

			if (!page.IsPaidContent)
				throw new InvalidOperationException("這篇文章不是付費內容，不能建立購買紀錄");

			var price = page.Price ?? 0m;

			// 1) 查這個會員 + 這篇文章有沒有舊訂單
			var summary = await _purchaseRepo.GetByUserAndPageAsync(
				userNumberId, pageId, ct);

			// 1-1) 沒有就建立一筆新的 pending 訂單
			if (summary == null)
			{
				summary = await _purchaseRepo.CreateAsync(
					userNumberId,
					pageId,
					price,
					paymentMethod,
					ct);
			}

			// 2) 如果已經是已付款訂單，就不用再叫金流，直接回傳
			if (summary.IsPaid)
			{
				return summary;
			}

			// 3) 若付款方式是 LINEPAY，就去 LINE Pay 建立付款請求
			if (string.Equals(paymentMethod, "LINEPAY", StringComparison.OrdinalIgnoreCase))
			{
				var result = await _linePay.RequestPaymentAsync(
					summary.PurchaseId,          // 用這筆 purchase 當 orderId 的一部分
					summary.PageId,
					page.Title ?? $"文章 {summary.PageId}",
					summary.Amount,
					ct);

				// 把 transactionId 記到 DB，但還是標記成 PENDING（尚未真正付款）
				await _purchaseRepo.UpdatePaymentAsync(
					summary.PurchaseId,
					isPaid: false,
					paymentStatus: "PENDING",
					gatewayTransactionId: result.TransactionId,
					ct);

				// ⭐ 把付款網址塞回 DTO，前端就可以拿來 redirect
				summary.PaymentUrl = result.PaymentUrl;
				summary.PaymentStatus = "PENDING";
			}

			return summary;
		}

		/// <summary>
		/// 會員中心：「我買過的文章」列表
		/// </summary>
		public Task<IReadOnlyList<PurchasedArticleDto>> GetPurchasedArticlesAsync(
			int userNumberId,
			CancellationToken ct = default)
		{
			return _purchaseRepo.GetPaidByUserAsync(userNumberId, ct);
		}

		public Task MockPayAsync(
			int purchaseId,
			string gatewayTransactionId = "MOCK",
			CancellationToken ct = default)
				{
					// 直接呼叫 Repository 更新付款狀態
					return _purchaseRepo.UpdatePaymentAsync(
						purchaseId,
						isPaid: true,
						paymentStatus: "PAID",
						gatewayTransactionId: gatewayTransactionId,
						ct);
				}

	}
}
