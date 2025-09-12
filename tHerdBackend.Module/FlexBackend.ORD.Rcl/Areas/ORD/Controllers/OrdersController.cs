using FlexBackend.Infra.Models;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
	[Area("ORD")]
	public class OrdersController : Controller
	{
		private readonly tHerdDBContext _db;

		public OrdersController(tHerdDBContext db)
		{
			_db = db;
		}

		// GET: /ORD/Orders
		public async Task<IActionResult> Index(
			string? Keyword,
			string? OrderStatusId,
			string? PaymentStatus,
			string? ShippingStatusId,
			string SortBy = "CreatedDate",
			string SortDirection = "DESC",
			int PageNumber = 1,
			int PageSize = 10)
		{
			var query = _db.OrdOrders.AsNoTracking().AsQueryable();

			// 關鍵字搜尋
			if (!string.IsNullOrWhiteSpace(Keyword))
			{
				query = query.Where(o =>
					o.OrderNo.Contains(Keyword) ||
					o.ReceiverName.Contains(Keyword) ||
					o.ReceiverPhone.Contains(Keyword));
			}

			// 狀態篩選
			if (!string.IsNullOrEmpty(OrderStatusId))
				query = query.Where(o => o.OrderStatusId == OrderStatusId);

			if (!string.IsNullOrEmpty(PaymentStatus))
				query = query.Where(o => o.PaymentStatus == PaymentStatus);

			if (!string.IsNullOrEmpty(ShippingStatusId))
				query = query.Where(o => o.ShippingStatusId == ShippingStatusId);

			// 排序
			query = (SortBy?.ToLower(), SortDirection?.ToUpper()) switch
			{
				("orderno", "ASC") => query.OrderBy(o => o.OrderNo),
				("orderno", _) => query.OrderByDescending(o => o.OrderNo),

				("paymentstatus", "ASC") => query.OrderBy(o => o.PaymentStatus),
				("paymentstatus", _) => query.OrderByDescending(o => o.PaymentStatus),

				("shippingstatusid", "ASC") => query.OrderBy(o => o.ShippingStatusId),
				("shippingstatusid", _) => query.OrderByDescending(o => o.ShippingStatusId),

				("totalamount", "ASC") => query.OrderBy(o => o.Subtotal - o.DiscountTotal + o.ShippingFee),
				("totalamount", _) => query.OrderByDescending(o => o.Subtotal - o.DiscountTotal + o.ShippingFee),

				("createddate", "ASC") => query.OrderBy(o => o.CreatedDate),
				_ => query.OrderByDescending(o => o.CreatedDate),
			};

			// 分頁
			PageNumber = Math.Max(1, PageNumber);
			PageSize = Math.Clamp(PageSize, 5, 100);
			var totalItems = await query.CountAsync();

			var orderEntities = await query
				.Skip((PageNumber - 1) * PageSize)
				.Take(PageSize)
				.ToListAsync();

			// 狀態字典
			List<string> codeIds = new() { "04", "05", "07" };
			var sysStatuses = await GetsysStatuses("ORD", codeIds);

			var sysPaymentStatus = StatusDictionary("04", sysStatuses);
			var sysShippingStatus = StatusDictionary("05", sysStatuses);
			var sysOrderStatus = StatusDictionary("07", sysStatuses);

			// 包裝 VM
			var orders = orderEntities.Select(o => new OrderListItemVM
			{
				OrderId = o.OrderId,
				OrderNo = o.OrderNo,
				UserNumberId = o.UserNumberId,
				UserName = $"{o.UserNumberId}",
				PaymentStatus = o.PaymentStatus,
				PaymentStatusName = GetSysCodeDesc(o.PaymentStatus, sysPaymentStatus),
				OrderStatusId = o.OrderStatusId,
				OrderStatusName = GetSysCodeDesc(o.OrderStatusId, sysOrderStatus),
				ShippingStatusId = o.ShippingStatusId,
				ShippingStatusName = GetSysCodeDesc(o.ShippingStatusId, sysShippingStatus),
				Subtotal = o.Subtotal,
				DiscountTotal = o.DiscountTotal,
				ShippingFee = o.ShippingFee,
				CreatedDate = o.CreatedDate,
				RevisedDate = o.RevisedDate,
				ReceiverName = o.ReceiverName,
				ReceiverPhone = o.ReceiverPhone
			}).ToList();

			var pageVm = new PaginationVM
			{
				TotalCount = totalItems,
				CurrentPage = PageNumber,
				PageSize = PageSize,
				TotalPages = (int)Math.Ceiling((double)totalItems / PageSize)
			};

			var searchParams = new OrderSearchVM
			{
				Keyword = Keyword,
				OrderStatusId = OrderStatusId,
				PaymentStatus = PaymentStatus,
				ShippingStatusId = ShippingStatusId,
				SortBy = SortBy,
				SortDirection = SortDirection,
				PageNumber = PageNumber,
				PageSize = PageSize
			};

			var vm = new OrderListVM
			{
				Orders = orders,
				Pagination = pageVm,
				SearchParams = searchParams,
				PaymentStatusOptions = sysPaymentStatus,
				ShippingStatusOptions = sysShippingStatus,
				OrderStatusOptions = sysOrderStatus
			};

			return View(vm);
		}

		// 即點即查訂單明細 (AJAX)
		[HttpGet]
		public async Task<IActionResult> GetOrderDetails(int orderId)
		{
			var order = await _db.OrdOrders
				.Where(o => o.OrderId == orderId)
				.Select(o => new
				{
					orderId = o.OrderId,
					receiverName = o.ReceiverName,
					receiverPhone = o.ReceiverPhone,
					receiverAddress = o.ReceiverAddress,
					// 🚀 配送方式從 SUP_Logistics 撈
					shippingMethod = _db.SupLogistics
						.Where(l => l.LogisticsId == o.LogisticsId)
						.Select(l => l.ShippingMethod)
						.FirstOrDefault(),
					couponCode = _db.MktCoupons
						.Where(c => c.CouponId == o.CouponId)
						.Select(c => c.CouponCode)
						.FirstOrDefault(),
					discountTotal = o.DiscountTotal,
					shippingFee = o.ShippingFee,
					totalAmount = o.Subtotal - o.DiscountTotal + o.ShippingFee
				})
				.FirstOrDefaultAsync();

			if (order == null)
				return Json(new { error = "找不到訂單" });

			var items = await _db.OrdOrderItems
				.Where(i => i.OrderId == orderId)
				.Select(i => new
				{
					productName = "商品#" + i.ProductId,
					skuSpec = "SKU#" + i.SkuId,
					unitPrice = i.UnitPrice,
					qty = i.Qty,
					subtotal = i.UnitPrice * i.Qty
				})
				.ToListAsync();

			return Json(new { order, items });
		}





		private static string GetSysCodeDesc(string codeId, IEnumerable<SelectOption> sysStatuses)
		{
			var match = sysStatuses.FirstOrDefault(s => s.Value == codeId);
			return match != null ? match.Text : codeId;
		}

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

		private async Task<List<SysCode>> GetsysStatuses(string ModuleId, List<string> codeIds)
		{
			return await _db.SysCodes
				.Where(c => c.ModuleId == ModuleId && codeIds.Contains(c.CodeId) && c.IsActive)
				.ToListAsync();
		}
	}
}
