using FlexBackend.Core.DTOs.USER;
using FlexBackend.Core.Web_Datatables;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Area("USER")]
[Controller]
public class MembersController : Controller
{
	private readonly tHerdDBContext _db;
	private readonly UserManager<ApplicationUser> _userMgr;
	private readonly RoleManager<ApplicationRole> _roleMgr;

	private const string MEMBER_ROLE_NAME = "Member";
	private const string DEFAULT_MEMBER_RANK = "MR001";
	private const string DEFAULT_PASSWORD = "Init@12345"; // 依你的實際政策調整

	public MembersController(
		tHerdDBContext db,
		UserManager<ApplicationUser> userMgr,
		RoleManager<ApplicationRole> roleMgr)
	{
		_db = db;
		_userMgr = userMgr;
		_roleMgr = roleMgr;
	}

	public IActionResult Index() => View();

	// 會員清單（僅 Member 角色）
	[HttpGet]
	public async Task<IActionResult> Users([FromQuery] DataTableRequest? dt)
	{
		var draw = dt?.Draw ?? 0;
		var start = Math.Max(0, dt?.Start ?? 0);
		var length = (dt?.Length ?? 10) > 0 ? dt!.Length : 10;

		// 搜尋關鍵字（DataTables fallback）
		var kw = dt?.Search?.Value;
		if (string.IsNullOrWhiteSpace(kw))
			kw = Request.Query["search[value]"].ToString();
		kw = kw?.Trim();

		// 取得 Member 角色 Id
		var memberRoleId = await _db.AspNetRoles
			.Where(r => r.Name == MEMBER_ROLE_NAME)
			.Select(r => r.Id)
			.FirstOrDefaultAsync();

		// 所有 Member 使用者
		var baseQ = _db.AspNetUsers.AsNoTracking()
	.Where(u => u.Roles.Any(r => r.Id == memberRoleId));
		//var baseQ = from u in _db.AspNetUsers.AsNoTracking()
		//			join ur in _db.AspNetUserRoles.AsNoTracking() on u.Id equals ur.UserId
		//			where ur.RoleId == memberRoleId
		//			select u;

		var recordsTotal = await baseQ.CountAsync();

		// 搜尋（姓+名、Email、電話）
		if (!string.IsNullOrEmpty(kw))
		{
			baseQ = baseQ.Where(u =>
				((u.LastName ?? "") + " " + (u.FirstName ?? "")).Contains(kw) ||
				(u.Email ?? "").Contains(kw) ||
				(u.PhoneNumber ?? "").Contains(kw));
		}

		var recordsFiltered = await baseQ.CountAsync();

		// 帶出黑名單旗標（逐列 Any）
		IQueryable<MemberWithFlag> qWithFlag = baseQ.Select(u => new MemberWithFlag
		{
			U = u,
			HasBlack = _db.UserBlockHistories.Any(b => b.UserNumberId == u.UserNumberId)
		});

		// 解析 DataTables 排序（columns[n][data]）
		string? orderColIdxStr = Request.Query["order[0][column]"];
		string? orderDir = Request.Query["order[0][dir]"];
		bool desc = string.Equals(orderDir, "desc", StringComparison.OrdinalIgnoreCase);

		IOrderedQueryable<MemberWithFlag> ordered;
		if (int.TryParse(orderColIdxStr, out var orderIdx))
		{
			var colKey = Request.Query[$"columns[{orderIdx}][data]"].ToString(); // number/lastName/firstName/email/phoneNumber/isActive/blackStatus/createdDate
			ordered = colKey switch
			{
				"number" => (desc ? qWithFlag.OrderByDescending(x => x.U.UserNumberId).ThenByDescending(x => x.U.LastName)
								  : qWithFlag.OrderBy(x => x.U.UserNumberId).ThenBy(x => x.U.LastName)),
				"lastName" => (desc ? qWithFlag.OrderByDescending(x => x.U.LastName).ThenByDescending(x => x.U.FirstName)
									: qWithFlag.OrderBy(x => x.U.LastName).ThenBy(x => x.U.FirstName)),
				"firstName" => (desc ? qWithFlag.OrderByDescending(x => x.U.FirstName)
									 : qWithFlag.OrderBy(x => x.U.FirstName)),
				"email" => (desc ? qWithFlag.OrderByDescending(x => x.U.Email)
								 : qWithFlag.OrderBy(x => x.U.Email)),
				"phoneNumber" => (desc ? qWithFlag.OrderByDescending(x => x.U.PhoneNumber)
									   : qWithFlag.OrderBy(x => x.U.PhoneNumber)),
				"isActive" => (desc ? qWithFlag.OrderByDescending(x => x.U.IsActive)
									: qWithFlag.OrderBy(x => x.U.IsActive)),
				"blackStatus" => (desc ? qWithFlag.OrderByDescending(x => x.HasBlack)
									   : qWithFlag.OrderBy(x => x.HasBlack)),
				"createdDate" => (desc ? qWithFlag.OrderByDescending(x => x.U.CreatedDate)
									   : qWithFlag.OrderBy(x => x.U.CreatedDate)),
				_ => qWithFlag.OrderByDescending(x => x.U.CreatedDate)
			};
		}
		else
		{
			ordered = qWithFlag.OrderByDescending(x => x.U.CreatedDate);
		}

		// 分頁 + 投影
		var page = await ordered
			.Skip(start).Take(length)
			.Select(x => new
			{
				x.U.Id,
				x.U.UserNumberId,
				x.U.LastName,
				x.U.FirstName,
				x.U.Email,
				x.U.PhoneNumber,
				x.U.IsActive,
				x.U.CreatedDate,
				HasBlack = x.HasBlack
			})
			.ToListAsync();

		var data = page.Select(u => new
		{
			id = u.Id,
			number = u.UserNumberId,
			lastName = u.LastName,
			firstName = u.FirstName,
			email = u.Email,
			phoneNumber = u.PhoneNumber,
			isActive = u.IsActive,
			blackStatus = u.HasBlack ? "黑名單" : "白名單",
			createdDate = u.CreatedDate.ToString("yyyy-MM-dd HH:mm")
		});

		return Json(new { draw, recordsTotal, recordsFiltered, data });
	}

	// 取得詳細（含 rank 與黑名單最新狀態）
	[HttpGet]
	public async Task<IActionResult> Details(string id)
	{
		var u = await _db.AspNetUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		if (u == null) return NotFound();

		var blk = await _db.UserBlockHistories
			.Where(b => b.UserNumberId == u.UserNumberId)
			.OrderByDescending(b => b.StartDate).ThenByDescending(b => b.BlockHistoryId)
			.FirstOrDefaultAsync();

		return Json(new
		{
			id = u.Id,
			number = u.UserNumberId,
			lastName = u.LastName,
			firstName = u.FirstName,
			email = u.Email,
			phoneNumber = u.PhoneNumber,
			gender = u.Gender ?? "N/A",
			birthDate = u.BirthDate?.ToString("yyyy-MM-dd"),
			address = u.Address,
			isActive = u.IsActive,
			rank = u.MemberRankId ?? DEFAULT_MEMBER_RANK,   // 假設 AspNetUsers 有 Rank 欄位
			isBlacklisted = blk != null,
			block = blk == null ? null : new
			{
				reason = blk.Reason,
				scope = blk.Scope,
				startDate = blk.StartDate.ToString("yyyy-MM-dd"),
				endDate = blk.EndDate?.ToString("yyyy-MM-dd"),
				status = blk.Status
			}
		});
	}

	// 儲存詳細（基本資料 + 黑名單設定）
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateDetails(
		[FromForm] string id,
		[FromForm] string lastName,
		[FromForm] string firstName,
		[FromForm] string email,
		[FromForm] string? phoneNumber,
		[FromForm] string? gender,
		[FromForm] DateTime? birthDate,
		[FromForm] string? address,
		[FromForm] bool isActive,
		// Rank 僅顯示，不改；若你要開放修改可加上 [FromForm] string rank,
		[FromForm] bool isBlacklisted,
		[FromForm] string? block_reason,
		[FromForm] string? block_scope,
		[FromForm] DateTime? block_startDate,
		[FromForm] DateTime? block_endDate
	)
	{
		var user = await _userMgr.FindByIdAsync(id);
		if (user == null) return BadRequest(new { ok = false, message = "使用者不存在" });

		if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(email))
			return BadRequest(new { ok = false, message = "必填欄位不足" });

		// 基本欄位
		user.LastName = lastName.Trim();
		user.FirstName = firstName.Trim();
		user.Gender = string.IsNullOrWhiteSpace(gender) ? (user.Gender ?? "N/A") : gender.Trim();
		user.BirthDate = birthDate;
		user.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
		user.IsActive = isActive;
		// Rank 僅顯示，不改：user.Rank 保持原樣

		// Email/UserName 正規化
		var newEmail = email.Trim();
		user.Email = newEmail;
		user.UserName = newEmail;
		user.NormalizedEmail = _userMgr.NormalizeEmail(newEmail);
		user.NormalizedUserName = _userMgr.NormalizeName(newEmail);
		user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
		user.RevisedDate = DateTime.UtcNow;

		var upd = await _userMgr.UpdateAsync(user);
		if (!upd.Succeeded)
			return BadRequest(new { ok = false, message = string.Join("; ", upd.Errors.Select(e => e.Description)) });

		// 黑名單：勾選 => 產生/覆蓋最新一筆；取消 => 結束所有現行紀錄
		var hist = await _db.UserBlockHistories
			.Where(b => b.UserNumberId == user.UserNumberId)
			.OrderByDescending(b => b.StartDate).ThenByDescending(b => b.BlockHistoryId)
			.FirstOrDefaultAsync();

		if (isBlacklisted)
		{
			if (string.IsNullOrWhiteSpace(block_reason))
				return BadRequest(new { ok = false, message = "請輸入黑名單原因" });

			if (hist == null || hist.EndDate != null || !string.Equals(hist.Status, "Active", StringComparison.OrdinalIgnoreCase))
			{
				// 新增一筆
				var now = DateTime.UtcNow;
				_db.UserBlockHistories.Add(new UserBlockHistory
				{
					UserNumberId = user.UserNumberId,
					Reason = block_reason.Trim(),
					Scope = string.IsNullOrWhiteSpace(block_scope) ? null : block_scope.Trim(),
					StartDate = block_startDate.HasValue
					? DateOnly.FromDateTime(block_startDate.Value)
					: DateOnly.FromDateTime(DateTime.UtcNow),
					EndDate = block_endDate.HasValue
					? DateOnly.FromDateTime(block_endDate.Value)
					: (DateOnly?)null,
					Status = "Active",
					Creator = 0,
					RevisedDate = now
				});
			}
			else
			{
				// 更新現行那筆
				hist.Reason = block_reason.Trim();
				hist.Scope = string.IsNullOrWhiteSpace(block_scope) ? null : block_scope.Trim();
				hist.StartDate = block_startDate.HasValue
					? DateOnly.FromDateTime(block_startDate.Value)
					: DateOnly.FromDateTime(DateTime.UtcNow);
				hist.EndDate = block_endDate.HasValue
					? DateOnly.FromDateTime(block_endDate.Value)
					: (DateOnly?)null;
				hist.Status = "Active";
				hist.RevisedDate = DateTime.UtcNow;
				_db.UserBlockHistories.Update(hist);
			}
		}
		else
		{
			// 取消黑名單：把所有未結束的紀錄收尾
			var actives = await _db.UserBlockHistories
				.Where(b => b.UserNumberId == user.UserNumberId && (b.EndDate == null || b.Status == "Active"))
				.ToListAsync();
			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			foreach (var a in actives)
			{
				a.EndDate = a.EndDate ?? today;
				a.Status = "Closed";
				a.RevisedDate = DateTime.UtcNow;
			}
			if (actives.Count > 0) _db.UserBlockHistories.UpdateRange(actives);
		}

		await _db.SaveChangesAsync();
		return Json(new { ok = true });
	}

	// 新增會員（Rank=MR001，加入 Member 角色，使用預設密碼）
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create([FromForm] string email,
											[FromForm] string lastName,
											[FromForm] string firstName,
											[FromForm] string? phone)
	{
		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
			return BadRequest(new { ok = false, message = "必填欄位缺失" });

		if (await _userMgr.FindByEmailAsync(email) != null)
			return BadRequest(new { ok = false, message = "Email 已存在" });

		// 確保有 Member 角色
		if (!await _roleMgr.RoleExistsAsync(MEMBER_ROLE_NAME))
			return BadRequest(new { ok = false, message = "系統尚未建立 Member 角色" });

		var normalizedEmail = _userMgr.NormalizeEmail(email.Trim());
		var normalizedUserName = _userMgr.NormalizeName(email.Trim());

		var user = new ApplicationUser
		{
			Id = Guid.NewGuid().ToString(),
			UserNumberId = 0, // 若有規則可改
			UserName = email.Trim(),
			NormalizedUserName = normalizedUserName,
			Email = email.Trim(),
			NormalizedEmail = normalizedEmail,
			LastName = lastName.Trim(),
			FirstName = firstName.Trim(),
			PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
			Gender = "N/A",
			MemberRankId = DEFAULT_MEMBER_RANK, // 假設 AspNetUsers 有 Rank 欄位
			IsActive = true,
			CreatedDate = DateTime.UtcNow,
			ActivationDate = DateTime.UtcNow,
			EmailConfirmed = true,
			SecurityStamp = Guid.NewGuid().ToString()
		};

		var createRes = await _userMgr.CreateAsync(user, DEFAULT_PASSWORD);
		if (!createRes.Succeeded)
			return BadRequest(new { ok = false, message = string.Join("; ", createRes.Errors.Select(e => e.Description)) });

		var addRoleRes = await _userMgr.AddToRoleAsync(user, MEMBER_ROLE_NAME);
		if (!addRoleRes.Succeeded)
			return BadRequest(new { ok = false, message = string.Join("; ", addRoleRes.Errors.Select(e => e.Description)) });

		return Json(new { ok = true });
	}

	// 就地欄位（姓/名/Email/Phone/啟用）
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateField([FromForm] string userId, [FromForm] string field, [FromForm] string? value)
	{
		var u = await _userMgr.Users.FirstOrDefaultAsync(x => x.Id == userId);
		if (u == null) return NotFound(new { ok = false, message = "找不到使用者" });

		switch (field)
		{
			case "lastName": u.LastName = value?.Trim() ?? ""; break;
			case "firstName": u.FirstName = value?.Trim() ?? ""; break;
			case "email":
				if (string.IsNullOrWhiteSpace(value)) return BadRequest(new { ok = false, message = "Email 必填" });
				var email = value.Trim();
				u.Email = email; u.UserName = email;
				u.NormalizedEmail = _userMgr.NormalizeEmail(email);
				u.NormalizedUserName = _userMgr.NormalizeName(email);
				break;
			case "phoneNumber": u.PhoneNumber = value?.Trim(); break;
			case "isActive":
				u.IsActive = value == "true" || value == "1" || value?.ToLowerInvariant() == "on";
				if (u.IsActive && u.ActivationDate == null) u.ActivationDate = DateTime.UtcNow;
				break;
			default:
				return BadRequest(new { ok = false, message = "不允許的欄位" });
		}

		u.RevisedDate = DateTime.UtcNow;
		var res = await _userMgr.UpdateAsync(u);
		if (!res.Succeeded)
			return BadRequest(new { ok = false, message = string.Join("; ", res.Errors.Select(e => e.Description)) });

		return Json(new { ok = true });
	}

	// 重設密碼為預設值
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ResetPassword([FromForm] string userId)
	{
		var user = await _userMgr.FindByIdAsync(userId);
		if (user == null) return NotFound(new { ok = false, message = "使用者不存在" });

		var token = await _userMgr.GeneratePasswordResetTokenAsync(user);
		var res = await _userMgr.ResetPasswordAsync(user, token, DEFAULT_PASSWORD);
		if (!res.Succeeded)
			return BadRequest(new { ok = false, message = string.Join("; ", res.Errors.Select(e => e.Description)) });

		return Json(new { ok = true });
	}

	private sealed class MemberWithFlag
	{
		public AspNetUser U { get; set; } = default!;
		public bool HasBlack { get; set; }
	}
}
