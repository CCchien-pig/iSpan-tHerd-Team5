using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.Services.SUP;
using tHerdBackend.Core.ValueObjects;
using tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	[ApiController]
	[Route("api/[folder]/[controller]")]
	public class BrandsController : ControllerBase
	{
		private readonly IBrandService _service;// 處理品牌基本資料
		private readonly IBrandLayoutService _layoutService; // 處理品牌版面配置
		private readonly ICurrentUser _me;

		public BrandsController(
			IBrandService service,
			IBrandLayoutService layoutService,
			ICurrentUser me)
		{
			_service = service;
			_layoutService = layoutService;
			_me = me;
		}

		#region 查品牌
		/// <summary>
		/// 取得所有品牌清單（包含未啟用者）。
		/// </summary>
		// GET /api/sup/Brands
		//[HttpGet]
		//public async Task<IActionResult> GetAllBrands()
		//{
		//	try
		//	{
		//		var list = await _service.GetAllAsync();
		//		return Ok(list);
		//	}
		//	catch (Exception ex)
		//	{
		//		return StatusCode(500, new { success = false, message = ex.Message });
		//	}
		//}

		/// <summary>
		/// 取得品牌清單，可依條件篩選：
		/// - isActive=true/false → 篩選啟用或未啟用品牌
		/// - isDiscountActive=true/false → 篩選有折扣或無折扣品牌
		/// - isFeatured=true/false → 篩選精選或非精選品牌
		/// 未傳入 (null) 則不篩選該條件
		/// </summary>
		/// <param name="isActive">品牌啟用狀態</param>
		/// <param name="isDiscountActive">品牌折扣狀態</param>
		/// <param name="isFeatured">品牌精選狀態</param>		
		// GET /api/sup/Brands?isActive=true&isDiscountActive=false&isFeatured=true
		[HttpGet]
		public async Task<IActionResult> GetBrands(
		bool? isActive = null,
		bool? isDiscountActive = null,
		bool? isFeatured = null)
		{
			try
			{
				var brands = await _service.GetFilteredAsync(isActive, isDiscountActive, isFeatured);

				if (brands == null || !brands.Any())
				{
					var messages = new List<string>();
					if (isActive.HasValue) messages.Add(isActive.Value ? "品牌啟用中" : "品牌未啟用");
					if (isDiscountActive.HasValue) messages.Add(isDiscountActive.Value ? "折扣活動中" : "無折扣活動");
					if (isFeatured.HasValue) messages.Add(isFeatured.Value ? "品牌精選中" : "品牌非精選");

					string message = messages.Any()
						? $"沒有符合條件的品牌（{string.Join("、", messages)}）"
						: "沒有品牌資料";

					return NotFound(new { success = false, message });
				}

				return Ok(brands);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}


		/// <summary>
		/// 查單一品牌
		/// </summary>
		/// <param name="id">品牌編號</param>
		// GET /api/sup/Brands/5  
		[HttpGet("{id}")]
		public async Task<IActionResult> GetByBrandId(int id)
		{
			try
			{
				var brand = await _service.GetByIdAsync(id);
				if (brand == null)
					return NotFound(new { success = false, message = "找不到該品牌" });
				return Ok(brand);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		#endregion

		#region 查品牌折扣

		/// <summary>
		/// 查詢所有品牌的折扣資料
		/// </summary>
		/// <returns>所有品牌折扣資訊及期間</returns>
		[HttpGet("discounts")]
		public async Task<ActionResult<ApiResponse<List<BrandDiscountDto>>>> GetAllBrandDiscounts()
		{
			try
			{
				var list = await _service.GetAllDiscountsAsync();
				return Ok(ApiResponse<List<BrandDiscountDto>>.Ok(list));
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<List<BrandDiscountDto>>.Fail("查詢品牌折扣失敗：" + ex.Message));
			}
		}

		/// <summary>
		/// 依品牌ID查詢該品牌的折扣資料（可匿名）
		/// </summary>
		/// <param name="brandId">品牌ID</param>
		/// <returns>ApiResponse: 品牌折扣資訊</returns>
		[HttpGet("discount/bybrand/{brandId}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetDiscountByBrandId(int brandId)
		{
			try
			{
				// 檢查資料庫中是否存在該品牌ID
				var brandExists = await _service.CheckBrandExistsAsync(brandId);
				if (!brandExists)
				{
					return Ok(ApiResponse<BrandDiscountDto>.Fail("找不到該品牌ID"));
				}

				var discount = await _service.GetDiscountByBrandIdAsync(brandId);

				// 若查不到資料（品牌存在但沒折扣）
				if (discount == null || discount.DiscountRate is null)
				{
					return Ok(ApiResponse<BrandDiscountDto>.Fail("沒有找到該品牌的折扣資料"));
				}

				return Ok(ApiResponse<BrandDiscountDto>.Ok(discount, "查詢成功"));
			}
			catch (Exception ex)
			{
				// 統一回傳失敗格式
				return Ok(ApiResponse<BrandDiscountDto>.Fail("系統錯誤：" + ex.Message));
			}
		}

		#endregion

		#region 查品牌按讚數

		/// <summary>
		/// 取得指定品牌的按讚數。
		/// </summary>
		/// <param name="id">品牌編號</param>
		/// GET /api/sup/Brands/LikeCount/5
		[HttpGet("LikeCount/{id}")]
		public async Task<IActionResult> GetBrandLikeCount(int id)
		{
			try
			{
				var likeCount = await _service.GetLikeCountAsync(id);
				if (likeCount == null)
					return NotFound(new { success = false, message = "找不到該品牌" });
				return Ok(new { BrandId = id, LikeCount = likeCount });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		#endregion

		#region 品牌版面 layouts API
		//查詢所有版面	GET		/api/brands/{brandId}/layouts	取得特定品牌的所有歷史版面設定（含版本與啟用狀態）
		//取得啟用版型	GET		/api/brands/{brandId}/layout/active	取得目前啟用中的 Layout（IsActive = 1）
		//新增版型		POST	/api/brands/{brandId}/layout	建立新的 JSON 版型設定
		//更新版型		PUT		/api/brands/layouts/{layoutId}	覆寫版面內容
		//啟用指定版型	PATCH	/api/brands/layouts/{layoutId}/activate	設定為啟用版本並停用其他同品牌版型
		//刪除版型		DELETE	/api/brands/layouts/{layoutId}	移除版面記錄（軟刪，停止啟用IsActive = 0）

		/// <summary>
		/// 取得特定品牌的所有歷史版面設定（含版本與啟用狀態）
		/// </summary>
		/// GET /api/sup/brands/{brandId}/layouts
		[HttpGet("{brandId}/layouts")]
		public async Task<IActionResult> GetBrandLayouts(int brandId)
		{
			try
			{
				var brand = await _service.GetByIdAsync(brandId);
				if (brand == null)
					// 找不到品牌
					return NotFound(new { success = false, message = $"找不到 ID 為 {brandId} 的品牌紀錄。" });

				var layouts = await _layoutService.GetLayoutsByBrandIdAsync(brandId);
				if (layouts == null || !layouts.Any())
					// 找不到版面紀錄
					return NotFound(new { success = false, message = "該品牌尚未建立任何版面設定紀錄。" });

				return Ok(layouts);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = $"伺服器錯誤: {ex.Message}" });
			}
		}

		/// <summary>
		/// 取得目前啟用中的品牌 Layout（IsActive = 1）
		/// </summary>
		/// GET /api/sup/brands/{brandId}/layout/active
		[HttpGet("{brandId}/layout/active")]
		public async Task<IActionResult> GetActiveLayout(int brandId)
		{
			try
			{
				var brand = await _service.GetByIdAsync(brandId);
				if (brand == null)
					// 找不到品牌
					return NotFound(new { success = false, message = $"找不到 ID 為 {brandId} 的品牌紀錄。" });

				var layout = await _layoutService.GetActiveLayoutAsync(brandId);
				if (layout == null)
					// 找不到啟用中的版面
					return NotFound(new { success = false, message = "該品牌目前沒有任何啟用中的版面設定。" });

				return Ok(layout);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = $"伺服器錯誤: {ex.Message}" });
			}
		}

		/// <summary>
		/// 建立新的品牌 Layout JSON 設定
		/// </summary>
		/// POST /api/sup/brands/{brandId}/layout
		[HttpPost("{brandId}/layout")]
		public async Task<IActionResult> CreateBrandLayout(int brandId, [FromBody] BrandLayoutCreateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(new { success = false, message = "輸入格式不正確，請檢查所有欄位。" }); // 稍微優化BadRequest

			if (!_me.IsAuthenticated)
				return Unauthorized(new { success = false, message = "使用者尚未登入，無法執行建立操作。" }); // 稍微優化Unauthorized

			// TODO:登入ID
			var creatorId = _me.IsAuthenticated ? _me.UserNumberId : 1004;
			dto.Creator = creatorId;

			try
			{
				var brand = await _service.GetByIdAsync(brandId);
				if (brand == null)
					// 找不到品牌
					return NotFound(new { success = false, message = $"找不到 ID 為 {brandId} 的品牌紀錄，無法新增版面。" });

				var newId = await _layoutService.CreateLayoutAsync(brandId, dto);

				// 成功後應回傳 201 Created，並導向取得該資源的 API
				return CreatedAtAction(nameof(GetActiveLayout), new { brandId = brandId },
					new { success = true, layoutId = newId, message = "品牌版面設定已成功建立。" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = $"執行建立操作時發生伺服器錯誤: {ex.Message}" });
			}
		}

		/// <summary>
		/// 修改品牌版面設定（整體覆寫 LayoutJson）
		/// </summary>
		/// PUT /api/sup/brands/layouts/{layoutId}
		[HttpPut("layouts/{layoutId}")]
		public async Task<IActionResult> UpdateBrandLayout(int layoutId, [FromBody] BrandLayoutUpdateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(new { success = false, message = "輸入資料錯誤，請檢查所有欄位。" });

			if (!_me.IsAuthenticated)
				return Unauthorized(new { success = false, message = "使用者尚未登入，無法執行更新操作。" });

			// TODO:登入ID
			var reviserId = _me.IsAuthenticated ? _me.UserNumberId : 1004;
			dto.Reviser = reviserId;

			try
			{
				var updated = await _layoutService.UpdateLayoutAsync(layoutId, dto);
				if (!updated)
					// 找不到 Layout
					return NotFound(new { success = false, message = $"找不到 ID 為 {layoutId} 的品牌版面配置，更新失敗。" });

				return Ok(new { success = true, message = "品牌版面設定已成功更新。" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = $"執行更新操作時發生伺服器錯誤: {ex.Message}" });
			}
		}

		/// <summary>
		/// 啟用指定版型（同品牌僅允許一個 Layout 為啟用狀態）
		/// </summary>
		/// PATCH /api/sup/brands/layouts/{layoutId}/activate
		[HttpPatch("layouts/{layoutId}/activate")]
		[AllowAnonymous] // ← TODO:暫時允許匿名訪問
		public async Task<IActionResult> ActivateBrandLayout(int layoutId)
		{
			//if (!_me.IsAuthenticated)
			//	return Unauthorized(new { success = false, message = "使用者尚未登入" });

			// TODO:暫時用登入ID
			var reviserId = 1004;

			try
			{
				var result = await _layoutService.ActivateLayoutAsync(layoutId, reviserId);
				if (!result)
					// 找不到 Layout
					return NotFound(new { success = false, message = $"找不到指定的版面配置 (Layout ID: {layoutId})。" });

				return Ok(new { success = true, message = "品牌版面設定已成功啟用為現行版本。" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = $"執行啟用操作時發生伺服器錯誤: {ex.Message}" });
			}
		}

		/// <summary>
		/// 軟刪除（停用）品牌 Layout
		/// </summary>
		/// DELETE /api/sup/brands/layouts/{layoutId}
		[HttpDelete("layouts/{layoutId}")]
		[AllowAnonymous] // ← TODO:暫時允許匿名訪問
		public async Task<IActionResult> DeleteBrandLayout(int layoutId)
		{
			//if (!_me.IsAuthenticated)
			//	return Unauthorized(new { success = false, message = "使用者尚未登入" });

			// TODO:暫時用登入ID
			var reviserId = 1004;

			try
			{
				//var result = await _service.SoftDeleteLayoutAsync(layoutId, _me.UserNumberId);
				var result = await _layoutService.SoftDeleteLayoutAsync(layoutId, reviserId);
				if (!result)
					return NotFound(new { success = false, message = "找不到指定的品牌版面配置 (Layout ID: " + layoutId + ")" });

				return Ok(new { success = true, message = "品牌版面配置已成功停用。" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "執行停用操作時發生伺服器錯誤: " + ex.Message });
			}
		}

		#endregion
	}
}
