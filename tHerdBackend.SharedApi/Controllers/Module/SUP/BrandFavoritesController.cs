using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	/// <summary>品牌收藏 API</summary>
	[ApiController]
	[Route("api/sup/[controller]")]
	[Authorize]
	public class BrandFavoritesController : ControllerBase
	{
		private readonly IBrandFavoriteService _service;
		private readonly ICurrentUser _me;
		private readonly ILogger<BrandFavoritesController> _logger;

		public BrandFavoritesController(
			IBrandFavoriteService service,
			ICurrentUser me,
			ILogger<BrandFavoritesController> logger)
		{
			_service = service;
			_me = me;
			_logger = logger;
		}

		/// <summary>新增品牌收藏（僅限登入）</summary>
		// POST /api/sup/BrandFavorites
		[HttpPost]
		public async Task<ActionResult<ApiResponse<bool>>> Add([FromBody] BrandFavoriteCreateBody body, CancellationToken ct)
		{
			var userId = _me.UserNumberId;
			_logger.LogInformation("BrandFavorite Add called. user={UserNumberId}, brand={BrandId}", userId, body.BrandId);

			try
			{
				var result = await _service.AddAsync(userId, body.BrandId, ct);
				_logger.LogInformation("BrandFavorite Add result. user={UserNumberId}, brand={BrandId}, success={Success}", userId, body.BrandId, result.Success);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BrandFavorite Add failed. user={UserNumberId}, brand={BrandId}", userId, body.BrandId);
				return Ok(ApiResponse<bool>.Fail("新增收藏發生錯誤"));
			}
		}

		/// <summary>取消品牌收藏（僅限登入）</summary>
		// DELETE /api/sup/BrandFavorites/{brandId}
		[HttpDelete("{brandId:int}")]
		public async Task<ActionResult<ApiResponse<bool>>> Remove([FromRoute] int brandId, CancellationToken ct)
		{
			var userId = _me.UserNumberId;
			_logger.LogInformation("BrandFavorite Remove called. user={UserNumberId}, brand={BrandId}", userId, brandId);

			try
			{
				var result = await _service.RemoveAsync(userId, brandId, ct);
				_logger.LogInformation("BrandFavorite Remove result. user={UserNumberId}, brand={BrandId}, success={Success}", userId, brandId, result.Success);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BrandFavorite Remove failed. user={UserNumberId}, brand={BrandId}", userId, brandId);
				return Ok(ApiResponse<bool>.Fail("取消收藏發生錯誤"));
			}
		}

		/// <summary>查詢是否已收藏（僅限登入）</summary>
		// Get /api/sup/BrandFavorites/exists/{brandId}
		[HttpGet("exists/{brandId:int}")]
		public async Task<ActionResult<ApiResponse<bool>>> Exists([FromRoute] int brandId, CancellationToken ct)
		{
			var userId = _me.UserNumberId;

			try
			{
				var result = await _service.ExistsAsync(userId, brandId, ct);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BrandFavorite Exists failed. user={UserNumberId}, brand={BrandId}", userId, brandId);
				return Ok(ApiResponse<bool>.Fail("查詢狀態發生錯誤"));
			}
		}

		/// <summary>取得我的品牌收藏清單（僅限登入）</summary>
		// Get /api/sup/BrandFavorites/my
		[HttpGet("my")]
		public async Task<ActionResult<ApiResponse<List<BrandFavoriteItemDto>>>> MyList(CancellationToken ct)
		{
			var userId = _me.UserNumberId;

			try
			{
				var result = await _service.GetMyListAsync(userId, ct);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BrandFavorite MyList failed. user={UserNumberId}", userId);
				return Ok(ApiResponse<List<BrandFavoriteItemDto>>.Fail("取得清單發生錯誤"));
			}
		}

	}

	/// <summary>
	/// 新增收藏請求的簡化 body（僅需品牌 Id）
	/// </summary>
	public class BrandFavoriteCreateBody
	{
		/// <summary>品牌 Id</summary>
		public int BrandId { get; set; }
	}
}
