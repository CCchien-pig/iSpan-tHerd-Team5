using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static FlexBackend.SUP.Rcl.Areas.SUP.Controllers.ApiControllers.LogisticsRateRequestDTO;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.Controllers.ApiControllers
{
	[Area("SUP")]
	[Route("api/[area]/[controller]")]
	[ApiController]
	[AllowAnonymous]
	public class LogisticsApiController : ControllerBase
	{
		private readonly tHerdDBContext _context;
		private readonly ICurrentUser _me;
		private readonly UserManager<ApplicationUser> _userMgr;

		public LogisticsApiController(
			tHerdDBContext context,
			ICurrentUser me,
			UserManager<ApplicationUser> userMgr)
		{
			_context = context;
			_me = me;
			_userMgr = userMgr;
		}

		#region Program CORS 設定
		// CORS 設定
		//builder.Services.AddCors(options =>
		//{
		//	options.AddPolicy(
		//		name: "AllowVue",
		//		policy =>
		//		{
		//			policy
		//			.AllowAnyOrigin()
		//			//.WithOrigins("http://localhost:3000")
		//			.AllowAnyHeader()
		//			.AllowAnyMethod();
		//		}
		//	);
		//});
		// 啟用 CORS
		//app.UseCors("AllowVue");
		#endregion
		#region 物流商
		// 查全筆：GET /api/SUP/LogisticsApi
		// 查單筆：GET /api/SUP/LogisticsApi/1000
		// 新增：POST /api/SUP/LogisticsApi
		// 修改：PUT  /api/SUP/LogisticsApi/1000
		// 刪除：DELETE /api/SUP/LogisticsApi/1000

		// GET: api/SUP/LogisticsApi/GetAll
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var list = await _context.SupLogistics.AsNoTracking().ToListAsync();
				return Ok(list);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		// GET: api/SUP/LogisticsApi/1000
		[HttpGet("{id}")]
		public async Task<IActionResult> Get(int id)
		{
			try
			{
				var log = await _context.SupLogistics.AsNoTracking().FirstOrDefaultAsync(l => l.LogisticsId == id);
				if (log == null)
					return NotFound(new { success = false, message = "找不到該物流商" });
				return Ok(log);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		// POST: api/SUP/LogisticsApi
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] SupLogistic model)
		{
			try
			{
				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null)
					return BadRequest(new { success = false, message = "找不到使用者資料" });

				model.Creator = user.UserNumberId;
				model.CreatedDate = DateTime.Now;
				model.Reviser = user.UserNumberId;
				model.RevisedDate = DateTime.Now;

				_context.SupLogistics.Add(model);
				await _context.SaveChangesAsync();

				return Ok(new { success = true, isCreate = true, logistics = model });
			}
			catch (DbUpdateException dbEx)
			{
				return StatusCode(500, new { success = false, message = "資料庫更新失敗: " + dbEx.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}

		// PUT: api/SUP/LogisticsApi/1000
		[HttpPut("{id}")]
		public async Task<IActionResult> Edit(int id, [FromBody] SupLogistic vm)
		{
			try
			{
				var logEntity = await _context.SupLogistics.FindAsync(id);
				if (logEntity == null)
					return NotFound(new { success = false, message = "找不到該物流商" });

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null)
					return BadRequest(new { success = false, message = "找不到使用者資料" });

				// 更新欄位
				logEntity.LogisticsName = vm.LogisticsName;
				logEntity.ShippingMethod = vm.ShippingMethod;
				logEntity.IsActive = vm.IsActive;
				logEntity.Reviser = user.UserNumberId;
				logEntity.RevisedDate = DateTime.Now;

				await _context.SaveChangesAsync();

				return Ok(new { success = true, logistics = logEntity });
			}
			catch (DbUpdateException dbEx)
			{
				return StatusCode(500, new { success = false, message = "資料庫更新失敗: " + dbEx.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}

		// DELETE: api/SUP/LogisticsApi/1000
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				var logEntity = await _context.SupLogistics.FindAsync(id);
				if (logEntity == null)
					return NotFound(new { success = false, message = "找不到該物流商" });

				_context.SupLogistics.Remove(logEntity);
				await _context.SaveChangesAsync();

				return Ok(new { success = true, message = "刪除成功" });
			}
			catch (DbUpdateException dbEx)
			{
				return StatusCode(500, new { success = false, message = "資料庫更新失敗: " + dbEx.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}

		// PATCH: api/SUP/LogisticsApi/ToggleActive/1000
		[HttpPatch("ToggleActive/{id}")]
		public async Task<IActionResult> ToggleActive(int id, [FromBody] bool isActive)
		{
			try
			{
				var logEntity = await _context.SupLogistics.FindAsync(id);
				if (logEntity == null)
					return NotFound(new { success = false, message = "找不到該物流商" });

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null)
					return BadRequest(new { success = false, message = "找不到使用者資料" });

				logEntity.IsActive = isActive;
				logEntity.Reviser = user.UserNumberId;
				logEntity.RevisedDate = DateTime.Now;
				await _context.SaveChangesAsync();

				return Ok(new { success = true, newStatus = isActive });
			}
			catch (DbUpdateException dbEx)
			{
				return StatusCode(500, new { success = false, message = "資料庫更新失敗: " + dbEx.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}

		#endregion


		#region 子表、Rate

		// 得指定物流商所有運費分段
		// GET: api/SUP/LogisticsRateApi/bylogistics/1000
		[HttpGet("bylogistics/{logisticsId}")]
		public async Task<IActionResult> GetByLogisticsId(int logisticsId)
		{
			try
			{
				var rates = await _context.SupLogisticsRates
					.Where(r => r.LogisticsId == logisticsId)
					.OrderBy(r => r.WeightMin)
					.Select(r => new
					{
						r.LogisticsRateId,
						r.LogisticsId,
						r.WeightMin,
						r.WeightMax,
						r.ShippingFee,
						r.IsActive,
						r.RevisedDate
					})
					.ToListAsync();

				return Ok(rates);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
			}
		}

		// POST: api/SUP/LogisticsRateApi/rate
		[HttpPost("rate")]
		public async Task<IActionResult> CreateRate([FromBody] CreateLogisticsRateRequest req)
		{
			try
			{
				if (req.WeightMin < 0)
					return BadRequest(new { success = false, message = "最小重量不可小於 0" });

				if (req.WeightMax.HasValue && req.WeightMax <= req.WeightMin)
					return BadRequest(new { success = false, message = "最大重量不可小於最小重量" });

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null)
					return BadRequest(new { success = false, message = "找不到使用者資料" });

				// 取得現有區間
				var existingRates = _context.SupLogisticsRates
					.Where(r => r.LogisticsId == req.LogisticsId)
					.OrderBy(r => r.WeightMin)
					.ToList();

				var overlappingRates = existingRates
					.Where(r => (r.WeightMax ?? decimal.MaxValue) > req.WeightMin && r.WeightMin < (req.WeightMax ?? decimal.MaxValue))
					.OrderBy(r => r.WeightMin)
					.ToList();

				foreach (var rate in overlappingRates)
				{
					var rMin = rate.WeightMin;
					var rMax = rate.WeightMax ?? decimal.MaxValue;
					// 完全被覆蓋
					if (req.WeightMin <= rMin && (req.WeightMax ?? decimal.MaxValue) >= rMax)
					{
						_context.SupLogisticsRates.Remove(rate);
					}
					// 部分重疊左側
					else if (req.WeightMin <= rMin && (req.WeightMax ?? decimal.MaxValue) < rMax)
					{
						rate.WeightMin = (req.WeightMax ?? decimal.MaxValue) + 0.1M;
						rate.RevisedDate = DateTime.Now;
					}
					// 部分重疊右側
					else if (req.WeightMin > rMin && (req.WeightMax ?? decimal.MaxValue) >= rMax)
					{
						rate.WeightMax = req.WeightMin - 0.1M;
						rate.RevisedDate = DateTime.Now;
					}
					// 中間切割
					else if (req.WeightMin > rMin && (req.WeightMax ?? decimal.MaxValue) < rMax)
					{
						var newUpper = new SupLogisticsRate
						{
							LogisticsId = rate.LogisticsId,
							WeightMin = (req.WeightMax ?? decimal.MaxValue) + 0.1M,
							WeightMax = rate.WeightMax,
							ShippingFee = rate.ShippingFee,
							IsActive = rate.IsActive,
							Reviser = user.UserNumberId,
							RevisedDate = DateTime.Now
						};
						_context.SupLogisticsRates.Add(newUpper);

						rate.WeightMax = req.WeightMin - 0.1M;
						rate.RevisedDate = DateTime.Now;
					}
				}

				var newRate = new SupLogisticsRate
				{
					LogisticsId = req.LogisticsId,
					WeightMin = req.WeightMin,
					WeightMax = req.WeightMax,
					ShippingFee = req.ShippingFee,
					IsActive = req.IsActive,
					Reviser = user.UserNumberId,
					RevisedDate = DateTime.Now
				};

				_context.SupLogisticsRates.Add(newRate);

				// 檢查收尾
				var lastRate = existingRates.OrderByDescending(r => r.WeightMin).FirstOrDefault(r => !r.WeightMax.HasValue);
				if (lastRate != null && (req.WeightMax ?? decimal.MaxValue) >= lastRate.WeightMin)
				{
					lastRate.WeightMin = (req.WeightMax ?? lastRate.WeightMin) + 0.1M;
					lastRate.ShippingFee = req.ShippingFee;
					lastRate.RevisedDate = DateTime.Now;
				}

				await _context.SaveChangesAsync();

				return Ok(new { success = true, rate = newRate });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		// PUT: api/SUP/LogisticsRateApi/{logisticsRateId}
		[HttpPut("{logisticsRateId}")]
		public async Task<IActionResult> UpdateRate(int logisticsRateId, [FromBody] UpdateLogisticsRateRequest req)
		{
			try
			{
				var rate = await _context.SupLogisticsRates.FindAsync(logisticsRateId);
				if (rate == null)
					return NotFound(new { success = false, message = "找不到區間資料" });

				// 寫入新資料
				rate.WeightMin = req.WeightMin;
				rate.WeightMax = req.WeightMax;
				rate.ShippingFee = req.ShippingFee;
				rate.IsActive = req.IsActive;
				rate.RevisedDate = DateTime.Now;

				await _context.SaveChangesAsync();
				return Ok(new { success = true });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		// DELETE: api/SUP/LogisticsRateApi/{logisticsRateId}
		[HttpDelete("{logisticsRateId}")]
		public async Task<IActionResult> DeleteRate(int logisticsRateId)
		{
			try
			{
				var rate = await _context.SupLogisticsRates.FirstOrDefaultAsync(r => r.LogisticsRateId == logisticsRateId);
				if (rate == null)
					return NotFound(new { success = false, message = "找不到區間資料" });

				_context.SupLogisticsRates.Remove(rate);
				await _context.SaveChangesAsync();
				return Ok(new { success = true, message = "刪除成功" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "刪除失敗: " + ex.Message });
			}
		}

		// PATCH: api/SUP/LogisticsRateApi/toggle-active/{logisticsRateId}
		[HttpPatch("toggle-active/{logisticsRateId}")]
		public async Task<IActionResult> ToggleRateActive(int logisticsRateId, [FromBody] bool isActive)
		{
			try
			{
				var rate = await _context.SupLogisticsRates.FindAsync(logisticsRateId);
				if (rate == null)
					return NotFound(new { success = false, message = "找不到該運費區間" });

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

				if (user == null)
					return BadRequest(new { success = false, message = "找不到使用者資料" });

				int currentUserId = user.UserNumberId;

				rate.IsActive = isActive;
				rate.Reviser = currentUserId;
				rate.RevisedDate = DateTime.Now;

				await _context.SaveChangesAsync();
				return Ok(new { success = true, newStatus = isActive });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}
	}

	#endregion

}
