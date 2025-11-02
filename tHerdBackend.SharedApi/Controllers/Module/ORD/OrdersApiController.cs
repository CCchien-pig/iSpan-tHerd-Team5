using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.ORD.Rcl.Areas.ORD.ApiControllers
{
    [Area("ORD")]
    [Route("api/ord/[controller]")]
    [ApiController]
    public class OrdersApiController : ControllerBase
    {
        private readonly tHerdDBContext _db;
        public OrdersApiController(tHerdDBContext db) => _db = db;
        [HttpGet("member/{userNumberId}")]
        public async Task<IActionResult> GetMemberOrders(int userNumberId)
        {
            // 先取得所有符合條件的訂單
            var ordersList = await _db.OrdOrders
                .Where(o => o.UserNumberId == userNumberId && o.IsVisibleToMember)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            // 取得所有 RMA 記錄
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
                deliveredDate = o.DeliveredDate,
                trackingNumber = o.TrackingNumber,
                hasRmaRequest = rmaOrderIds.Contains(o.OrderId), // 改用 Contains 檢查
                canReturn = o.DeliveredDate.HasValue &&
                           (DateTime.Now - o.DeliveredDate.Value).Days <= 7 &&
                           !rmaOrderIds.Contains(o.OrderId)
            }).ToList();

            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId, [FromQuery] int userNumberId)
        {
            var order = await _db.OrdOrders
                .Where(o => o.OrderId == orderId && o.UserNumberId == userNumberId && o.IsVisibleToMember)
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
                    deliveredDate = o.DeliveredDate,
                    canReturn = o.DeliveredDate.HasValue &&
                               EF.Functions.DateDiffDay(o.DeliveredDate.Value, DateTime.Now) <= 7 &&
                               !_db.OrdReturnRequests.Any(r => r.OrderId == o.OrderId)
                })
                .FirstOrDefaultAsync();

            if (order == null) return NotFound();

            var items = await _db.OrdOrderItems
                .Where(i => i.OrderId == orderId)
                .Join(_db.ProdProducts, i => i.ProductId, p => p.ProductId, (i, p) => new { i, p })
                .Join(_db.ProdProductSkus, x => x.i.SkuId, s => s.SkuId, (x, s) => new
                {
                    orderItemId = x.i.OrderItemId,
                    productName = x.p.ProductName,
                    specCode = s.SpecCode,
                    unitPrice = x.i.UnitPrice,
                    qty = x.i.Qty,
                    subtotal = x.i.UnitPrice * x.i.Qty
                })
                .ToListAsync();

            return Ok(new { order, items });
        }

        [HttpPost("rma")]
        public async Task<IActionResult> CreateRma([FromBody] CreateRmaRequest req)
        {
            var order = await _db.OrdOrders.FindAsync(req.OrderId);
            if (order == null || order.UserNumberId != req.UserNumberId)
                return BadRequest("訂單不存在");

            // 檢查是否已經申請過
            var existingRma = await _db.OrdReturnRequests
                .AnyAsync(r => r.OrderId == req.OrderId);
            if (existingRma)
                return BadRequest("此訂單已經申請過退換貨");

            if (!order.DeliveredDate.HasValue)
                return BadRequest("訂單尚未送達");

            var daysSinceDelivery = (DateTime.Now - order.DeliveredDate.Value).Days;
            if (daysSinceDelivery > 7)
                return BadRequest("已超過7天鑑賞期");

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
                Creator = req.UserNumberId,
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

        [HttpGet("member/{userNumberId}/rma")]
        public async Task<IActionResult> GetMemberRmaList(int userNumberId)
        {
            var rmaList = await _db.OrdReturnRequests
                .Where(r => r.Creator == userNumberId)
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new
                {
                    returnRequestId = r.ReturnRequestId,
                    rmaId = r.RmaId,
                    orderNo = r.Order.OrderNo,
                    requestType = r.RequestType,
                    refundScope = r.RefundScope,
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

    public class CreateRmaRequest
    {
        public int OrderId { get; set; }
        public int UserNumberId { get; set; }
        public string RequestType { get; set; }
        public string RefundScope { get; set; }
        public string ReasonCode { get; set; }
        public string Reason { get; set; }
        public List<RmaItemRequest> Items { get; set; }
    }

    public class RmaItemRequest
    {
        public int OrderItemId { get; set; }
        public int Qty { get; set; }
    }
}