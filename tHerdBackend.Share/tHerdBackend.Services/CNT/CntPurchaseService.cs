using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.CNT
{
	/// <summary>
	/// 付費文章 – 購買流程 Service
	/// </summary>
	public class CntPurchaseService : ICntPurchaseService
	{
		private readonly ICntPurchaseRepository _purchaseRepo;
		private readonly tHerdDBContext _db;

		public CntPurchaseService(ICntPurchaseRepository purchaseRepo, tHerdDBContext db)
		{
			_purchaseRepo = purchaseRepo;
			_db = db;


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
					// 1. 先找舊訂單（你原本就有）
					var existing = await _purchaseRepo.GetByUserAndPageAsync(
						userNumberId, pageId, ct);

					if (existing != null)
					{
						// 已經買過 / 或有未付款訂單，直接用這筆
						return existing;
					}

					// 2. 從 CNT_Page 撈出這篇文章，拿 Price
					var page = await _db.CntPages
						.AsNoTracking()
						.FirstOrDefaultAsync(p => p.PageId == pageId, ct);

					if (page == null)
						throw new InvalidOperationException($"找不到 PageId={pageId} 的文章");

					// 如果你有區分是不是付費文章，可以順便檢查：
					if (!page.IsPaidContent)
						throw new InvalidOperationException("這篇文章不是付費內容，不能建立購買紀錄");

					// CNT_Page.Price 可能是 decimal?，記得處理 null
					var price = page.Price ?? 0m;   // 或者你不想允許 0，可以再加判斷

					// 3. 用文章的 Price 建立新的購買紀錄
					var created = await _purchaseRepo.CreateAsync(
						userNumberId,
						pageId,
						price,
						paymentMethod,
						ct);

					return created;
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

		public Task MockPayAsync(int purchaseId, CancellationToken ct = default)
		{
			// 直接呼叫 Repository 更新付款狀態
			return _purchaseRepo.UpdatePaymentAsync(
				purchaseId,
				isPaid: true,
				paymentStatus: "PAID",
				gatewayTransactionId: "MOCK",
				ct);
		}
	}
}
