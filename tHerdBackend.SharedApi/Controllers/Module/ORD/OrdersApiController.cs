using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.Models;
using tHerdBackend.Services.Common; // ICurrentUserService

namespace tHerdBackend.ORD.Rcl.Areas.ORD.ApiControllers
{
	[Area("ORD")]
	[Route("api/ord/[controller]")]
	[ApiController]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public class OrdersApiController : ControllerBase
	{
		private readonly tHerdDBContext _db;
		private readonly ICurrentUserService _current;

		public OrdersApiController(tHerdDBContext db, ICurrentUserService current)
		{
			_db = db;
			_current = current;
		}

		// ✅ 取得「自己的」訂單（不收 route id）
		// GET /api/ord/OrdersApi/member/me
		[HttpGet("member/me")]
		public async Task<IActionResult> GetMyOrders()
		{
			var userNumberId = _current.GetRequiredUserNumberId();

			var ordersList = await _db.OrdOrders
				.Where(o => o.UserNumberId == userNumberId && o.IsVisibleToMember)
				.OrderByDescending(o => o.CreatedDate)
				.ToListAsync();

			var rmaOrderIds = await _db.OrdReturnRequests
				.Select(r => r.OrderId)
				.Distinct()
				.ToListAsync();

			var orders = ordersList.Select(o => new
			{
				orderId = o.OrderId,
				orderNo = o.OrderNo,
				orderStatusId = o.OrderStatusId,
				paymentStatus = o.PaymentStatus,
				shippingStatusId = o.ShippingStatusId,
				subtotal = o.Subtotal,
				discountTotal = o.DiscountTotal,
				shippingFee = o.ShippingFee,
				totalAmount = o.Subtotal - o.DiscountTotal + o.ShippingFee,
				createdDate = o.CreatedDate,
				revisedDate = o.RevisedDate,          
				deliveredDate = o.DeliveredDate,
				trackingNumber = o.TrackingNumber,
				hasRmaRequest = rmaOrderIds.Contains(o.OrderId),
				canReturn = o.DeliveredDate.HasValue &&
							(DateTime.Now - o.DeliveredDate.Value).TotalDays < 7 &&
							!rmaOrderIds.Contains(o.OrderId)
			});

			return Ok(orders);
		}

		// ✅ 單筆詳情（不再接 userNumberId；從身分取）
		// GET /api/ord/OrdersApi/{orderId}
		[HttpGet("{orderId}")]
		public async Task<IActionResult> GetOrderDetail(int orderId)
		{
			var userNumberId = _current.GetRequiredUserNumberId();

			var order = await _db.OrdOrders
				.Where(o => o.OrderId == orderId &&
							o.UserNumberId == userNumberId &&
							o.IsVisibleToMember)
				.Select(o => new
				{
					orderId = o.OrderId,
					orderNo = o.OrderNo,
					receiverName = o.ReceiverName,
					receiverPhone = o.ReceiverPhone,
					receiverAddress = o.ReceiverAddress,
					subtotal = o.Subtotal,
					discountTotal = o.DiscountTotal,
					shippingFee = o.ShippingFee,
					totalAmount = o.Subtotal - o.DiscountTotal + o.ShippingFee,
					paymentStatus = o.PaymentStatus,
					shippingStatusId = o.ShippingStatusId,
					orderStatusId = o.OrderStatusId,
					trackingNumber = o.TrackingNumber,
					createdDate = o.CreatedDate,
					revisedDate = o.RevisedDate,     
					deliveredDate = o.DeliveredDate,
					canReturn = o.DeliveredDate.HasValue &&
								EF.Functions.DateDiffDay(o.DeliveredDate.Value, DateTime.Now) < 7 &&
								!_db.OrdReturnRequests.Any(r => r.OrderId == o.OrderId)
				})
				.FirstOrDefaultAsync();

			if (order == null)
				throw new KeyNotFoundException("找不到訂單或無權存取");

			var items = await _db.OrdOrderItems
				.Where(i => i.OrderId == orderId)
				.Join(_db.ProdProducts, i => i.ProductId, p => p.ProductId, (i, p) => new { i, p })
				.Join(_db.ProdProductSkus, x => x.i.SkuId, s => s.SkuId, (x, s) => new
				{
					orderItemId = x.i.OrderItemId,
					productName = x.p.ProductName,
					specName = s.SpecCode,
					unitPrice = x.i.UnitPrice,
					qty = x.i.Qty,
					subtotal = x.i.UnitPrice * x.i.Qty
				})
				.ToListAsync();

			return Ok(new { order, items });
		}

		// ✅ 建立 RMA（使用目前登入者的 UserNumberId，不信任 body 的 userNumberId）
		// POST /api/ord/OrdersApi/rma
		[HttpPost("rma")]
		public async Task<IActionResult> CreateRma([FromBody] CreateRmaRequest req)
		{
			var userNumberId = _current.GetRequiredUserNumberId();

			var order = await _db.OrdOrders.FindAsync(req.OrderId);
			if (order == null || order.UserNumberId != userNumberId)
				throw new KeyNotFoundException("訂單不存在或無權限");

			var existingRma = await _db.OrdReturnRequests
				.AnyAsync(r => r.OrderId == req.OrderId);
			if (existingRma)
				throw new ArgumentException("此訂單已經申請過退換貨");

			if (!order.DeliveredDate.HasValue)
				throw new ArgumentException("訂單尚未送達");

			var daysSinceDelivery = (DateTime.Now - order.DeliveredDate.Value).TotalDays;
			if (daysSinceDelivery > 7)
				throw new ArgumentException("已超過7天鑑賞期");

			var rmaId = $"RMA{DateTime.Now:yyyyMMddHHmmss}";

			var returnRequest = new OrdReturnRequest
			{
				RmaId = rmaId,
				OrderId = req.OrderId,
				RequestType = req.RequestType,
				RefundScope = req.RefundScope,
				ReasonCode = req.ReasonCode,
				ReasonText = req.Reason,
				Status = "pending",
				Creator = userNumberId,
				CreatedDate = DateTime.Now
			};

			_db.OrdReturnRequests.Add(returnRequest);
			await _db.SaveChangesAsync();

			if (req.RefundScope == "items" && req.Items?.Any() == true)
			{
				foreach (var item in req.Items)
				{
					_db.OrdReturnItems.Add(new OrdReturnItem
					{
						ReturnRequestId = returnRequest.ReturnRequestId,
						OrderId = req.OrderId,
						OrderItemId = item.OrderItemId,
						Qty = item.Qty,
						ApprovedQty = 0,
						RefundQty = 0,
						ReshipQty = 0,
						CreatedDate = DateTime.Now
					});
				}
				await _db.SaveChangesAsync();
			}

			return Ok(new { rmaId = returnRequest.RmaId, returnRequestId = returnRequest.ReturnRequestId });
		}

		// ✅ 我的 RMA 清單（改用登入者）
		// GET /api/ord/OrdersApi/member/me/rma
		[HttpGet("member/me/rma")]
		public async Task<IActionResult> GetMyRmaList()
		{
			var userNumberId = _current.GetRequiredUserNumberId();

			var rmaList = await _db.OrdReturnRequests
				.Where(r => r.Creator == userNumberId)
				.OrderByDescending(r => r.CreatedDate)
				.Select(r => new
				{
					returnRequestId = r.ReturnRequestId,
					orderId = r.OrderId,              
					rmaId = r.RmaId,
					orderNo = r.Order.OrderNo,
					requestType = r.RequestType,
					scope = r.RefundScope,            
					status = r.Status,
					reasonText = r.ReasonText,
					createdDate = r.CreatedDate,
					reviewedDate = r.ReviewedDate,
					reviewComment = r.ReviewComment
				})
				.ToListAsync();

			return Ok(rmaList);
		}
	}

	// 舊前端如果還在送 userNumberId，可移除；保留 model 僅為了 items 結構
	public class CreateRmaRequest
	{
		public int OrderId { get; set; }
		public string RequestType { get; set; } = "refund"; // refund | reship
		public string RefundScope { get; set; } = "order";  // order | items
		public string ReasonCode { get; set; } = "other";
		public string Reason { get; set; } = "";
		public List<RmaItemRequest>? Items { get; set; }
	}

	public class RmaItemRequest
	{
		public int OrderItemId { get; set; }
		public int Qty { get; set; }
	}
}