using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.PROD.ord;
using tHerdBackend.Core.DTOs.PROD.sup;
using tHerdBackend.Core.DTOs.PROD.user;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.ValueObjects;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Repository.PROD;

namespace tHerdBackend.SharedApi.Controllers.Module.PROD
{
	[ApiController]
	[Area("prod")]
	[Route("api/[area]/[controller]")]
	public class ProductsController : ControllerBase
	{
		private readonly IProductsForApiService _service;  // 服務注入
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly ApplicationDbContext _appDb;  // Identity 專用

		// 建構子注入 Service
		public ProductsController(IProductsForApiService service,
			UserManager<ApplicationUser> userMgr,
			ApplicationDbContext appDb)
		{
			_userMgr = userMgr;
			_appDb = appDb;
			_service = service;
		}

        /// <summary>
        /// 前台：查詢產品清單 (支援關鍵字、分類、價格區間、分頁)
        /// </summary>
        /// <param name="query">查詢條件</param>
        /// <param name="ct">連線</param>
        /// <returns></returns>
        [AllowAnonymous]  // 不用 JWT，前台也能看
        [HttpPost("search")]
		public async Task<IActionResult> SearchProducts(
			[FromBody] ProductFrontFilterQueryDto query,
			CancellationToken ct = default)
		{
            try
            {
                var result = await _service.GetFrontProductListAsync(query, ct);

                return Ok(ApiResponse<PagedResult<ProdProductSearchDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                // 捕捉異常（例如 SQL 錯誤、參數錯誤）
                return StatusCode(500, ApiResponse<string>.Fail($"伺服器錯誤：{ex.Message}"));
            }
        }

        /// <summary>
        /// 前台：查詢產品詳細
        /// </summary>
        /// <param name="id">商品編號</param>
        /// <param name="ct">連線</param>
        /// <returns></returns>
        [AllowAnonymous]  // 不用 JWT，前台也能看
        [HttpGet("{productId:int}")]
        public async Task<IActionResult> GetProductDetail(
            int productId, int? skuId, CancellationToken ct = default)
        {
            try
            {
                var result = await _service.GetFrontProductListAsync(productId, skuId, ct);

                if (result == null)
                    return NotFound(ApiResponse<string>.Fail("找不到商品"));

                return Ok(ApiResponse<ProdProductDetailDto>.Ok(result));
            }
            catch (Exception ex)
            {
                // 捕捉異常（例如 SQL 錯誤、參數錯誤）
                return StatusCode(500, ApiResponse<string>.Fail($"伺服器錯誤：{ex.Message}"));
            }
        }

        /// <summary>
        /// 前台：查詢產品分類
        /// </summary>
        /// <returns></returns>
        [HttpGet("ProductTypetree/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductTypeTree(int? id,
            CancellationToken ct = default)
        {
            try
            {
                var data = await _service.GetProductTypeTreeAsync(id, ct);
                return Ok(ApiResponse<IEnumerable<ProductTypeTreeDto>>.Ok(data));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail("查詢商品分類時發生錯誤：" + ex.Message));
            }
        }

        /// <summary>
        /// 加入購物車（以 Sku 為主）
        /// </summary>
        [AllowAnonymous]
        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddShoppingCartAsync([FromBody] AddToCartDto dto, CancellationToken ct = default)
        {
            try
            {
                if (dto == null)
                    return BadRequest(ApiResponse<string>.Fail("參數錯誤：Body 為空"));

                var cartId = await _service.AddShoppingCartAsync(dto, ct);

                if (cartId <= 0)
                    return NotFound(ApiResponse<string>.Fail("找不到商品或加入失敗"));

                return Ok(ApiResponse<int>.Ok(cartId, "商品已成功加入購物車"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail($"伺服器錯誤：{ex.Message}"));
            }
        }

        /// <summary>
        /// 取得購物車即時數量
        /// </summary>
        [AllowAnonymous]
        [HttpGet("get-summary-cart")]
        public async Task<IActionResult> GetCartSummaryAsync([FromQuery] int? userNumberId, [FromQuery] string? sessionId, CancellationToken ct = default)
        {
            try
            {
                var result = await _service.GetCartSummaryAsync(userNumberId, sessionId, ct);

                return Ok(ApiResponse<dynamic>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail($"伺服器錯誤：{ex.Message}"));
            }
        }

        /// <summary>
        /// 取得所有啟用品牌（前台用）
        /// </summary>
        [AllowAnonymous]
        [HttpGet("get-brands-all")]
        public async Task<IActionResult> GetBrandsAll()
        {
            try
            {
                var result = await _service.GetBrandsAll();
                return Ok(ApiResponse<List<SupBrandsDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail($"伺服器錯誤：{ex.Message}"));
            }
        }

        /// <summary>
        /// 搜尋品牌名稱（關鍵字）
        /// </summary>
        [AllowAnonymous]
        [HttpGet("search-brands")]
        public async Task<IActionResult> SearchBrands([FromQuery] string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return BadRequest(ApiResponse<string>.Fail("搜尋關鍵字不可為空"));

                var result = await _service.SearchBrands(keyword);
                return Ok(ApiResponse<List<SupBrandsDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail($"伺服器錯誤：{ex.Message}"));
            }
        }

        /// <summary>
        /// 取得屬性
        /// </summary>
        [AllowAnonymous]
        [HttpGet("get-att")]
        public async Task<IActionResult> GetFilterAttributes(CancellationToken ct = default)
        {
            try
            {
                var result = await _service.GetFilterAttributesAsync(ct);
                return Ok(ApiResponse<List<AttributeWithOptionsDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail($"伺服器錯誤：{ex.Message}"));
            }
        }

		/// <summary>
		/// 檢查目前使用者是否對指定商品按讚過（未登入者回傳 false）
		/// </summary>
		[HttpGet("check/{productId:int}")]
		[AllowAnonymous] // 前台可匿名呼叫
		public async Task<IActionResult> CheckLikeStatus(int productId, CancellationToken ct)
		{
			if (productId <= 0)
				return BadRequest(ApiResponse<string>.Fail("商品 ID 錯誤"));

			try
			{
				var numberId = await GetCurrentUserNumberIdAsync();
				var isLiked = await _service.HasUserLikedProductAsync(numberId, productId, ct);
				return Ok(new { isLiked });
			}
			catch (UnauthorizedAccessException)
			{
				// 未登入的使用者不報錯，直接回傳 false
				return Ok(new { isLiked = false });
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<string>.Fail($"檢查按讚狀態時發生錯誤：{ex.Message}"));
			}
		}

		/// <summary>
		/// 按讚 / 取消讚（僅登入者可操作）
		/// </summary>
		[HttpPost("toggle")]
		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] 
		public async Task<IActionResult> ToggleLike([FromBody] ToggleLikeDto dto, CancellationToken ct)
		{
			if (dto == null || dto.ProductId <= 0)
				return BadRequest(ApiResponse<string>.Fail("商品 ID 錯誤"));

			try
			{
				var numberId = await GetCurrentUserNumberIdAsync();
				var result = await _service.ToggleLikeAsync(numberId, dto.ProductId, ct);

				return Ok(ApiResponse<object>.Ok(new
				{
					isLiked = result.IsLiked,
					message = result.Message
				}));
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(ApiResponse<string>.Fail("請先登入再進行按讚操作"));
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<string>.Fail($"按讚操作發生錯誤：{ex.Message}"));
			}
		}

		/// <summary>
		/// 取得現有使用者
		/// </summary>
		/// <returns></returns>
		/// <exception cref="UnauthorizedAccessException"></exception>
		/// <exception cref="KeyNotFoundException"></exception>
		private async Task<int> GetCurrentUserNumberIdAsync()
		{
			var userId = _userMgr.GetUserId(User);
			if (string.IsNullOrEmpty(userId))
				throw new UnauthorizedAccessException("未登入");

			var numberId = await _appDb.Users.AsNoTracking()
				.Where(u => u.Id == userId)
				.Select(u => u.UserNumberId)
				.FirstOrDefaultAsync();

			if (numberId == 0)
				throw new KeyNotFoundException("找不到使用者或 UserNumberId 無效");

			return numberId;
		}
	}
}
