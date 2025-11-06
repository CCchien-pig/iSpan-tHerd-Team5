using Microsoft.AspNetCore.Authentication.JwtBearer; // JwtBearerDefaults
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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
		[AllowAnonymous]   // 或保留 [Authorize] 都可以，開發用而已
		#endif
		public async Task<IActionResult> MockPay([FromRoute] int id, CancellationToken ct)
		{
			await _svc.MockPayAsync(id, ct);  // 你在 Service 寫一個簡單方法呼叫 UpdatePaymentAsync
			return NoContent();
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
