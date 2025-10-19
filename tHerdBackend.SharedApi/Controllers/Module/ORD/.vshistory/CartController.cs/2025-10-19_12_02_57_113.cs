using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.Models;

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
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new
                        {
                            items = new List<object>(),
                            totalQty = 0,
                            subtotal = 0
                        },
                        Message = "購物車為空"
                    });
                }

                // 取得購物車中的 SKU 和商品 ID
                var skuIds = cart.OrdShoppingCartItems.Select(i => i.SkuId).ToList();
                var productIds = cart.OrdShoppingCartItems.Select(i => i.ProductId).ToList();

                // 查詢商品詳細資訊（包含圖片）- 根據您的 SQL 結構
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
                        unitPrice = item.UnitPrice,
                        quantity = item.Qty,
                        subtotal = item.UnitPrice * item.Qty,
                        imageUrl = detail?.FileUrl ?? "",
                        isPublished = detail?.IsPublished ?? false
                    };
                }).ToList();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        items = items,
                        totalQty = items.Sum(i => i.quantity),
                        subtotal = items.Sum(i => i.subtotal)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"取得購物車失敗: {ex.Message}"
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
                // 1. 查找或建立購物車
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

                // 2. 檢查購物車上限
                if (cart.OrdShoppingCartItems.Count >= cart.MaxItemsAllowed)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = $"購物車已達上限 ({cart.MaxItemsAllowed} 項)"
                    });
                }

                // 3. 驗證商品和 SKU 是否存在且有效
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
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "商品不存在或已下架"
                    });
                }

                // 4. 查找是否已存在相同商品
                var existingItem = cart.OrdShoppingCartItems
                    .FirstOrDefault(i => i.ProductId == request.ProductId && i.SkuId == request.SkuId);

                if (existingItem != null)
                {
                    // 已存在則增加數量
                    existingItem.Qty += request.Quantity;
                    _context.OrdShoppingCartItems.Update(existingItem);
                }
                else
                {
                    // 新增購物車項目
                    var cartItem = new OrdShoppingCartItem
                    {
                        CartId = cart.CartId,
                        ProductId = request.ProductId,
                        SkuId = request.SkuId,
                        Qty = request.Quantity,
                        UnitPrice = productSku.SalePrice,  // 使用售價
                        CreatedDate = DateTime.Now
                    };

                    _context.OrdShoppingCartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "已加入購物車"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"加入購物車失敗: {ex.Message}"
                });
            }
        }

        // PUT: api/ord/Cart/update/{cartItemId}
        /// <summary>
        /// 更新購物車商品數量
        /// </summary>
        [HttpPut("update/{cartItemId}")]
        public async Task<IActionResult> UpdateCartItem(
            int cartItemId,
            [FromBody] UpdateCartRequest request)
        {
            try
            {
                var cartItem = await _context.OrdShoppingCartItems.FindAsync(cartItemId);

                if (cartItem == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "購物車項目不存在"
                    });
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "數量必須大於 0"
                    });
                }

                cartItem.Qty = request.Quantity;
                _context.OrdShoppingCartItems.Update(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "更新成功"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"更新失敗: {ex.Message}"
                });
            }
        }

        // DELETE: api/ord/Cart/remove/{cartItemId}
        /// <summary>
        /// 移除購物車商品
        /// </summary>
        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> RemoveCartItem(int cartItemId)
        {
            try
            {
                var cartItem = await _context.OrdShoppingCartItems.FindAsync(cartItemId);

                if (cartItem == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "購物車項目不存在"
                    });
                }

                _context.OrdShoppingCartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "已移除商品"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"移除失敗: {ex.Message}"
                });
            }
        }

        // DELETE: api/ord/Cart/clear
        /// <summary>
        /// 清空購物車
        /// </summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart(
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

                if (cart != null && cart.OrdShoppingCartItems.Any())
                {
                    _context.OrdShoppingCartItems.RemoveRange(cart.OrdShoppingCartItems);
                    await _context.SaveChangesAsync();
                }

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "購物車已清空"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"清空失敗: {ex.Message}"
                });
            }
        }
    }

    #region DTO Models

    public class AddToCartRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public int? UserNumberId { get; set; }
        public int ProductId { get; set; }
        public int SkuId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartRequest
    {
        public int Quantity { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}