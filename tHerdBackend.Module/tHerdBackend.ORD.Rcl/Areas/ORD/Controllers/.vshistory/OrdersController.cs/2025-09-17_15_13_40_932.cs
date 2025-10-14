using FlexBackend.Infra.Models;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
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
			DateTime? StartDate = null,   // ← 改名
			DateTime? EndDate = null,
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


            // 日期篩選（含當日）
            if (StartDate.HasValue)
                query = query.Where(o => o.CreatedDate >= StartDate.Value.Date);

            if (EndDate.HasValue)
            {
                var to = EndDate.Value.Date.AddDays(1);
                query = query.Where(o => o.CreatedDate < to);
            }



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
                UserName = _db.AspNetUsers
                .Where(m => m.UserNumberId == o.UserNumberId)
				.Select(m => m.LastName + m.FirstName) 
				.FirstOrDefault(),
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
				ReceiverPhone = o.ReceiverPhone,

                IsVisibleToMember = o.IsVisibleToMember
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
				PageSize = PageSize,


                StartDate = StartDate?.Date,
                EndDate = EndDate?.Date
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
			// 撈訂單主檔
			var order = await _db.OrdOrders
				.Where(o => o.OrderId == orderId)
				.Select(o => new
				{
					orderId = o.OrderId,
					receiverName = o.ReceiverName,
					receiverPhone = o.ReceiverPhone,
					receiverAddress = o.ReceiverAddress,
					shippingMethod = _db.SupLogistics
						.Where(l => l.LogisticsId == o.LogisticsId)
						.Select(l => l.ShippingMethod)
						.FirstOrDefault(),
                    trackingNumber = o.TrackingNumber,
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

			// 撈訂單明細 (Join Product 與 Sku)
			var items = await (
				from i in _db.OrdOrderItems
				join p in _db.ProdProducts on i.ProductId equals p.ProductId
				join s in _db.ProdProductSkus on i.SkuId equals s.SkuId
				where i.OrderId == orderId
				select new
				{
					productName = p.ProductName,   
					skuSpec = s.SpecCode,          
					unitPrice = i.UnitPrice,
					qty = i.Qty,
					subtotal = i.UnitPrice * i.Qty
				}).ToListAsync();

			return Json(new { order, items });
		}


        private const string SHIPPING_SHIPPED_CODE = "shipped";
        // 產出出貨單號
        private static string GenerateTrackingNo(int orderId)
        {
            // 例：TRK250917000123
            return $"TRK{DateTime.UtcNow:yyMMdd}{orderId:D6}";
        }


        // 即點即改訂單狀態 (AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string field, string value)
        {
            var order = await _db.OrdOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return Json(new { success = false, message = "找不到訂單" });

            try
            {
                switch (field)
                {
                    case "PaymentStatus":
                        order.PaymentStatus = value;
                        break;

                    case "ShippingStatusId":
                        order.ShippingStatusId = value;

                        // 若設為 shipped 且目前沒有單號 → 自動產生
                        if (string.Equals(value, SHIPPING_SHIPPED_CODE, StringComparison.OrdinalIgnoreCase)
                            && string.IsNullOrEmpty(order.TrackingNumber))
                        {
                            order.TrackingNumber = GenerateTrackingNo(order.OrderId);
                        }
                        break;

                    case "OrderStatusId":
                        order.OrderStatusId = value;
                        break;
                }

                order.RevisedDate = DateTime.Now;
                await _db.SaveChangesAsync();

                return Json(new { success = true, trackingNo = order.TrackingNumber });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        //      // 即點即改訂單狀態 (AJAX)
        //      [HttpPost]
        //public async Task<IActionResult> UpdateOrderStatus(int orderId, string field, string value)
        //{
        //	var order = await _db.OrdOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        //	if (order == null)
        //	{
        //		return Json(new { success = false, message = "找不到訂單" });
        //	}

        //	try
        //	{
        //		switch (field)
        //		{
        //			case "PaymentStatus":
        //				order.PaymentStatus = value; // 存 CodeNo
        //				break;
        //			case "ShippingStatusId":
        //				order.ShippingStatusId = value;
        //				break;
        //			case "OrderStatusId":
        //				order.OrderStatusId = value;
        //				break;
        //		}

        //		await _db.SaveChangesAsync();
        //		return Json(new { success = true });
        //	}
        //	catch (Exception ex)
        //	{
        //		return Json(new { success = false, message = ex.Message });
        //	}
        //}



        // 批量更新訂單狀態
        [HttpPost]
        public async Task<IActionResult> BulkUpdateOrders(
		[FromForm] List<int> orderIds,
		[FromForm] string? shippingStatusId,
		[FromForm] string? orderStatusId)
        {
            if (orderIds == null || !orderIds.Any())
                return Json(new { success = false, message = "沒有選擇訂單" });

            try
            {
                var orders = await _db.OrdOrders
                                      .Where(o => orderIds.Contains(o.OrderId))
                                      .ToListAsync();

                if (!orders.Any())
                    return Json(new { success = false, message = "找不到訂單" });

                foreach (var order in orders)
                {
                    if (!string.IsNullOrEmpty(shippingStatusId))
                    {
                        order.ShippingStatusId = shippingStatusId;

                        if (string.Equals(shippingStatusId, SHIPPING_SHIPPED_CODE, StringComparison.OrdinalIgnoreCase)
                            && string.IsNullOrEmpty(order.TrackingNumber))
                        {
                            order.TrackingNumber = GenerateTrackingNo(order.OrderId);
                        }
                    }

                    if (!string.IsNullOrEmpty(orderStatusId))
                        order.OrderStatusId = orderStatusId;

                    order.RevisedDate = DateTime.Now;
                }

                await _db.SaveChangesAsync();

                var generated = orders.Where(o => !string.IsNullOrEmpty(o.TrackingNumber))
                                      .Select(o => new { o.OrderId, o.TrackingNumber })
                                      .ToList();

                return Json(new { success = true, generated });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        //      // ======================
        //      // 批量更新訂單狀態
        //      // ======================
        //      [HttpPost]
        //public async Task<IActionResult> BulkUpdateOrders(
        //[FromForm] List<int> orderIds,
        //[FromForm] string? shippingStatusId,
        //[FromForm] string? orderStatusId)
        //{
        //	if (orderIds == null || !orderIds.Any())
        //		return Json(new { success = false, message = "沒有選擇訂單" });

        //	try
        //	{
        //		var orders = await _db.OrdOrders
        //			.Where(o => orderIds.Contains(o.OrderId))
        //			.ToListAsync();

        //		if (!orders.Any())
        //			return Json(new { success = false, message = "找不到訂單" });

        //		foreach (var order in orders)
        //		{
        //			if (!string.IsNullOrEmpty(shippingStatusId))
        //				order.ShippingStatusId = shippingStatusId;

        //			if (!string.IsNullOrEmpty(orderStatusId))
        //				order.OrderStatusId = orderStatusId;

        //			order.RevisedDate = DateTime.Now;
        //		}

        //		await _db.SaveChangesAsync();
        //		return Json(new { success = true });
        //	}
        //	catch (Exception ex)
        //	{
        //		return Json(new { success = false, message = ex.Message });
        //	}
        //}


        // ======================
        // 批量列印出貨單
        // ======================
        public async Task<IActionResult> PrintOrders(string ids)
		{
			if (string.IsNullOrEmpty(ids))
				return Content("沒有選擇訂單");

			var orderIds = ids.Split(',').Select(int.Parse).ToList();

			var orders = await _db.OrdOrders
				.Where(o => orderIds.Contains(o.OrderId))
				.Select(o => new PrintOrderVM
				{
					Order = o,
					ShippingMethod = _db.SupLogistics
						.Where(l => l.LogisticsId == o.LogisticsId)
						.Select(l => l.ShippingMethod)
						.FirstOrDefault(),
					Items = _db.OrdOrderItems
						.Where(i => i.OrderId == o.OrderId)
						.Join(_db.ProdProducts, i => i.ProductId, p => p.ProductId,
							(i, p) => new { i, p.ProductName })
						.Join(_db.ProdProductSkus, ip => ip.i.SkuId, s => s.SkuId,
							(ip, s) => new PrintOrderItemVM
							{
								ProductName = ip.ProductName,
								SpecCode = s.SpecCode,
								UnitPrice = ip.i.UnitPrice,
								Qty = ip.i.Qty,
								Subtotal = ip.i.UnitPrice * ip.i.Qty
							})
						.ToList()
				})
				.ToListAsync();

			return View("PrintOrders", orders);
		}

        // ======================
        // 切換訂單對會員是否可見
        // ======================

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int orderId, bool visible)
        {
            var order = await _db.OrdOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return Json(new { success = false, message = "找不到訂單" });

            order.IsVisibleToMember = visible;
            order.RevisedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new { success = true, visible });
        }



        // 讀取 sysCode 的描述
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
