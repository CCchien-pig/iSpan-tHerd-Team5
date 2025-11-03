using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP.Brand;
using tHerdBackend.Core.DTOs.SUP.BrandLayout;
using tHerdBackend.Core.Interfaces.SUP;
using tHerdBackend.Core.Services.SUP;
using tHerdBackend.Core.ValueObjects;
using tHerdBackend.Infra.Models.Sup;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	[ApiController]
	[Route("api/[folder]/[controller]")]
	[Authorize]
	public class BrandsController : ControllerBase
	{
		private readonly IBrandService _service;// 處理品牌基本資料
		private readonly IBrandLayoutService _layoutService; // 處理品牌版面配置
		private readonly IBrandLogoService _brandLogoService;
		private readonly ICurrentUser _me;
		private readonly ILogger<BrandsController> _logger;

		public BrandsController(
			IBrandService service,
			IBrandLayoutService layoutService,
			IBrandLogoService brandLogoService,
			ICurrentUser me,
			ILogger<BrandsController> logger)
		{
			_service = service;
			_layoutService = layoutService;
			_brandLogoService = brandLogoService;
			_me = me;
			_logger = logger;
		}

		#region 查品牌

		/// <summary>
		/// 取得品牌清單，可依條件篩選：
		/// - isActive=true/false → 篩選啟用或未啟用品牌
		/// - isDiscountActive=true/false → 篩選有折扣或無折扣品牌
		/// - isFeatured=true/false → 篩選精選或非精選品牌
		/// 未傳入 (null) 則不篩選該條件
		/// </summary>
		// GET /api/sup/Brands
		[HttpGet]
		[AllowAnonymous] // 此查詢功能通常前後台都會用到，可允許匿名存取
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
					// 為了前端處理方便，即使找不到資料也回傳 HTTP 200 與空陣列
					return Ok(new List<BrandDto>());
				}

				return Ok(brands);
			}
			catch (Exception ex)
			{
				// 記錄日誌 (Log) 的好地方
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		/// <summary>
		/// 查單一品牌
		/// </summary>
		/// <param name="id">品牌編號</param>
		// GET /api/sup/Brands/{id}
		[HttpGet("{id}")]
		[AllowAnonymous] // 同樣，查詢單一品牌也可能用於前台
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

		/// <summary>
		/// 依品牌名稱首字母分組的品牌清單 (可依條件篩選)，回傳分組列表
		/// 給 Brands A–Z 頁使用
		/// </summary>
		// GET /api/sup/Brands/grouped
		[HttpGet("grouped")]
		[AllowAnonymous]
		public async Task<IActionResult> GetBrandsGroupedByFirstLetter(
			bool? isActive = null,
			bool? isDiscountActive = null,
			bool? isFeatured = null,
			CancellationToken ct = default)
		{
			try
			{
				var brands = await _service.GetFilteredAsync(isActive, isDiscountActive, isFeatured);
				if (brands == null || !brands.Any())
					return Ok(ApiResponse<List<BrandGroupDto>>.Ok(new List<BrandGroupDto>(), "無符合條件的品牌"));

				// 建立 Logo 快取
				await _brandLogoService.BuildLogoMapAsync(ct);

				string GetGroupKey(string name)
				{
					if (string.IsNullOrWhiteSpace(name)) return "0-9";
					var ch = name.Trim()[0];
					if (char.IsDigit(ch)) return "0-9";
					var upper = char.ToUpper(ch);
					return upper >= 'A' && upper <= 'Z' ? upper.ToString() : "0-9";
				}

				var dict = brands
					.GroupBy(b => GetGroupKey(b.BrandName))
					.ToDictionary(
						g => g.Key,
						g => g.OrderBy(b => b.BrandName)
							.Select(b => new BrandGroupItemDto
							{
								BrandId = b.BrandId,
								BrandName = b.BrandName,
								BrandCode = b.BrandCode,
								IsActive = b.IsActive,
								IsFeatured = b.IsFeatured,
								DiscountRate = b.DiscountRate,
								IsDiscountActive = b.IsDiscountActive,
								LogoUrl = _brandLogoService.TryResolve(b.BrandName, b.BrandCode)
							}).ToList()
					);

				var result = new List<BrandGroupDto>
		{
			new() { Letter = "0-9", Brands = dict.ContainsKey("0-9") ? dict["0-9"] : new() }
		};

				for (char c = 'A'; c <= 'Z'; c++)
				{
					var key = c.ToString();
					result.Add(new BrandGroupDto
					{
						Letter = key,
						Brands = dict.ContainsKey(key) ? dict[key] : new()
					});
				}

				return Ok(ApiResponse<List<BrandGroupDto>>.Ok(result, "品牌分組載入成功"));
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<List<BrandGroupDto>>.Fail($"品牌分組載入失敗：{ex.Message}"));
			}
		}

		/// <summary>
		/// 取得啟用中品牌 Logo URL
		/// 從 SYS_AssetFile 表中找出 FolderId = 56 且 IsActive = 1 的圖片，回傳 { brandName, logoUrl } 清單。
		/// </summary>
		// GET /api/sup/Brands/logos
		[HttpGet("logos")]
		[AllowAnonymous]
		public async Task<IActionResult> GetActiveBrandLogos(CancellationToken ct)
		{
			try
			{
				var logos = await _brandLogoService.BuildLogoMapAsync(ct);
				if (logos == null || logos.Count == 0)
					return Ok(ApiResponse<object>.Ok(new List<object>(), "目前沒有啟用中的品牌 Logo"));

				var result = logos.Select(kv => new
				{
					BrandName = kv.Key,
					LogoUrl = kv.Value
				}).ToList();

				return Ok(ApiResponse<object>.Ok(result, "成功取得啟用中品牌 Logo"));
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<object>.Fail($"取得品牌 Logo 發生錯誤：{ex.Message}"));
			}
		}


		/// <summary>
		/// 取得精選品牌清單 (平面列表)，給 Brands A–Z 或首頁 Carousel 使用
		/// IsActive = true 且 IsFeatured = true 的品牌並透過 _brandLogoService 補上 logoUrl
		/// </summary>
		// GET /api/sup/Brands/featured
		[HttpGet("featured")]
		[AllowAnonymous]
		public async Task<IActionResult> GetFeaturedBrands(CancellationToken ct = default)
		{
			try
			{
				var brands = await _service.GetFilteredAsync(isActive: true, isDiscountActive: null, isFeatured: true);
				if (brands == null || !brands.Any())
					return Ok(ApiResponse<List<object>>.Ok(new List<object>(), "目前沒有精選品牌"));

				// 先建立 Logo 快取
				await _brandLogoService.BuildLogoMapAsync(ct);

				// 將品牌轉為前端使用格式
				var result = brands
					.OrderBy(b => b.BrandName)
					.Select(b => new
					{
						brandId = b.BrandId,
						brandName = b.BrandName,
						brandCode = b.BrandCode,
						isActive = b.IsActive,
						isFeatured = b.IsFeatured,
						discountRate = b.DiscountRate,
						isDiscountActive = b.IsDiscountActive,
						logoUrl = _brandLogoService.TryResolve(b.BrandName, b.BrandCode)
					})
					.ToList();

				return Ok(ApiResponse<object>.Ok(result, "成功取得精選品牌"));
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<object>.Fail($"取得精選品牌時發生錯誤：{ex.Message}"));
			}
		}

		#endregion

		#region 查品牌折扣

		/// <summary>
		/// 查詢所有品牌的折扣資料
		/// </summary>
		[HttpGet("discounts")]
		[AllowAnonymous]
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
		/// 依品牌ID查詢該品牌的折扣資料
		/// </summary>
		[HttpGet("discount/bybrand/{brandId}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetDiscountByBrandId(int brandId)
		{
			try
			{
				var brandExists = await _service.CheckBrandExistsAsync(brandId);
				if (!brandExists)
				{
					return Ok(ApiResponse<BrandDiscountDto>.Fail("找不到該品牌ID"));
				}

				var discount = await _service.GetDiscountByBrandIdAsync(brandId);
				if (discount == null || discount.DiscountRate is null)
				{
					return Ok(ApiResponse<BrandDiscountDto>.Fail("沒有找到該品牌的折扣資料"));
				}

				return Ok(ApiResponse<BrandDiscountDto>.Ok(discount, "查詢成功"));
			}
			catch (Exception ex)
			{
				return Ok(ApiResponse<BrandDiscountDto>.Fail("系統錯誤：" + ex.Message));
			}
		}

		#endregion

		#region 查品牌按讚數

		/// <summary>
		/// 取得指定品牌的按讚數。
		/// </summary>
		[HttpGet("LikeCount/{id}")]
		[AllowAnonymous]
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
		[HttpGet("{brandId}/layouts")]
		public async Task<IActionResult> GetBrandLayouts(int brandId)
		{
			try
			{
				var layouts = await _layoutService.GetLayoutsByBrandIdAsync(brandId);
				if (layouts == null || !layouts.Any())
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
		[HttpGet("{brandId}/layout/active")]
		[AllowAnonymous] // 前台顯示頁面需要，允許匿名
		public async Task<IActionResult> GetActiveLayout(int brandId)
		{
			try
			{
				var layout = await _layoutService.GetActiveLayoutAsync(brandId);
				if (layout == null)
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
		[HttpPost("{brandId}/layout")]
		public async Task<IActionResult> CreateBrandLayout(int brandId, [FromBody] BrandLayoutCreateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(new { success = false, message = "輸入格式不正確，請檢查所有欄位。" });

			// 2. 直接使用 _me.UserNumberId，不再需要判斷或使用硬編碼
			dto.Creator = _me.UserNumberId;

			try
			{
				var brand = await _service.GetByIdAsync(brandId);
				if (brand == null)
					return NotFound(new { success = false, message = $"找不到 ID 為 {brandId} 的品牌紀錄，無法新增版面。" });

				var newId = await _layoutService.CreateLayoutAsync(brandId, dto);

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
		[HttpPut("layouts/{layoutId}")]
		public async Task<IActionResult> UpdateBrandLayout(int layoutId, [FromBody] BrandLayoutUpdateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(new { success = false, message = "輸入資料錯誤，請檢查所有欄位。" });

			// 2. 直接使用 _me.UserNumberId
			dto.Reviser = _me.UserNumberId;

			try
			{
				var updated = await _layoutService.UpdateLayoutAsync(layoutId, dto);
				if (!updated)
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
		[HttpPatch("layouts/{layoutId}/activate")]
		public async Task<IActionResult> ActivateBrandLayout(int layoutId)
		{
			// 2. 直接使用 _me.UserNumberId
			var reviserId = _me.UserNumberId;

			try
			{
				var result = await _layoutService.ActivateLayoutAsync(layoutId, reviserId);
				if (!result)
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
		[HttpDelete("layouts/{layoutId}")]
		public async Task<IActionResult> DeleteBrandLayout(int layoutId)
		{
			// 2. 直接使用 _me.UserNumberId
			var reviserId = _me.UserNumberId;

			try
			{
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

		#region 前台查詳情

		/// <summary>
		/// 取得品牌詳頁（Banner、分類按鈕、Accordion）
		/// </summary>
		/// <param name="brandId">品牌 Id</param>
		/// <param name="ct">取消權杖</param>
		/// <returns>BrandDetailDto</returns>
		/// <remarks>
		/// 資料來源：
		/// - Banner：SUP_Brand.ImgId → SYS_AssetFile.FileUrl
		/// - Buttons：SUP_BrandProductTypeFilter（依 ButtonOrder 排序，回傳 Text/Order/Id）
		/// - Accordions：SUP_BrandAccordionContent（以 Content 分組，組內依 OrderSeq 排序）
		/// </remarks>
		[HttpGet("{brandId:int}/detail")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(ApiResponse<BrandDetailDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> GetDetail([FromRoute] int brandId, CancellationToken ct)
		{
			try
			{
				var dto = await _service.GetBrandDetailAsync(brandId, ct);
				if (dto == null)
					return Ok(ApiResponse<BrandDetailDto>.Fail("品牌不存在或已停用"));

				return Ok(ApiResponse<BrandDetailDto>.Ok(dto));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetDetail failed. brandId={BrandId}", brandId);
				return StatusCode(StatusCodes.Status500InternalServerError,
					ApiResponse<BrandDetailDto>.Fail($"發生錯誤：{ex.Message}"));
			}
		}

		#endregion

	}
}