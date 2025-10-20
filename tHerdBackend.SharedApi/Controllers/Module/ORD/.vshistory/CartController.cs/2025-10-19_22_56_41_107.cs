using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tHerdBackend.SharedApi.Controllers.Module.ORD
{
    [ApiController]
    [Route("api/ord/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly tHerdDBContext _context;

        public CartController(tHerdDBContext context)
        {
            _context = context;
        }

        // GET: api/ord/Cart/items?sessionId=xxx&userNumberId=1
        /// <summary>
        /// 取得購物車內容
        /// </summary>
        [HttpGet("items")]
        public async Task<IActionResult> GetCartItems(
            [FromQuery] string sessionId,
            [FromQuery] int? userNumberId)
        {
            try
            {
                var cart = await _context.OrdShoppingCarts
                    .Include(c => c.OrdShoppingCartItems)
                    .FirstOrDefaultAsync(c =>
                        (userNumberId.HasValue && c.UserNumberId == userNumberId) ||
                        (!userNumberId.HasValue && c.SessionId == sessionId));

                if (cart == null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            items = new List<object>(),
                            totalQty = 0,
                            subtotal = 0
                        },
                        message = "購物車為空"
                    });
                }

                var skuIds = cart.OrdShoppingCartItems.Select(i => i.SkuId).ToList();
                var productIds = cart.OrdShoppingCartItems.Select(i => i.ProductId).ToList();

                var productDetails = await (
                    from ps in _context.ProdProductSkus
                    join p in _context.ProdProducts on ps.ProductId equals p.ProductId
                    join sm in _context.SysSeoMetaAssets on p.SeoId equals sm.SeoId into smGroup
                    from sm in smGroup.Where(x => x.IsPrimary == true).DefaultIfEmpty()
                    join af in _context.SysAssetFiles on sm.FileId equals af.FileId into afGroup
                    from af in afGroup.DefaultIfEmpty()
                    where skuIds.Contains(ps.SkuId) && productIds.Contains(p.ProductId)
                    select new
                    {
                        ps.SkuId,
                        ps.ProductId,
                        p.ProductName,
                        FileUrl = af != null ? af.FileUrl : "",
                        ps.SalePrice,
                        ps.UnitPrice,
                        ps.SkuCode,
                        p.IsPublished
                    }
                ).ToListAsync();

                var items = cart.OrdShoppingCartItems.Select(item =>
                {
                    var detail = productDetails.FirstOrDefault(d =>
                        d.SkuId == item.SkuId && d.ProductId == item.ProductId);

                    return new
                    {
                        cartItemId = item.CartItemId,
                        productId = item.ProductId,
                        skuId = item.SkuId,
                        productName = detail?.ProductName ?? "未知商品",
                        optionName = detail?.SkuCode ?? "預設規格",
                        unitPrice = detail?.UnitPrice ?? 0,
                        salePrice = detail?.SalePrice ?? 0,
                        quantity = item.Qty,
                        subtotal = (detail?.SalePrice ?? 0) * item.Qty,
                        imageUrl = detail?.FileUrl ?? "",
                        isPublished = detail?.IsPublished ?? false
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items = items,
                        totalQty = items.Sum(i => i.quantity),
                        subtotal = items.Sum(i => i.subtotal)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"取得購物車失敗: {ex.Message}"
                });
            }
        }

        // POST: api/ord/Cart/add
        /// <summary>
        /// 加入購物車
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var cart = await _context.OrdShoppingCarts
                    .Include(c => c.OrdShoppingCartItems)
                    .FirstOrDefaultAsync(c =>
                        (request.UserNumberId.HasValue && c.UserNumberId == request.UserNumberId) ||
                        (!request.UserNumberId.HasValue && c.SessionId == request.SessionId));

                if (cart == null)
                {
                    cart = new OrdShoppingCart
                    {
                        SessionId = request.SessionId,
                        UserNumberId = request.UserNumberId,
                        MaxItemsAllowed = request.UserNumberId.HasValue ? 10 : 5,
                        CreatedDate = DateTime.Now,
                        RevisedDate = DateTime.Now
                    };
                    _context.OrdShoppingCarts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                if (cart.OrdShoppingCartItems.Count >= cart.MaxItemsAllowed)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"購物車已達上限 ({cart.MaxItemsAllowed} 項)"
                    });
                }

                var productSku = await (
                    from ps in _context.ProdProductSkus
                    join p in _context.ProdProducts on ps.ProductId equals p.ProductId
                    where ps.SkuId == request.SkuId
                          && ps.ProductId == request.ProductId
                          && ps.IsActive == true
                          && p.IsPublished == true
                    select new
                    {
                        ps.SkuId,
                        ps.ProductId,
                        ps.SalePrice,
                        ps.UnitPrice,
                        p.ProductName
                    }
                ).FirstOrDefaultAsync();

                if (productSku == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "商品不存在或已下架"
                    });
                }

                var existingItem = cart.OrdShoppingCartItems
                    .FirstOrDefault(i => i.SkuId == request.SkuId && i.ProductId == request.ProductId);

                if (existingItem != null)
                {
                    existingItem.Qty += request.Quantity;
                    existingItem.UnitPrice = productSku.SalePrice;
                    _context.OrdShoppingCartItems.Update(existingItem);
                }
                else
                {
                    var newItem = new OrdShoppingCartItem
                    {
                        CartId = cart.CartId,
                        ProductId = request.ProductId,
                        SkuId = request.SkuId,
                        Qty = request.Quantity,
                        UnitPrice = productSku.SalePrice,
                        CreatedDate = DateTime.Now
                    };
                    _context.OrdShoppingCartItems.Add(newItem);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "已加入購物車"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"加入購物車失敗: {ex.Message}"
                });
            }
        }

        // POST: api/ord/Cart/validate-coupon
        /// <summary>
        /// 驗證優惠券
        /// </summary>
        [HttpPost("validate-coupon")]
        public IActionResult ValidateCoupon([FromBody] CouponValidateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CouponCode))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "請輸入優惠碼"
                    });
                }

                // 固定優惠碼驗證邏輯
                var couponCode = request.CouponCode.ToUpper();

                // 定義可用的優惠券
                var validCoupons = new Dictionary<string, decimal>
                {
                    { "SAVE100", 100 },
                    { "SAVE200", 200 },
                    { "DISCOUNT50", 50 }
                };

                if (validCoupons.ContainsKey(couponCode))
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            discountAmount = validCoupons[couponCode],
                            discountType = "fixed",
                            message = $"優惠券套用成功！折扣 ${validCoupons[couponCode]}"
                        }
                    });
                }

                // TODO: 進階版 - 從資料庫查詢優惠券
                /*
                var coupon = await _context.MktCoupons
                    .FirstOrDefaultAsync(c => 
                        c.CouponCode == couponCode 
                        && c.IsActive 
                        && c.StartDate <= DateTime.Now 
                        && c.EndDate >= DateTime.Now);

                if (coupon != null)
                {
                    decimal discountAmount = coupon.DiscountType == "Percentage" 
                        ? request.Subtotal * (coupon.DiscountValue / 100) 
                        : coupon.DiscountValue;

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            discountAmount = discountAmount,
                            discountType = coupon.DiscountType,
                            message = $"優惠券套用成功！折扣 ${discountAmount}"
                        }
                    });
                }
                */

                return Ok(new
                {
                    success = false,
                    message = "優惠券無效或已過期"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"驗證優惠券失敗: {ex.Message}"
                });
            }
        }

        // POST: api/ord/Cart/checkout
        /// <summary>
        /// 結帳
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Step 1: 驗證購物車
                if (request.CartItems == null || !request.CartItems.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "購物車是空的"
                    });
                }

                var errorList = new List<CheckoutErrorItem>();
                decimal subtotal = 0;

                // Step 2: 檢查商品與庫存
                foreach (var item in request.CartItems)
                {
                    var sku = await _context.ProdProductSkus
                        .AsNoTracking()
                        .Include(s => s.Product)
                        .FirstOrDefaultAsync(s => s.SkuId == item.SkuId && s.ProductId == item.ProductId);

                    if (sku == null)
                    {
                        errorList.Add(new CheckoutErrorItem
                        {
                            ProductName = item.ProductName ?? "未知商品",
                            OptionName = item.OptionName ?? "無資料",
                            Reason = "商品不存在",
                            CurrentStock = null
                        });
                        continue;
                    }

                    string optionNames = sku.SkuCode ?? "預設規格";
                    string status = "";

                    if (!sku.IsActive)
                        status = "已下架";

                    if (sku.StockQty <= 0 || sku.StockQty < item.Quantity)
                    {
                        if (!string.IsNullOrEmpty(status)) status += "、";
                        status += "庫存不足";
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        errorList.Add(new CheckoutErrorItem
                        {
                            ProductName = sku.Product?.ProductName ?? "未知商品",
                            OptionName = optionNames,
                            Reason = status,
                            CurrentStock = sku.StockQty
                        });
                        continue;
                    }

                    subtotal += item.SalePrice * item.Quantity;
                }

                if (errorList.Any())
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        success = false,
                        message = "以下商品庫存不足或無法結帳",
                        errors = errorList
                    });
                }

                // Step 3: 建立訂單主檔
                string orderNo = await GenerateOrderNoAsync();

                var order = new OrdOrder
                {
                    OrderNo = orderNo,
                    UserNumberId = request.UserNumberId ?? 1056,
                    OrderStatusId = "pending",
                    PaymentStatus = "unpaid",
                    ShippingStatusId = "unshipped",
                    Subtotal = subtotal,
                    DiscountTotal = request.DiscountAmount ?? 0,
                    ShippingFee = 0,
                    PaymentConfigId = 1000,
                    ReceiverName = null,
                    ReceiverPhone = null,
                    ReceiverAddress = null,
                    HasShippingLabel = false,
                    IsVisibleToMember = true,
                    CreatedDate = DateTime.Now
                };

                _context.OrdOrders.Add(order);
                await _context.SaveChangesAsync();

                // Step 4: 建立訂單明細
                foreach (var item in request.CartItems)
                {
                    var orderItem = new OrdOrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        SkuId = item.SkuId,
                        Qty = item.Quantity,
                        UnitPrice = item.SalePrice
                    };
                    _context.OrdOrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                // Step 5: 如果有優惠券，記錄調整
                if (request.DiscountAmount.HasValue && request.DiscountAmount.Value > 0 && !string.IsNullOrEmpty(request.CouponCode))
                {
                    var adjustment = new OrdOrderAdjustment
                    {
                        OrderId = order.OrderId,
                        Kind = "coupon",
                        Scope = "order",
                        Code = request.CouponCode,
                        Method = "fixed",
                        DiscountRate = null,
                        AdjustmentAmount = -request.DiscountAmount.Value,
                        CreatedDate = DateTime.Now,
                        RevisedDate = DateTime.Now
                    };
                    _context.OrdOrderAdjustments.Add(adjustment);
                    await _context.SaveChangesAsync();
                }

                // Step 6: 清空購物車
                if (!string.IsNullOrEmpty(request.SessionId) || request.UserNumberId.HasValue)
                {
                    var cart = await _context.OrdShoppingCarts
                        .Include(c => c.OrdShoppingCartItems)
                        .FirstOrDefaultAsync(c =>
                            (request.UserNumberId.HasValue && c.UserNumberId == request.UserNumberId) ||
                            (!request.UserNumberId.HasValue && c.SessionId == request.SessionId));

                    if (cart != null)
                    {
                        _context.OrdShoppingCartItems.RemoveRange(cart.OrdShoppingCartItems);
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                // 計算最終金額
                decimal totalAmount = subtotal - (request.DiscountAmount ?? 0);

                return Ok(new
                {
                    success = true,
                    message = "訂單建立成功",
                    orderNo = orderNo,
                    orderId = order.OrderId,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    success = false,
                    message = $"結帳失敗: {ex.Message}"
                });
            }
        }

        // 生成訂單編號（yyyyMMdd + 7位流水號）
        private async Task<string> GenerateOrderNoAsync()
        {
            string prefix = DateTime.Now.ToString("yyyyMMdd");
            var lastOrder = await _context.OrdOrders
                .Where(o => o.OrderNo.StartsWith(prefix))
                .OrderByDescending(o => o.OrderNo)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (lastOrder != null && int.TryParse(lastOrder.OrderNo.Substring(8), out int lastNum))
            {
                nextNum = lastNum + 1;
            }

            return $"{prefix}{nextNum:D7}";
        }
    }

    // ===== Request Models =====

    public class AddToCartRequest
    {
        public string SessionId { get; set; }
        public int? UserNumberId { get; set; }
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public int Quantity { get; set; }
    }

    public class CouponValidateRequest
    {
        public string CouponCode { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class CheckoutRequest
    {
        public string SessionId { get; set; }
        public int? UserNumberId { get; set; }
        public List<CartItemRequest> CartItems { get; set; }
        public string CouponCode { get; set; }
        public decimal? DiscountAmount { get; set; }
    }

    public class CartItemRequest
    {
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public string ProductName { get; set; }
        public string OptionName { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
    }

    public class CheckoutErrorItem
    {
        public string ProductName { get; set; }
        public string OptionName { get; set; }
        public string Reason { get; set; }
        public int? CurrentStock { get; set; }
    }
}