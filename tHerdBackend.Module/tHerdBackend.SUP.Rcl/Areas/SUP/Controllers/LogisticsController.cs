using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.SUP;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Infra.Models;
using tHerdBackend.SUP.Rcl.Areas.SUP.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace tHerdBackend.SUP.Rcl.Areas.SUP.Controllers
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

		#region index
		// GET: /SUP/Logistics/Index
		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

		// POST: SUP/Logistics/IndexJson
		[HttpPost("SUP/Logistics/IndexJson")]
		public async Task<IActionResult> IndexJson()
		{
			// 取得 DataTables POST 參數
			var draw = Request.Form["draw"].FirstOrDefault() ?? "1";
			var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
			var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
			var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";

			// 排序資訊
			var sortColumnIndex = Convert.ToInt32(Request.Form["order[0][column]"].FirstOrDefault() ?? "0");
			var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault() ?? "asc";

			// 建立查詢
			var query = _context.SupLogistics.AsQueryable();

			// 搜尋功能（物流商名稱 / 配送方式）
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(l =>
					EF.Functions.Like(l.LogisticsName, $"%{searchValue}%") ||
					EF.Functions.Like(l.ShippingMethod, $"%{searchValue}%")
				);
			}

			// 總筆數與過濾後筆數
			var totalRecords = await _context.SupLogistics.CountAsync();
			var filteredRecords = await query.CountAsync();

			// 排序
			query = sortColumnIndex switch
			{
				0 => sortDirection == "asc"
						? query.OrderBy(l => l.RevisedDate ?? l.CreatedDate)
						: query.OrderByDescending(l => l.RevisedDate ?? l.CreatedDate),
				1 => sortDirection == "asc" ? query.OrderBy(l => l.LogisticsId) : query.OrderByDescending(l => l.LogisticsId),
				2 => sortDirection == "asc" ? query.OrderBy(l => l.LogisticsName) : query.OrderByDescending(l => l.LogisticsName),
				3 => sortDirection == "asc" ? query.OrderBy(l => l.ShippingMethod) : query.OrderByDescending(l => l.ShippingMethod),
				4 => sortDirection == "asc" ? query.OrderBy(l => l.IsActive) : query.OrderByDescending(l => l.IsActive),
				_ => query.OrderByDescending(l => l.RevisedDate ?? l.CreatedDate)
			};

			// 分頁與選取欄位
			var data = await query
				.Skip(start)
				.Take(length)
				.Select(l => new
				{
					logisticsId = l.LogisticsId,
					logisticsName = l.LogisticsName,
					shippingMethod = l.ShippingMethod,
					isActive = l.IsActive,
					sortDate = l.RevisedDate ?? l.CreatedDate
				})
				.ToListAsync();

			// 回傳給 DataTables
			return Json(new
			{
				draw,
				recordsTotal = totalRecords,
				recordsFiltered = filteredRecords,
				data
			});
		}

		#endregion

		#region 子表、Rate
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


		// POST: /SUP/Logistics/CreateRate
		[HttpPost]
		public async Task<IActionResult> CreateRate(int logisticsId, decimal weightMin, decimal? weightMax, decimal shippingFee, bool isActive = true)
		{
			try
			{
				if (weightMin < 0)
					return Json(new { success = false, message = "最小重量不可小於 0" });

				if (weightMax.HasValue && weightMax <= weightMin)
					return Json(new { success = false, message = "最大重量不可小於最小重量" });

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null) return Json(new { success = false, message = "找不到使用者資料" });

				// 取得現有區間
				var existingRates = _context.SupLogisticsRates
					.Where(r => r.LogisticsId == logisticsId)
					.OrderBy(r => r.WeightMin)
					.ToList();

				var overlappingRates = existingRates
					.Where(r => (r.WeightMax ?? decimal.MaxValue) > weightMin && r.WeightMin < (weightMax ?? decimal.MaxValue))
					.OrderBy(r => r.WeightMin)
					.ToList();

				foreach (var rate in overlappingRates)
				{
					var rMin = rate.WeightMin;
					var rMax = rate.WeightMax ?? decimal.MaxValue;

					// 完全被覆蓋
					if (weightMin <= rMin && (weightMax ?? decimal.MaxValue) >= rMax)
					{
						_context.SupLogisticsRates.Remove(rate);
					}
					// 部分重疊左側（新區間切掉舊區間前半段）
					else if (weightMin <= rMin && (weightMax ?? decimal.MaxValue) < rMax)
					{
						rate.WeightMin = (weightMax ?? decimal.MaxValue) + 0.1M;
						rate.RevisedDate = DateTime.Now;
					}
					// 部分重疊右側（新區間切掉舊區間後半段）
					else if (weightMin > rMin && (weightMax ?? decimal.MaxValue) >= rMax)
					{
						rate.WeightMax = weightMin - 0.1M;
						rate.RevisedDate = DateTime.Now;
					}
					// 中間切割（舊區間分兩段）
					else if (weightMin > rMin && (weightMax ?? decimal.MaxValue) < rMax)
					{
						var newUpper = new SupLogisticsRate
						{
							LogisticsId = rate.LogisticsId,
							WeightMin = (weightMax ?? decimal.MaxValue) + 0.1M,
							WeightMax = rate.WeightMax,
							ShippingFee = rate.ShippingFee,
							IsActive = rate.IsActive,
							Reviser = user.UserNumberId,
							RevisedDate = DateTime.Now
						};
						_context.SupLogisticsRates.Add(newUpper);

						rate.WeightMax = weightMin - 0.1M;
						rate.RevisedDate = DateTime.Now;
					}
				}

				// 新增新區間
				var newRate = new SupLogisticsRate
				{
					LogisticsId = logisticsId,
					WeightMin = weightMin,
					WeightMax = weightMax,
					ShippingFee = shippingFee,
					IsActive = isActive,
					Reviser = user.UserNumberId,
					RevisedDate = DateTime.Now
				};

				_context.SupLogisticsRates.Add(newRate);

				// 若最後一個區間是無限大，並且在新增區間之後，更新費用保持一致
				var lastRate = existingRates.OrderByDescending(r => r.WeightMin).FirstOrDefault(r => !r.WeightMax.HasValue);
				if (lastRate != null && (weightMax ?? decimal.MaxValue) >= lastRate.WeightMin)
				{
					lastRate.WeightMin = (weightMax ?? lastRate.WeightMin) + 0.1M;
					lastRate.ShippingFee = shippingFee;
					lastRate.RevisedDate = DateTime.Now;
				}

				await _context.SaveChangesAsync();

				return Json(new { success = true, rate = newRate });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}


		// POST: /SUP/Logistics/UpdateRateWeightMax
		[HttpPost]
		public async Task<IActionResult> UpdateRateWeightMax(int logisticsId, int newRateId, decimal newWeightMin, decimal newWeightMax)
		{
			try
			{
				// 取得同一物流的所有區間（排好序）
				var rates = await _context.SupLogisticsRates
					.Where(r => r.LogisticsId == logisticsId)
					.OrderBy(r => r.WeightMin)
					.ToListAsync();

				var newRate = rates.FirstOrDefault(r => r.LogisticsRateId == newRateId);
				if (newRate == null)
					return Json(new { success = false, message = "找不到新區間資料" });

				// 更新新區間
				newRate.WeightMin = newWeightMin;
				newRate.WeightMax = newWeightMax;
				newRate.RevisedDate = DateTime.Now;

				// 調整與新區間重疊的舊區間
				foreach (var rate in rates)
				{
					if (rate.LogisticsRateId == newRateId)
						continue;

					// 舊區間最大重量在新區間最小重量之後且最小重量在新區間最大重量之前 => 有重疊
					if ((rate.WeightMax ?? decimal.MaxValue) > newWeightMin && rate.WeightMin < newWeightMax)
					{
						// 若舊區間完全被新區間包含，直接刪除舊區間或改成最小重量在新區間之前
						if (rate.WeightMin < newWeightMin)
						{
							rate.WeightMax = newWeightMin; // 舊區間截斷到新區間最小重量
						}
						else
						{
							rate.WeightMin = newWeightMax; // 舊區間被截斷在新區間之後
							if (rate.WeightMax <= rate.WeightMin)
							{
								// 若截斷後無效，刪除
								_context.SupLogisticsRates.Remove(rate);
							}
						}
						rate.RevisedDate = DateTime.Now;
					}
				}

				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
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


		// POST: /SUP/Logistics/DeleteRate
		[HttpPost]
		public async Task<IActionResult> DeleteRate(int rateId)
		{
			try
			{
				// 找出要刪除的區間
				var rate = await _context.SupLogisticsRates.FirstOrDefaultAsync(r => r.LogisticsRateId == rateId);
				if (rate == null)
					return Json(new { success = false, message = "找不到區間資料" });

				var logisticsId = rate.LogisticsId;

				// 取得同物流的所有區間，按 WeightMin 排序
				var rates = await _context.SupLogisticsRates
					.Where(r => r.LogisticsId == logisticsId && r.LogisticsRateId != rateId)
					.OrderBy(r => r.WeightMin)
					.ToListAsync();

				// 找到被刪除區間的前後區間
				var prev = rates.LastOrDefault(r => r.WeightMax <= rate.WeightMin);
				var next = rates.FirstOrDefault(r => r.WeightMin >= rate.WeightMax);

				// 自動整理前後區間，使連貫
				if (prev != null && next != null)
				{
					prev.WeightMax = next.WeightMin; // 前區間的最大重量連到下一區間最小重量
					prev.RevisedDate = DateTime.Now;
				}
				else if (prev != null && next == null)
				{
					prev.WeightMax = null; // 前區間接到無限大
					prev.RevisedDate = DateTime.Now;
				}
				else if (prev == null && next != null)
				{
					next.WeightMin = 0; // 若前面沒有區間，下一區間從 0 開始
					next.RevisedDate = DateTime.Now;
				}

				// 刪除目標區間
				_context.SupLogisticsRates.Remove(rate);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "刪除成功" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "刪除失敗", error = ex.Message });
			}
		}

		// 切換子表運費區間的啟用狀態
		// POST: /SUP/Logistics/ToggleRateActive
		[HttpPost]
		public async Task<IActionResult> ToggleRateActive(int id, bool isActive)
		{
			try
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
			catch (DbUpdateException dbEx)
			{
				return Json(new { success = false, message = "資料庫更新失敗: " + dbEx.Message });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}

		#endregion

		#region 物流商

		// GET: /SUP/Logistics/Create
		[HttpGet]
		public IActionResult Create()
		{
			// 空物件給 Partial View 使用
			var viewModel = new LogisticsContactViewModel();
			return PartialView("~/Areas/SUP/Views/Logistics/Partials/_LogisticsFormPartial.cshtml", viewModel);
		}

		// POST: /SUP/Logistics/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SupLogistic model)
		{
			if (!ModelState.IsValid)
				return PartialView("~/Areas/SUP/Views/Logistics/Partials/_LogisticsFormPartial.cshtml", model);

			try
			{
				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null) return Json(new { success = false, message = "找不到使用者資料" });

				model.Creator = user.UserNumberId;
				model.CreatedDate = DateTime.Now;
				model.Reviser = user.UserNumberId;
				model.RevisedDate = DateTime.Now;

				_context.SupLogistics.Add(model);
				await _context.SaveChangesAsync();

				return Json(new { success = true, isCreate = true, logistics = model });
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


		// GET: /SUP/Logistics/Edit/1000
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var log = await _context.SupLogistics.FindAsync(id);
			if (log == null) return NotFound();

			var vm = new LogisticsContactViewModel
			{
				LogisticsId = log.LogisticsId,
				LogisticsName = log.LogisticsName,
				ShippingMethod = log.ShippingMethod,
				IsActive = log.IsActive,
			};

			ViewBag.FormAction = "Edit";
			return PartialView("~/Areas/SUP/Views/Logistics/Partials/_LogisticsFormPartial.cshtml", vm);
		}


		// POST: /SUP/Logistics/Edit/1000
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(LogisticsContactViewModel vm)
		{
			if (!ModelState.IsValid)
				return PartialView("~/Areas/SUP/Views/Logistics/Partials/_LogisticsFormPartial.cshtml", vm);

			try
			{
				var logEntity = await _context.SupLogistics.FindAsync(vm.LogisticsId);
				if (logEntity == null)
					return Json(new { success = false, message = "找不到該物流商" });

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null)
					return Json(new { success = false, message = "找不到使用者資料" });

				int currentUserId = user.UserNumberId;

				// 更新欄位
				logEntity.LogisticsName = vm.LogisticsName;
				logEntity.ShippingMethod = vm.ShippingMethod;
				logEntity.IsActive = vm.IsActive;
				logEntity.Reviser = currentUserId;
				logEntity.RevisedDate = DateTime.Now;


				_context.Update(logEntity);
				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					isCreate = false,
					logistics = new
					{
						logisticsId = logEntity.LogisticsId,
						logisticsName = logEntity.LogisticsName,
						shippingMethod = logEntity.ShippingMethod,
						isActive = logEntity.IsActive
					}
				});
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

		// GET: /SUP/Logistics/Details/1000
		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			var log = await _context.SupLogistics
				.AsNoTracking()
				.FirstOrDefaultAsync(l => l.LogisticsId == id);

			if (log == null) return NotFound();

			// 將 SupLogistic 轉成 ViewModel
			var vm = new LogisticsContactViewModel
			{

				LogisticsId = log.LogisticsId,
				LogisticsName = log.LogisticsName,
				ShippingMethod = log.ShippingMethod,
				IsActive = log.IsActive,
				Creator = log.Creator,
				CreatedDate = log.CreatedDate,
				Reviser = log.Reviser,
				RevisedDate = log.RevisedDate
			};

			return PartialView("~/Areas/SUP/Views/Logistics/Partials/_LogisticsInfoPartial.cshtml", vm);

		}

		// POST: /SUP/Logistics/Delete
		[HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				var logEntity = await _context.SupLogistics.FindAsync(id);
				if (logEntity == null)
					return Json(new { success = false, message = "找不到該物流商" });
				
				// 先檢查是否存在子資料（運費分段）
				bool hasRates = await _context.SupLogisticsRates.AnyAsync(r => r.LogisticsId == id);
				if (hasRates)
				{
					return Json(new
					{
						success = false,
						message = "該物流商仍有運費分段資料，請先刪除或轉移後再刪除物流商。"
					});
				}

				_context.SupLogistics.Remove(logEntity);
				await _context.SaveChangesAsync();
				return Json(new { success = true, message = "刪除成功" });
			}
			catch (DbUpdateException dbEx)
			{
				//return Json(new { success = false, message = "資料庫更新失敗: " + dbEx.Message });
				
				// 外鍵違反等資料庫層異常（保險起見仍處理）
				Console.WriteLine(dbEx);
				return Json(new
				{
					success = false,
					message = "刪除失敗：存在關聯資料（運費分段），請先刪除相關資料後再試。"
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "發生錯誤: " + ex.Message });
			}
		}

		// 切換物流商啟用狀態
		// POST: /SUP/Logistics/ToggleActive
		public async Task<IActionResult> ToggleActive(int id, bool isActive)
		{
			try
			{
				var logEntity = await _context.SupLogistics.FindAsync(id);
				if (logEntity == null)
					return Json(new { success = false, message = "找不到該物流商" });

				var userId = _me.Id;
				var user = await _userMgr.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
				if (user == null)
					return Json(new { success = false, message = "找不到使用者資料" });

				logEntity.IsActive = isActive;
				logEntity.Reviser = user.UserNumberId;
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

		#endregion
	}
}
