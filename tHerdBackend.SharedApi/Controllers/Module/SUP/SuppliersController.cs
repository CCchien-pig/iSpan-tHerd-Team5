using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Interfaces.SUP;

namespace tHerdBackend.SharedApi.Controllers.Module.SUP
{
	[ApiController]
	[Route("api/[folder]/[controller]")]
	public class SuppliersController : ControllerBase
	{
		private readonly ISupplierService _service;

		public SuppliersController(ISupplierService service)
		{
			_service = service;
		}

		/// <summary>
		/// 取得所有供應商清單
		/// URI: GET /api/SUP/Suppliers
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> GetAllSuppliers()
		{
			try
			{
				var suppliers = await _service.GetAllSuppliersAsync();
				return Ok(suppliers);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"取得所有供應商時發生錯誤: {ex.Message}");
			}
		}

		/// <summary>
		/// 取得所有啟用中的供應商清單
		/// URI: GET /api/SUP/Suppliers/active
		/// </summary>
		[HttpGet("active")]
		public async Task<IActionResult> GetActiveSuppliers()
		{
			try
			{
				var suppliers = await _service.GetActiveSuppliersAsync();
				return Ok(suppliers);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"取得啟用中供應商時發生錯誤: {ex.Message}");
			}
		}

	}

}
