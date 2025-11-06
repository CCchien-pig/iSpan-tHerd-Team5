using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.PROD.ord;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.ValueObjects;

namespace tHerdBackend.SharedApi.Controllers.Module.PROD
{
	[ApiController]
    [Area("prod")]
    [Route("api/[area]/[controller]")]   // 統一為 /api/prod/Products
    public class ProductsController : ControllerBase
	{
		private readonly IProductsForApiService _service;  // 服務注入

		// 建構子注入 Service
		public ProductsController(IProductsForApiService service)
		{
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
    }
}
