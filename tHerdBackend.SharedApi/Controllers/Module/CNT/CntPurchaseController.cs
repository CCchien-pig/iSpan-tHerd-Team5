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
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
			var claim =
				User.FindFirst("user_number_id") ??
				User.FindFirst("sub") ??
				User.FindFirst("uid") ??
				User.FindFirst(ClaimTypes.NameIdentifier);

			if (claim == null || !int.TryParse(claim.Value, out var id))
			{
				// 這種寫法之後可以在 Action 裡 catch 到，回 Unauthorized()
				throw new UnauthorizedAccessException("未登入");
			}

			return id;
		}
	}
}
