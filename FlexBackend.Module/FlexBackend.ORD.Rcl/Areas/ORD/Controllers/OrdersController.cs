using FlexBackend.Infra.Models;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
    [Area("ORD")]
    public class OrdersController : Controller
    {
        private readonly tHerdDBContext _db;
		private readonly ILogger<OrdersController> _logger;
		
		public OrdersController(tHerdDBContext db, ILogger<OrdersController> logger)
		{
			_db = db;
			_logger = logger;
		}

		// GET: /ORD/Orders
		public async Task<IActionResult> Index(
            string? Keyword,           // 改為符合 View 的參數名稱
            string SortBy = "CreatedDate", 
            string SortDirection = "DESC",
            int PageNumber = 1, 
            int PageSize = 10, 
            bool showHidden = false)
        {
            // 1) 基礎查詢 + 搜尋 + 是否顯示軟刪
            var query = _db.OrdOrders.AsNoTracking().AsQueryable();

            if (!showHidden)
                query = query.Where(o => o.IsVisibleToMember);

            if (!string.IsNullOrWhiteSpace(Keyword))
                query = query.Where(o =>
                    o.OrderNo.Contains(Keyword) ||
                    o.ReceiverName.Contains(Keyword) ||
                    o.ReceiverPhone.Contains(Keyword));

            // 2) 排序
            query = (SortBy?.ToLower(), SortDirection?.ToUpper()) switch
            {
                ("orderno", "ASC") => query.OrderBy(o => o.OrderNo),
                ("orderno", _) => query.OrderByDescending(o => o.OrderNo),
                
                ("usernumberid", "ASC") => query.OrderBy(o => o.UserNumberId),
                ("usernumberid", _) => query.OrderByDescending(o => o.UserNumberId),
                
                ("paymentstatus", "ASC") => query.OrderBy(o => o.PaymentStatus),
                ("paymentstatus", _) => query.OrderByDescending(o => o.PaymentStatus),
                
                ("shippingstatusid", "ASC") => query.OrderBy(o => o.ShippingStatusId),
                ("shippingstatusid", _) => query.OrderByDescending(o => o.ShippingStatusId),
                
                ("totalamount", "ASC") => query.OrderBy(o => o.Subtotal - o.DiscountTotal + o.ShippingFee),
                ("totalamount", _) => query.OrderByDescending(o => o.Subtotal - o.DiscountTotal + o.ShippingFee),
                
                // default
                ("createddate", "ASC") => query.OrderBy(o => o.CreatedDate),
                _ => query.OrderByDescending(o => o.CreatedDate),
            };

            // 3) 總數 + 取分頁資料
            PageNumber = Math.Max(1, PageNumber);
            PageSize = Math.Clamp(PageSize, 5, 100);

            var totalItems = await query.CountAsync();

            // 取得所有需要的系統代碼 (包含配送狀態 05)
            List<string> codeIds = new List<string> { "04", "05", "07" };
            var sysStatuses = await GetsysStatuses("ORD", codeIds);

            // 取得各個狀態字典
            var sysPaymentStatus = StatusDictionary("04", sysStatuses);   // 付款狀態
            var sysShippingStatus = StatusDictionary("05", sysStatuses);  // 配送狀態
            var sysOrderStatus = StatusDictionary("07", sysStatuses);     // 訂單狀態

            var orderEntities = await query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var orders = orderEntities
                .Select(o => new OrderListItemVM
                {
                    OrderId = o.OrderId,
                    OrderNo = o.OrderNo,
                    UserNumberId = o.UserNumberId,
                    UserName = $"{o.UserNumberId}",
                    
                    // 付款狀態
                    PaymentStatus = o.PaymentStatus,
                    PaymentStatusName = GetSysCodeDesc(o.PaymentStatus, sysPaymentStatus),
                    
                    // 訂單狀態
                    OrderStatusId = o.OrderStatusId,
                    OrderStatusName = GetSysCodeDesc(o.OrderStatusId, sysOrderStatus),
                    
                    // 配送狀態
                    ShippingStatusId = o.ShippingStatusId,
                    ShippingStatusName = GetSysCodeDesc(o.ShippingStatusId.ToString(), sysShippingStatus),
                    
                    Subtotal = o.Subtotal,
                    DiscountTotal = o.DiscountTotal,
                    ShippingFee = o.ShippingFee,
                    CreatedDate = o.CreatedDate,
                    RevisedDate = o.RevisedDate,
                    ReceiverName = o.ReceiverName,
                    ReceiverPhone = o.ReceiverPhone,
                    HasShippingLabel = o.HasShippingLabel
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

            // 5) 建立分頁資訊
            var pageVm = new PaginationVM
            {
                TotalCount = totalItems,
                CurrentPage = PageNumber,
                PageSize = PageSize,
                TotalPages = (int)Math.Ceiling((double)totalItems / PageSize)
            };

            // 6) 建立搜尋參數
            var searchParams = new OrderSearchVM
            {
                Keyword = Keyword,
                SortBy = SortBy,
                SortDirection = SortDirection,
                PageNumber = PageNumber,
                PageSize = PageSize
            };

            // 7) 建立排序選項
            var sortOptions = new List<SortOption>
            {
                new SortOption { Value = "CreatedDate", Text = "建立時間", Direction = "DESC" },
                new SortOption { Value = "OrderNo", Text = "訂單編號", Direction = "ASC" },
                new SortOption { Value = "TotalAmount", Text = "訂單金額", Direction = "DESC" },
                new SortOption { Value = "PaymentStatus", Text = "付款狀態", Direction = "ASC" },
                new SortOption { Value = "ShippingStatusId", Text = "配送狀態", Direction = "ASC" }
            };

            // 8) 包裝成 OrderListVM
            var vm = new OrderListVM
            {
                Orders = orders,
                Pagination = pageVm,
                SearchParams = searchParams,
                SortOptions = sortOptions,
                PaymentStatusOptions = sysPaymentStatus,
                ShippingStatusOptions = sysShippingStatus,
                OrderStatusOptions = sysOrderStatus
            };

            return View(vm);
        }

		// 取得訂單明細 (AJAX)
		[HttpGet]
		public async Task<IActionResult> GetOrderDetails(int orderId)
		{
			try
			{
				// 先檢查訂單是否存在
				var orderExists = await _db.OrdOrders.AnyAsync(o => o.OrderId == orderId);
				if (!orderExists)
				{
					return Json(new { error = $"訂單 {orderId} 不存在" });
				}

				// 查詢訂單明細
				var items = await _db.OrdOrderItems
					.Where(i => i.OrderId == orderId)
					.Select(i => new
					{
						orderItemId = i.OrderItemId,
						orderId = i.OrderId,
						productId = i.ProductId,
						skuId = i.SkuId,
						unitPrice = i.UnitPrice,
						qty = i.Qty,
						productName = $"商品#{i.ProductId}",
						skuSpec = $"SKU#{i.SkuId}",
						subtotal = i.UnitPrice * i.Qty
					})
					.ToListAsync();

				// 記錄查詢結果
				_logger?.LogInformation($"訂單 {orderId} 查詢到 {items.Count} 筆明細");

				// 如果沒有明細，回傳空陣列而不是錯誤
				if (items.Count == 0)
				{
					return Json(new List<object>());
				}

				return Json(items);
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, $"查詢訂單 {orderId} 明細時發生錯誤");
				return Json(new { error = $"系統錯誤: {ex.Message}" });
			}
		}



		// 更新訂單狀態 (AJAX)
		[HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] OrderStatusUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "資料驗證失敗" });
            }

            try
            {
                var order = await _db.OrdOrders.FindAsync(model.OrderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "找不到指定的訂單" });
                }

                if (string.IsNullOrEmpty(model.OrderStatusId) == false)
                {
                    order.OrderStatusId = model.OrderStatusId;
                }

                if (string.IsNullOrEmpty(model.ShippingStatusId) == false)
                {
                    order.ShippingStatusId = model.ShippingStatusId;
                }

                order.RevisedDate = DateTime.Now;
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "狀態更新成功" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "更新失敗: " + ex.Message });
            }
        }

        // 根據代碼 ID 從字典中取得描述
        private static string GetSysCodeDesc(string codeId, IEnumerable<SelectOption> sysStatuses)
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
                .ToListAsync();
        }

	}
}