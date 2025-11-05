using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.PROD.ord;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Repository.Common;
using tHerdBackend.Infra.Repository.PROD.Services;

namespace tHerdBackend.Infra.Repository.PROD
{
    public class ShoppingCartRepository : BaseRepository, IShoppingCartRepository
    {
        private readonly ICurrentUser _currentUser;          // 目前登入使用者
        private readonly UserManager<ApplicationUser>? _userMgr;    // Identity UserManager
        private readonly SignInManager<ApplicationUser>? _signInMgr;// Identity SignInManager
        private readonly ProductRelationService _relationSvc; // 商品關聯服務

        public ShoppingCartRepository(
            ISqlConnectionFactory factory,
            tHerdDBContext db,
            ICurrentUser currentUser,
            UserManager<ApplicationUser>? userMgr = null,
            SignInManager<ApplicationUser>? signInMgr = null)
            : base(factory, db)
        {
            _currentUser = currentUser;
            _userMgr = userMgr;
            _signInMgr = signInMgr;
            _relationSvc = new ProductRelationService();
        }

        /// <summary>
        /// 加入購物車（以 Sku 為主）
        /// </summary>
        public async Task<int> AddShoppingCartAsync(AddToCartDto dto, CancellationToken ct = default)
        {
            var (conn, tran, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

            try
            {
                // 若無 SessionId，建立新的 Guid
                // var sessionId = Guid.NewGuid().ToString(); // 佔不用

                // 嘗試取得現有購物車
                var cartId = await conn.ExecuteScalarAsync<int?>(
                    @"SELECT CartId FROM ORD_ShoppingCart 
              WHERE (UserNumberId = @UserNumberId OR SessionId = @SessionId)
              ORDER BY CreatedDate DESC",
                    new { dto.UserNumberId, dto.SessionId }, tran);

                // 若沒有購物車則建立
                if (cartId == null)
                {
                    cartId = await conn.ExecuteScalarAsync<int>(
                        @"INSERT INTO ORD_ShoppingCart (UserNumberId, SessionId, MaxItemsAllowed, CreatedDate)
                  OUTPUT INSERTED.CartId
                  VALUES (@UserNumberId, @SessionId, 20, SYSDATETIME())",
                        new { dto.UserNumberId, dto.SessionId }, tran);
                }

                // 檢查是否已有相同 SKU
                var existItemId = await conn.ExecuteScalarAsync<int?>(
                    @"SELECT CartItemId FROM ORD_ShoppingCartItem 
              WHERE CartId = @CartId AND SkuId = @SkuId",
                    new { CartId = cartId, dto.SkuId }, tran);

                if (existItemId != null)
                {
                    // 更新數量
                    await conn.ExecuteAsync(
                        @"UPDATE ORD_ShoppingCartItem
                  SET Qty = Qty + @Qty,
                      UnitPrice = @UnitPrice,
                      CreatedDate = SYSDATETIME()
                  WHERE CartItemId = @CartItemId",
                        new
                        {
                            CartItemId = existItemId,
                            dto.Qty,
                            dto.UnitPrice
                        }, tran);
                }
                else
                {
                    //  用 Dapper 查出 ProductId（取代 _db）
                    var productId = await conn.ExecuteScalarAsync<int>(
                        @"SELECT TOP 1 ProductId 
                  FROM PROD_ProductSku 
                  WHERE SkuId = @SkuId",
                        new { dto.SkuId }, tran);

                    if (productId <= 0)
                        throw new Exception($"找不到對應的 ProductId (SkuId: {dto.SkuId})");

                    // 新增新項目
                    await conn.ExecuteAsync(
                        @"INSERT INTO ORD_ShoppingCartItem (CartId, ProductId, SkuId, Qty, UnitPrice, CreatedDate)
                  VALUES (@CartId, @ProductId, @SkuId, @Qty, @UnitPrice, SYSDATETIME())",
                        new
                        {
                            CartId = cartId,
                            ProductId = productId,
                            dto.SkuId,
                            dto.Qty,
                            dto.UnitPrice
                        }, tran);
                }

                // 更新購物車修改時間
                await conn.ExecuteAsync(
                    @"UPDATE ORD_ShoppingCart 
              SET RevisedDate = SYSDATETIME()
              WHERE CartId = @CartId",
                    new { CartId = cartId }, tran);

                if (tran != null)
                    tran.Commit();
                return cartId.Value;
            }
            catch (Exception ex)
            {
                if (tran != null)
                    tran.Rollback();
                throw new Exception($"加入購物車時發生錯誤：{ex.Message}", ex);
            }
            finally
            {
                if (needDispose && conn != null)
                    conn.Dispose();
            }
        }
    }
}
