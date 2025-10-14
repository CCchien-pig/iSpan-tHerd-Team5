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

		//GET /api/sup/Brands
		[HttpGet]
		public async Task<IActionResult> GetAllBrands()
		{
			try
			{
				var list = await _service.GetAllAsync();
				return Ok(list);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		//GET /api/sup/Brands/{id}
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

		//GET /api/sup/Brands/Active
		[HttpGet("Active")]
		public async Task<IActionResult> GetActiveBrands()
		{
			try
			{
				var brands = await _service.GetActiveAsync();
				return Ok(brands);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		//GET /api/sup/Brands/ActiveDiscount
		[HttpGet("ActiveDiscount")]
		public async Task<IActionResult> GetActiveDiscountBrands()
		{
			try
			{
				var brands = await _service.GetActiveDiscountAsync();
				return Ok(brands);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		//GET /api/sup/Brands/ActiveFeatured
		[HttpGet("ActiveFeatured")]
		public async Task<IActionResult> GetActiveFeaturedBrands()
		{
			try
			{
				var brands = await _service.GetActiveFeaturedAsync();
				return Ok(brands);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		//GET /api/sup/Brands/LikeCount/{id}
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