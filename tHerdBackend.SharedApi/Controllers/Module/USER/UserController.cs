										  
using Microsoft.AspNetCore.Authentication.JwtBearer; // JwtBearerDefaults
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using tHerdBackend.Core.DTOs;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.Models;          // ApplicationDbContext, ApplicationUser (Identity)
using tHerdBackend.Services.Common;


namespace tHerdBackend.SharedApi.Controllers.Module.USER
{
	[ApiController]
	[Route("api/user")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public class UserController : ControllerBase
	{
		private readonly UserManager<ApplicationUser> _userMgr;
		private readonly ApplicationDbContext _appDb;  // Identity 專用
		private readonly tHerdDBContext _herdDb;       // 業務資料專用
		private readonly ISysAssetFileRepository _fileRepo;


		public UserController(
			UserManager<ApplicationUser> userMgr,
			ApplicationDbContext appDb,
			tHerdDBContext herdDb,
			ISysAssetFileRepository fileRepo)
		{
			_userMgr = userMgr;
			_appDb = appDb;
			_herdDb = herdDb;
			_fileRepo = fileRepo;
		}

		// ======== 共用小工具：取目前使用者的 UserNumberId ========
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

		// ======== 只給 Avatar 等需要完整使用者資料的地方使用 ========
		private async Task<ApplicationUser> GetCurrentUserAsync()
		{
			var userId = _userMgr.GetUserId(User);
			if (string.IsNullOrEmpty(userId))
				throw new UnauthorizedAccessException("未登入");

			var user = await _appDb.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (user == null)
				throw new UnauthorizedAccessException("未登入");

			return user;
		}

		/// <summary>
		/// 目前登入者完整資料（Identity 基本 + 你需要顯示的欄位）
		/// 來源：AspNetUsers (ApplicationDbContext)
		/// </summary>
		[HttpGet("me/detail")]
		public async Task<IActionResult> GetMyDetail()
		{
			var userId = _userMgr.GetUserId(User);
			if (string.IsNullOrEmpty(userId))
				return Unauthorized(new { error = "未登入" });

			var u = await _appDb.Users.AsNoTracking()
				.Where(x => x.Id == userId)
				.Select(x => new UserDetailDto
				{
					Id = x.Id,
					UserNumberId = x.UserNumberId,
					Email = x.Email!,
					LastName = x.LastName,
					FirstName = x.FirstName,
					Name = x.LastName + x.FirstName,
					ImgId = x.ImgId,
					Gender = x.Gender,
					BirthDate = x.BirthDate,
					Address = x.Address,
					MemberRankId = x.MemberRankId,
					ReferralCode = x.ReferralCode,
					UsedReferralCode = x.UsedReferralCode,
					CreatedDate = x.CreatedDate,
					RevisedDate = x.RevisedDate,
					LastLoginDate = x.LastLoginDate,
					IsActive = x.IsActive,
					ActivationDate = x.ActivationDate,
					PhoneNumber = x.PhoneNumber,
					EmailConfirmed = x.EmailConfirmed,
					TwoFactorEnabled = x.TwoFactorEnabled
				})
				.FirstOrDefaultAsync();

			if (u == null)
				return NotFound(new { error = "找不到使用者" });

			return Ok(u);
		}

		public class UpdateProfileDto
		{
			public string LastName { get; set; } = "";
			public string FirstName { get; set; } = "";
			public string? PhoneNumber { get; set; }
			public string? Address { get; set; }
			public string? Gender { get; set; }
			public DateTime? BirthDate { get; set; } // 前端傳 yyyy-MM-dd → model binder 會吃
		}

		[HttpPatch("me")]
		public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
		{
			var user = await GetCurrentUserAsync();

			user.LastName = dto.LastName?.Trim() ?? user.LastName;
			user.FirstName = dto.FirstName?.Trim() ?? user.FirstName;
			user.PhoneNumber = dto.PhoneNumber?.Trim();
			user.Address = dto.Address?.Trim();
			user.Gender = dto.Gender?.Trim();
			user.BirthDate = dto.BirthDate;

			user.RevisedDate = DateTime.UtcNow;
			await _appDb.SaveChangesAsync();

			return Ok(new { ok = true });
		}

		// =====================
		// =      Avatar       =
		// =====================

		/// <summary>
		/// GET /api/user/avatar 取得目前頭像（沒有則空）
		/// </summary>
		[HttpGet("avatar")]
		public async Task<IActionResult> GetAvatar()
		{
			try
			{
				var user = await GetCurrentUserAsync();
				if (!(user.ImgId is int fileId) || fileId <= 0)
					return Ok(new { fileId = (int?)null, fileUrl = "" });

				var dto = await _fileRepo.GetFileById(fileId);
				return Ok(new
				{
					fileId = dto?.FileId,
					fileUrl = dto?.FileUrl ?? ""
				});
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
		}

		//密碼變更
		public class ChangePasswordDto
		{
			public string OldPassword { get; set; } = "";
			public string NewPassword { get; set; } = "";
		}

		[HttpPost("change-password")]
		public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
				return BadRequest(new { error = "密碼不可為空" });

			var user = await GetCurrentUserAsync();

			var result = await _userMgr.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
			if (!result.Succeeded)
				return BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

			user.RevisedDate = DateTime.UtcNow;
			await _appDb.SaveChangesAsync();

			return Ok(new { ok = true });
		}

		/// <summary>
		/// POST /api/user/avatar (multipart/form-data) 上傳/更換
		/// 檔案大小上限 5MB（雖設置 RequestSizeLimit 10MB，但邏輯上仍以 5MB 限制）
		/// </summary>
		[HttpPost("avatar")]
		[Consumes("multipart/form-data")]
		[RequestSizeLimit(10 * 1024 * 1024)]
		public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarForm form)
		{
			var file = form.File;

			if (file == null || file.Length == 0)
				return BadRequest(new { error = "請選擇圖片檔案" });
			if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
				return BadRequest(new { error = "僅接受圖片檔案" });
			if (file.Length > 5 * 1024 * 1024)
				return BadRequest(new { error = "檔案過大，請小於 5MB" });

			try
			{
				var user = await GetCurrentUserAsync();

				// 1) 透過共用 Repo 上傳到 Cloudinary 並寫 DB
				//    moduleId/progId 用固定路徑：USER/Avatar
				var upload = new AssetFileUploadDto
				{
					ModuleId = "USER",
					ProgId = "Avatar",
					Meta = new List<AssetFileDetailsDto>
					{
						new AssetFileDetailsDto
						{
							File = file,
							AltText = $"{user.LastName}{user.FirstName} avatar",
							Caption = "user avatar",
							IsActive = true
						}
					}
				};

				var result = await _fileRepo.AddFilesAsync(upload);

				if (!AssetFileResultHelper.TryPickFirstFile(result, out var newFileId, out var newUrl))
					return StatusCode(500, new { error = "上傳失敗（無法解析回傳值）" });

				// 2) 若原本有頭像 → 軟刪舊檔（統一透過 Repo）
				if (user.ImgId is int oldId && oldId > 0)
				{
					await _fileRepo.DeleteImage(oldId);
				}

				// 3) 綁定到使用者
				user.ImgId = newFileId;
				await _appDb.SaveChangesAsync();

				return Ok(new { fileId = newFileId, fileUrl = newUrl });
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
		}

		/// <summary>
		/// DELETE /api/user/avatar 移除目前頭像
		/// </summary>
		[HttpDelete("avatar")]
		public async Task<IActionResult> DeleteAvatar()
		{
			try
			{
				var user = await GetCurrentUserAsync();
				if (!(user.ImgId is int fileId) || fileId <= 0)
					return Ok(new { ok = true }); // 本來就沒有

				// 軟刪檔案（你的 Repo 預設軟刪）
				await _fileRepo.DeleteImage(fileId);

				user.ImgId = null;
				await _appDb.SaveChangesAsync();

				return Ok(new { ok = true });
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
		}

		/// <summary>
		/// 依會員等級代碼取得等級資料
		/// 來源：USER_MemberRank (tHerdDBContext)
		/// </summary>
		[HttpGet("member-ranks/{id}")]
		public async Task<IActionResult> GetMemberRank([FromRoute] string id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return BadRequest(new { error = "缺少等級代碼" });

			var rank = await _herdDb.UserMemberRanks
				.AsNoTracking()
				.Where(r => r.MemberRankId == id)
				.Select(r => new MemberRankDto
				{
					MemberRankId = r.MemberRankId,
					RankName = r.RankName,
					TotalSpentForUpgrade = r.TotalSpentForUpgrade,
					OrderCountForUpgrade = r.OrderCountForUpgrade,
					RebateRate = r.RebateRate,
					RankDescription = r.RankDescription,
					IsActive = r.IsActive
				})
				.FirstOrDefaultAsync();

			if (rank == null)
				return NotFound(new { error = "找不到會員等級" });

			return Ok(rank);
		}

		/// <summary>
		/// 目前登入者的「通知偏好」清單
		/// 來源：USER_Notification (tHerdDBContext) by UserNumberId
		/// </summary>
		[HttpGet("notifications")]
		public async Task<IActionResult> GetMyNotifications()
		{
			try
			{
				var numberId = await GetCurrentUserNumberIdAsync();

				var list = await _herdDb.UserNotifications
					.AsNoTracking()
					.Where(n => n.UserNumberId == numberId)
					.OrderByDescending(n => n.CreatedDate)
					.Select(n => new NotificationDto
					{
						NotificationId = n.NotificationId,
						NotificationType = n.NotificationType,
						IsActive = n.IsActive,
						CreatedDate = n.CreatedDate,
						RevisedDate = n.RevisedDate
					})
					.ToListAsync();

				return Ok(list);
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}

		// =========================
		// =  NotificationHistory  =
		// =========================

		/// <summary>
		/// 未讀通知數（給側邊欄 badge）
		/// 來源：USER_NotificationHistory
		/// </summary>
		[HttpGet("notifications/unread-count")]
		public async Task<IActionResult> GetUnreadCount()
		{
			try
			{
				var numberId = await GetCurrentUserNumberIdAsync();

				var count = await _herdDb.UserNotificationHistories
					.AsNoTracking()
					.Where(n => n.UserNumberId == numberId && !n.IsRead)
					.CountAsync();

				return Ok(new { count });
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}

		/// <summary>
		/// 通知清單（預設只取未讀，可用 onlyUnread=false 取全部），含分頁
		/// GET /api/user/notifications/history?onlyUnread=true&page=1&pageSize=20
		/// </summary>
		[HttpGet("notifications/history")]
		public async Task<IActionResult> GetNotificationHistory(
			[FromQuery] bool onlyUnread = true,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 20)
		{
			try
			{
				var numberId = await GetCurrentUserNumberIdAsync();
				page = page <= 0 ? 1 : page;
				pageSize = pageSize <= 0 ? 20 : pageSize;

				var q = _herdDb.UserNotificationHistories.AsNoTracking()
					.Where(n => n.UserNumberId == numberId);

				if (onlyUnread) q = q.Where(n => !n.IsRead);

				var total = await q.CountAsync();

				var items = await q
					.OrderByDescending(n => n.SentDate)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.Select(n => new NotificationItemDto(
						n.NotificationHistoryId,
						n.NotificationType,
						n.ContentTitle,
						n.Content,
						n.DeliveryChannel,
						n.SentDate,
						n.Status,
						n.ModuleId,
						n.IsRead
					))
					.ToListAsync();

				return Ok(new PagedResult<NotificationItemDto>(items, total, page, pageSize));
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}

		/// <summary>
		/// 標記單筆通知為已讀
		/// POST /api/user/notifications/{id}/read
		/// </summary>
		[HttpPost("notifications/{id}/read")]
		public async Task<IActionResult> MarkNotificationRead([FromRoute] int id)
		{
			try
			{
				var numberId = await GetCurrentUserNumberIdAsync();

				var row = await _herdDb.UserNotificationHistories
					.Where(n => n.NotificationHistoryId == id && n.UserNumberId == numberId)
					.FirstOrDefaultAsync();

				if (row == null) return NotFound();

				if (!row.IsRead)
				{
					row.IsRead = true;
					await _herdDb.SaveChangesAsync();
				}
				return Ok(new { ok = true });
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}

		/// <summary>
		/// 將目前使用者所有未讀通知標記為已讀（可選：最近 N 天）
		/// POST /api/user/notifications/read-all?days=30
		/// </summary>
		[HttpPost("notifications/read-all")]
		public async Task<IActionResult> MarkAllNotificationsRead([FromQuery] int? days = null)
		{
			try
			{
				var numberId = await GetCurrentUserNumberIdAsync();

				var q = _herdDb.UserNotificationHistories
					.Where(n => n.UserNumberId == numberId && !n.IsRead);

				if (days is int d && d > 0)
				{
					var since = DateTime.UtcNow.AddDays(-d);
					q = q.Where(n => n.SentDate >= since);
				}

#if NET7_0_OR_GREATER
				var affected = await q.ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
#else
                var list = await q.ToListAsync();
                foreach (var n in list) n.IsRead = true;
                var affected = await _herdDb.SaveChangesAsync();
#endif
				return Ok(new { ok = true, affected });
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}
		// =====================
		// =   Coupon Wallet   =
		// =====================

		/// <summary>
		/// 目前登入者的優惠券錢包清單（支援狀態過濾、僅可用、分頁）
		/// GET /api/user/coupons/wallet?status=&onlyUsable=true&page=1&pageSize=20
		/// 說明：
		/// - status 可傳：Active / Used / Expired（依你錢包 Status 欄位實際值）
		/// - onlyUsable=true 會套用：Status == "Active" && UsedDate IS NULL
		/// </summary>
		[HttpGet("coupons/wallet")]
		public async Task<IActionResult> GetMyCouponWallet(
			[FromQuery] string? status = null,
			[FromQuery] bool onlyUsable = false,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 20)
		{
			try
			{
				var numberId = await GetCurrentUserNumberIdAsync();
				page = page <= 0 ? 1 : page;
				pageSize = pageSize <= 0 ? 20 : pageSize;

				var q = _herdDb.UserCouponWallets.AsNoTracking()
					.Where(w => w.UserNumberId == numberId);

				if (!string.IsNullOrWhiteSpace(status))
				{
					var s = status.Trim();
					q = q.Where(w => w.Status == s);
				}

				if (onlyUsable)
				{
					// 「可用」最小定義：狀態 Active 且尚未使用（沒有 UsedDate）
					q = q.Where(w => w.Status == "Active" && w.UsedDate == null);
				}

				var total = await q.CountAsync();

				var items = await q
					.OrderByDescending(w => w.ClaimedDate)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.Select(w => new CouponWalletItemDto(
						w.CouponWalletId,
						w.CouponId,
						w.ClaimedDate,
						w.UsedDate,
						w.Status,
						// 最小版「可用」判斷（如未來有到期日請在此補上條件）
						(w.Status == "Active" && w.UsedDate == null)
					))
					.ToListAsync();

				return Ok(new PagedResult<CouponWalletItemDto>(items, total, page, pageSize));
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}

		/// <summary>
		/// 優惠券錢包「摘要」：每種狀態的數量 + 可用數量
		/// GET /api/user/coupons/wallet/summary
		/// </summary>
		[HttpGet("coupons/wallet/summary")]
		public async Task<IActionResult> GetMyCouponWalletSummary()
		{
			try
			{
				var numberId = await GetCurrentUserNumberIdAsync();

				var all = await _herdDb.UserCouponWallets.AsNoTracking()
					.Where(w => w.UserNumberId == numberId)
					.Select(w => new { w.Status, w.UsedDate })
					.ToListAsync();

				var total = all.Count;
				var byStatus = all.GroupBy(x => x.Status)
								  .ToDictionary(g => g.Key, g => g.Count());

				var usable = all.Count(x => x.Status == "Active" && x.UsedDate == null);

				return Ok(new
				{
					total,
					byStatus,
					usable
				});
			}
			catch (UnauthorizedAccessException)
			{
				return Unauthorized(new { error = "未登入" });
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}

	}
}
