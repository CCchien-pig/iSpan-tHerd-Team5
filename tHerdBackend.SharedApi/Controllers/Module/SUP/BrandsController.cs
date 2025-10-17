using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	[ApiController]
	[Route("api/[folder]/[controller]")]
	public class BrandsController : ControllerBase
	{
		private readonly IBrandService _service;

		public BrandsController(IBrandService service)
		{
			_service = service;
		}

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
	}
}
