using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using tHerdBackend.Core.DTOs.CNT;
using tHerdBackend.Core.Interfaces.CNT;
using tHerdBackend.Infra.Models;



namespace tHerdBackend.SharedApi.Controllers.Module.CNT
{
	[ApiController]
	[Route("api/cnt")]
	//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[Authorize]
	public class CntPurchaseController : ControllerBase
	{
		private readonly ICntPurchaseService _svc;
		private readonly tHerdDBContext _db;

		public CntPurchaseController(ICntPurchaseService svc, tHerdDBContext db)
		{
			_svc = svc;
			_db = db;
		}

		// 建立 / 重新使用購買紀錄
		[HttpPost("articles/{id:int}/purchase")]
		public async Task<ActionResult<PurchaseSummaryDto>> CreatePurchase(
				[FromRoute] int id,
				[FromBody] CreatePurchaseRequestDto request,
				CancellationToken ct)
		{
			int userNumberId = GetCurrentUserNumberId();
			var dto = await _svc.CreateOrReusePurchaseAsync(
				id,
				userNumberId,
				request?.PaymentMethod ?? "LINEPAY",
				ct);

			return Ok(dto);
		}

		[HttpPost("purchases/{id:int}/mock-pay")]
		#if DEBUG
		[AllowAnonymous]   // 開發用
		#endif
		public async Task<IActionResult> MockPay([FromRoute] int id, CancellationToken ct)
		{
			await _svc.MockPayAsync(id, ct: ct);   // 👈 ct 用參數名稱帶
			return NoContent();
		}

		// LINE Pay 付款成功後的 callback
		[HttpGet("payments/linepay/confirm")]
		[AllowAnonymous]  // ⭐ 很重要：LINE Pay 回呼不會帶你的 JWT，所以一定要 AllowAnonymous
		public async Task<IActionResult> LinePayConfirm(
			[FromQuery] int purchaseId,
			[FromQuery] string transactionId,
			[FromQuery] string orderId,
			CancellationToken ct)
		{
			// 1) 把這筆訂單標記為已付款，並寫入交易編號
			await _svc.MockPayAsync(purchaseId, transactionId, ct);

			// 2) 查出這筆訂單對應哪一篇文章
			var purchase = await _db.CntPurchases
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.PurchaseId == purchaseId, ct);

			if (purchase == null)
			{
				// 找不到訂單，就顯示一段簡單訊息（不想 Redirect 也可以）
				return Content("付款完成，但找不到對應訂單，請回會員中心確認。");
			}

			// 3) Redirect 回前端文章頁（期末先寫死 localhost:5173 就好）
			var frontBaseUrl = "http://localhost:5173";
			var redirectUrl = $"{frontBaseUrl}/cnt/article/{purchase.PageId}?paid=1";

			return Redirect(redirectUrl);
		}
		[HttpGet("payments/linepay/cancel")]
		[AllowAnonymous]
		public async Task<IActionResult> LinePayCancel(
			[FromQuery] int purchaseId,
			[FromQuery] string transactionId,
			[FromQuery] string orderId,
			CancellationToken ct)
				{
					// 這裡看需求，要不要把訂單標記成 CANCELLED
					var purchase = await _db.CntPurchases
						.AsNoTracking()
						.FirstOrDefaultAsync(x => x.PurchaseId == purchaseId, ct);

					var frontBaseUrl = "http://localhost:5173";

					if (purchase == null)
					{
						return Redirect($"{frontBaseUrl}/cnt/articles?cancel=1");
					}

					var redirectUrl = $"{frontBaseUrl}/cnt/article/{purchase.PageId}?cancel=1";
					return Redirect(redirectUrl);
				}

		// 會員中心：我購買的文章
		[HttpGet("member/purchased-articles")]
		public async Task<ActionResult<IReadOnlyList<PurchasedArticleDto>>> GetMyPurchasedArticles(
		CancellationToken ct)
		{
			int userNumberId = GetCurrentUserNumberId();
			var items = await _svc.GetPurchasedArticlesAsync(userNumberId, ct);
			return Ok(items);
		}

		private int GetCurrentUserNumberId()
		{
			// 1️⃣ 若未來你有加自訂 claim，就優先用它
			var numClaim = User.FindFirst("user_number_id");
			if (numClaim != null && int.TryParse(numClaim.Value, out var numId))
			{
				return numId;
			}

			// 2️⃣ 目前狀況：用 Identity 的 NameIdentifier (就是 Users.Id 那個 GUID)
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				throw new UnauthorizedAccessException("未登入");  // cookie 都沒有
			}

			// 這裡用你的 DbContext 查 Users 表
			// DbSet 名稱通常是 Users，如果你叫別的名字，就替換掉
			var user = _db.AspNetUsers.FirstOrDefault(u => u.Id == userId);
			if (user == null)
			{
				throw new UnauthorizedAccessException("找不到使用者");
			}

			if (user.UserNumberId <= 0)
			{
				throw new UnauthorizedAccessException("會員編號尚未設定");
			}

			return user.UserNumberId;  // ✅ 這裡就會是 1025 / 1076 這類數字
		}
	}
}
