using FlexBackend.Infra.Models;
using FlexBackend.ORD.Areas.ORD.ViewModels;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
	[Area("ORD")]
	public class OrdersController : Controller
	{
		private readonly tHerdDBContext _db; // ← 換成你的 DbContext 型別
		public OrdersController(tHerdDBContext db) => _db = db;

		// GET: /ORD/Orders
		public async Task<IActionResult> Index(
			string? q, string sort = "CreatedDate", string dir = "desc",
			int page = 1, int pageSize = 10, bool showHidden = false)
		{
			// 1) 基礎查詢 + 搜尋 + 是否顯示軟刪
			var query = _db.OrdOrders.AsNoTracking().AsQueryable();

			if (!showHidden)
				query = query.Where(o => o.IsVisibleToMember);

			if (!string.IsNullOrWhiteSpace(q))
				query = query.Where(o =>
					o.OrderNo.Contains(q) ||
					o.ReceiverName.Contains(q) ||
					o.ReceiverPhone.Contains(q));

			// 2) 排序（支援：OrderNo / UserNumberId / PaymentStatus / ShippingStatusId / Total / CreatedDate）
			query = (sort, dir.ToLower()) switch
			{
				("OrderNo", "asc") => query.OrderBy(o => o.OrderNo),
				("OrderNo", _) => query.OrderByDescending(o => o.OrderNo),

				("UserNumberId", "asc") => query.OrderBy(o => o.UserNumberId),
				("UserNumberId", _) => query.OrderByDescending(o => o.UserNumberId),

				("PaymentStatus", "asc") => query.OrderBy(o => o.PaymentStatus),
				("PaymentStatus", _) => query.OrderByDescending(o => o.PaymentStatus),

				("ShippingStatusId", "asc") => query.OrderBy(o => o.ShippingStatusId),
				("ShippingStatusId", _) => query.OrderByDescending(o => o.ShippingStatusId),

				("Total", "asc") => query.OrderBy(o => o.Subtotal - o.DiscountTotal + o.ShippingFee),
				("Total", _) => query.OrderByDescending(o => o.Subtotal - o.DiscountTotal + o.ShippingFee),

				// default
				("CreatedDate", "asc") => query.OrderBy(o => o.CreatedDate),
				_ => query.OrderByDescending(o => o.CreatedDate),
			};

			// 3) 總數 + 取分頁資料（先投影成 VM，不包含 Items）
			page = Math.Max(1, page);
			pageSize = Math.Clamp(pageSize, 5, 100);

			var totalItems = await query.CountAsync();

			List<string> codeIds = new List<string> { "04", "07" };
			var sysStatuses = await GetsysStatuses("ORD", codeIds); // 取得所有需要的系統代碼

			// 取得各個狀態
			var sysPaymentStatus = StatusDictionary("04", sysStatuses); // 付款狀態代碼設定
			var sysOrderStatusId = StatusDictionary("07", sysStatuses); // 訂單狀態代碼設定

			var orderEntities = await query
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync(); // 先把資料抓回來

			var orders = orderEntities.Select(o => new OrderListItemVM
			{
				OrderId = o.OrderId,
				OrderNo = o.OrderNo,
				UserNumberId = o.UserNumberId,
				PaymentStatus = GetSysCodeDesc(o.PaymentStatus, sysPaymentStatus), // 現在是記憶體操作
				OrderStatusId = o.OrderStatusId,
				ShippingStatusId = o.ShippingStatusId,
				Subtotal = o.Subtotal,
				DiscountTotal = o.DiscountTotal,
				ShippingFee = o.ShippingFee,
				CreatedDate = o.CreatedDate,
			}).ToList();

			// 4) 批次載入明細給展開列（避免 N+1）
			var orderIds = orders.Select(x => x.OrderId).ToList();
			var items = await _db.OrdOrderItems.AsNoTracking()
				.Where(i => orderIds.Contains(i.OrderId))
				.Select(i => new OrderItemVM
				{
					OrderId = i.OrderId,
					OrderItemId = i.OrderItemId,
					ProductId = i.ProductId,
					SkuId = i.SkuId,
					UnitPrice = i.UnitPrice,
					Qty = i.Qty
				})
				.ToListAsync();

			var grouped = items.GroupBy(i => i.OrderId)
							   .ToDictionary(g => g.Key, g => g.ToList());

			foreach (var o in orders)
				o.Items = grouped.TryGetValue(o.OrderId, out var list) ? list : new List<OrderItemVM>();

			// 5) 付款狀態中文（可選；若不需要可移除）
			ViewBag.PaymentStatusMap = await _db.SysCodes
				.Where(c => c.ModuleId == "ORD" && c.CodeId == "04")
				.ToDictionaryAsync(c => c.CodeNo, c => c.CodeDesc);

			// 1) 建立分頁資訊
			var pageVm = new PaginationVM
			{
				TotalCount = totalItems,
				CurrentPage = page,
				PageSize = pageSize
			};

			// 2) 包裝成 OrderListVM
			var vm = new OrderListVM
			{
				Orders = orders.ToList(),  // 這裡 orders 是 List<OrderListItemVM>
				Pagination = pageVm,
				PaymentStatusOptions = sysPaymentStatus
			};

			return View(vm);
		}

		// 根據代碼 ID 從字典中取得描述，若找不到則回傳原始代碼
		public static string GetSysCodeDesc(string codeId, IEnumerable<SelectOption> sysStatuses)
		{
			var match = sysStatuses.FirstOrDefault(s => s.Value == codeId);
			return match != null ? match.Text : codeId;
		}

		// 取得指定 CodeId 的狀態字典
		private IEnumerable<SelectOption> StatusDictionary(string codeId, List<SysCode> sysStatuses)
		{
			return sysStatuses
				.Where(c => c.CodeId == codeId)
				.Select(x => new SelectOption
				{
					Value = x.CodeNo,
					Text = x.CodeDesc
				})
				.ToList();
		}

		// 取得多個系統代碼
		private async Task<List<SysCode>> GetsysStatuses(string ModuleId, List<string> codeIds)
		{
			return await _db.SysCodes
				.Where(c => c.ModuleId == ModuleId && codeIds.Contains(c.CodeId) && c.IsActive)
				.ToListAsync();  // 返回符合條件的 SysCode 列表
		}
	}
}
