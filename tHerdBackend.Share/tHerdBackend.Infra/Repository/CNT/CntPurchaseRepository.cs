using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.CNT
{
	public class CntPurchaseRepository : ICntPurchaseRepository
	{
		private readonly tHerdDBContext _db;

		public CntPurchaseRepository(tHerdDBContext db)
		{
			_db = db;
		}

		// 1️⃣ 取得某會員 + 某文章的最新購買摘要（或 null）
		public async Task<PurchaseSummaryDto?> GetByUserAndPageAsync(
			int userNumberId,
			int pageId,
			CancellationToken ct = default)
		{
			var entity = await _db.CntPurchases
								  .AsNoTracking()
								  .Where(x => x.UserNumberId == userNumberId &&
											  x.PageId == pageId)
								  .OrderByDescending(x => x.CreatedDate)
								  .FirstOrDefaultAsync(ct);

			return entity == null ? null : MapToSummary(entity);
		}

		// 2️⃣ 建立一筆新的購買紀錄（PENDING），回傳摘要
		public async Task<PurchaseSummaryDto> CreateAsync(
			int userNumberId,
			int pageId,
			decimal unitPrice,
			string paymentMethod,
			CancellationToken ct = default)
		{
			var entity = new CntPurchase
			{
				UserNumberId = userNumberId,
				PageId = pageId,
				UnitPrice = unitPrice,
				IsPaid = false,
				PaymentMethod = paymentMethod,
				PaymentStatus = "PENDING",
				Currency = "TWD",
				CreatedDate = DateTime.UtcNow
			};

			await _db.CntPurchases.AddAsync(entity, ct);
			await _db.SaveChangesAsync(ct);

			return MapToSummary(entity);
		}

		// 3️⃣ 更新付款狀態（例如金流 callback 後）
		public async Task UpdatePaymentAsync(
			int purchaseId,
			bool isPaid,
			string paymentStatus,
			string? gatewayTransactionId,
			CancellationToken ct = default)
		{
			var entity = await _db.CntPurchases
								  .FirstOrDefaultAsync(x => x.PurchaseId == purchaseId, ct);

			if (entity == null) return;   // 也可以選擇 throw

			entity.IsPaid = isPaid;
			entity.PaymentStatus = paymentStatus;
			entity.GatewayTransactionId = gatewayTransactionId;
			entity.RevisedDate = DateTime.UtcNow;

			await _db.SaveChangesAsync(ct);
		}

		// 4️⃣ 會員中心：取得某會員所有已付款的文章清單（已組好 DTO）
		public async Task<IReadOnlyList<PurchasedArticleDto>> GetPaidByUserAsync(
			int userNumberId,
			CancellationToken ct = default)
		{
			var list = await _db.CntPurchases
								.Include(x => x.Page)
									.ThenInclude(p => p.PageType) // 如果有 PageType 導覽屬性
								.Where(x => x.UserNumberId == userNumberId &&
											x.IsPaid)
								.OrderByDescending(x => x.CreatedDate)
								.ToListAsync(ct);

			return list
				.Where(x => x.Page != null)
				.Select(x => new PurchasedArticleDto
				{
					PurchaseId = x.PurchaseId,
					PageId = x.PageId,
					Title = x.Page.Title,
					UnitPrice = x.UnitPrice,
					PurchasedDate = x.CreatedDate,
					PublishedDate = x.Page.PublishedDate,
					CategoryName = x.Page.PageType?.TypeName ?? ""
				})
				.ToList();
		}

		// 共用：把 Entity 變成 PurchaseSummaryDto
		private static PurchaseSummaryDto MapToSummary(CntPurchase x)
		{
			return new PurchaseSummaryDto
			{
				PurchaseId = x.PurchaseId,
				PageId = x.PageId,
				Amount = x.UnitPrice,
				IsPaid = x.IsPaid,
				PaymentMethod = x.PaymentMethod,
				PaymentStatus = x.PaymentStatus,
				PaymentUrl = null   // 之後串金流時在 Service 裡補
			};
		}
	}
}
