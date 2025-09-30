using FlexBackend.Core.Abstractions;
using FlexBackend.Core.DTOs.SUP;
using FlexBackend.Core.DTOs.USER;
using FlexBackend.Infra.Models;
using FlexBackend.SUP.Rcl.Areas.SUP.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.SUP.Rcl.Areas.SUP.Controllers
{
	[Area("SUP")]
	public class LogisticsController : Controller
	{
		private readonly tHerdDBContext _context;
		private readonly ICurrentUser _me;
		private readonly UserManager<ApplicationUser> _userMgr;

		public LogisticsController(
			tHerdDBContext context,
			ICurrentUser me,
			UserManager<ApplicationUser> userMgr)
		{
			_context = context;
			_me = me;
			_userMgr = userMgr;
		}

		// GET: /SUP/Logistics/Index
		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

		// POST: /SUP/Logistics/IndexJson
		[HttpPost("SUP/Logistics/IndexJson")]
		public async Task<IActionResult> IndexJson()
		{
			var draw = Request.Form["draw"].FirstOrDefault();
			var start = Request.Form["start"].FirstOrDefault();
			var length = Request.Form["length"].FirstOrDefault();
			var searchValue = Request.Form["search[value]"].FirstOrDefault();

			int pageSize = length != null ? Convert.ToInt32(length) : 10;
			int skip = start != null ? Convert.ToInt32(start) : 0;

			var query = _context.SupLogistics.AsQueryable();

			// 搜尋 (物流商名稱 / 配送方式)
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(l =>
					l.LogisticsName.Contains(searchValue) ||
					l.ShippingMethod.Contains(searchValue));
			}

			int recordsTotal = await query.CountAsync();

			var data = await query
				.OrderBy(l => l.LogisticsId)
				.Skip(skip)
				.Take(pageSize)
				.Select(l => new
				{
					logisticsId = l.LogisticsId,
					logisticsName = l.LogisticsName,
					shippingMethod = l.ShippingMethod,
					isActive = l.IsActive
				})
				.ToListAsync();

			return Json(new
			{
				draw = draw,
				recordsFiltered = recordsTotal,
				recordsTotal = recordsTotal,
				data = data
			});
		}

		// 子表展開
		// GET: /SUP/Logistics/GetByLogisticsId
		[HttpGet]
		public async Task<IActionResult> GetByLogisticsId(int id)
		{
			try
			{
				var rates = await _context.SupLogisticsRates
					.Where(r => r.LogisticsId == id)
					.OrderBy(r => r.WeightMin)
					.Select(r => new LogisticsRateDto
					{
						LogisticsRateId = r.LogisticsRateId,
						LogisticsId = r.LogisticsId,
						WeightMin = r.WeightMin,
						WeightMax = r.WeightMax,
						ShippingFee = r.ShippingFee,
						IsActive = r.IsActive
					})
					.ToListAsync();

				return Json(rates);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
			}
		}


		// GET: /SUP/Logistics/GetLastWeightMax
		[HttpGet]
		public IActionResult GetLastWeightMax(int logisticsId)
		{
			var last = _context.SupLogisticsRates
				.Where(r => r.LogisticsId == logisticsId)
				.OrderByDescending(r => r.WeightMax ?? decimal.MaxValue)
				.FirstOrDefault();

			return Json(last?.WeightMax);
		}


		// POST: /SUP/Logistics/Create
		[HttpPost]
		public async Task<IActionResult> Create([FromForm] LogisticsRateModel model)
		{
			if (model.WeightMax <= model.WeightMin)
				return Json(new { success = false, message = "最大重量必須大於最小重量" });

			var userId = _me.Id;
			var user = await _userMgr.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
				return Json(new { success = false, message = "找不到使用者資料" });

			int currentUserId = user.UserNumberId;

			var rate = new SupLogisticsRate
			{
				LogisticsId = model.LogisticsId,
				WeightMin = model.WeightMin,
				WeightMax = model.WeightMax,
				ShippingFee = model.ShippingFee,
				IsActive = model.IsActive,
				Reviser = currentUserId,
				RevisedDate = DateTime.Now
			};
			_context.SupLogisticsRates.Add(rate);
			await _context.SaveChangesAsync();
			return Json(new { success = true });
		}


		// POST: /SUP/Logistics/Delete
		[HttpPost]
		public IActionResult Delete(int id)
		{
			var rate = _context.SupLogisticsRates.FirstOrDefault(r => r.LogisticsRateId == id);
			if (rate == null)
				return Json(new { success = false, message = "找不到資料" });
			_context.SupLogisticsRates.Remove(rate);
			_context.SaveChanges();
			return Json(new { success = true });
		}


		// 切換物流商啟用狀態
		// POST: /SUP/Logistics/ToggleActive
		[HttpPost]
		public async Task<IActionResult> ToggleActive(int id, bool isActive)
		{
			try
			{
				var logEntity = await _context.SupLogistics.FindAsync(id);
				if (logEntity == null)
					return Json(new { success = false, message = "找不到該物流商" });

				var userId = _me.Id;
				var user = await _userMgr.Users
					.AsNoTracking()
					.FirstOrDefaultAsync(u => u.Id == userId);

				if (user == null)
					return Json(new { success = false, message = "找不到使用者資料" });

				int currentUserId = user.UserNumberId;

				logEntity.IsActive = isActive;
				logEntity.Reviser = currentUserId;
				logEntity.RevisedDate = DateTime.Now;

				await _context.SaveChangesAsync();
				return Json(new { success = true, newStatus = isActive });
			}
			catch (DbUpdateException dbEx)
			{
				return Json(new { success = false, message = "資料庫更新失敗: " + dbEx.Message });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}

		// 切換子表運費區間的啟用狀態
		// POST: /SUP/Logistics/ToggleRateActive
		[HttpPost]
		public async Task<IActionResult> ToggleRateActive(int id, bool isActive)
		{
			var rate = await _context.SupLogisticsRates.FindAsync(id);
			if (rate == null)
				return Json(new { success = false, message = "找不到該運費區間" });

			var userId = _me.Id;
			var user = await _userMgr.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
				return Json(new { success = false, message = "找不到使用者資料" });

			int currentUserId = user.UserNumberId;

			rate.IsActive = isActive;
			rate.Reviser = currentUserId;
			rate.RevisedDate = DateTime.Now;

			await _context.SaveChangesAsync();
			return Json(new { success = true, newStatus = isActive });
		}
	}
}
