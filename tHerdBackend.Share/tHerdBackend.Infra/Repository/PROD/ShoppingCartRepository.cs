using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.PROD.ord;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.ValueObjects;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Repository.Common;

namespace tHerdBackend.Infra.Repository.PROD
{
    public class ShoppingCartRepository : BaseRepository, IShoppingCartRepository
    {
        public ShoppingCartRepository(
            ISqlConnectionFactory factory,
            tHerdDBContext db)
            : base(factory, db)
        {
        }

        /// <summary>
        /// 加入購物車（以 Sku 為主）
        /// </summary>
        public async Task<int> AddShoppingCartAsync(AddToCartDto dto, CancellationToken ct = default)
        {
            var (conn, tran, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

            try
            {
                var now = DateTime.Now;

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
                  VALUES (@UserNumberId, @SessionId, 20, @now)",
                        new { dto.UserNumberId, dto.SessionId, now }, tran);
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
                      CreatedDate = @now
                  WHERE CartItemId = @CartItemId",
                        new
                        {
                            CartItemId = existItemId,
                            dto.Qty,
                            dto.UnitPrice,
							now
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
                  VALUES (@CartId, @ProductId, @SkuId, @Qty, @UnitPrice, @now)",
                        new
                        {
                            CartId = cartId,
                            ProductId = productId,
                            dto.SkuId,
                            dto.Qty,
                            dto.UnitPrice,
                            now
                        }, tran);
                }

                // 更新購物車修改時間
                await conn.ExecuteAsync(
					@"UPDATE ORD_ShoppingCart 
              SET RevisedDate = @now
              WHERE CartId = @CartId",
                    new { CartId = cartId, now }, tran);

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

        /// <summary>
        /// 取得購物車摘要（商品數量 / 總數量 / 小計）
        /// </summary>
        public async Task<dynamic?> GetCartSummaryAsync(int? userNumberId, string? sessionId, CancellationToken ct = default)
        {
            var (conn, tran, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

            try
            {
                var data = await conn.QueryFirstOrDefaultAsync(
                    @"SELECT 
                          COUNT(DISTINCT sci.CartItemId) AS ItemCount,
                          ISNULL(SUM(sci.Qty), 0) AS TotalQty,
                          ISNULL(SUM(sci.Qty * sci.UnitPrice), 0) AS Subtotal
                      FROM ORD_ShoppingCart sc
                      JOIN ORD_ShoppingCartItem sci ON sc.CartId = sci.CartId
                      WHERE (sc.UserNumberId = @UserNumberId OR sc.SessionId = @SessionId);",
                    new { UserNumberId = userNumberId, SessionId = sessionId });

                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"取得購物車摘要時發生錯誤：{ex.Message}", ex);
            }
            finally
            {
                if (needDispose)
                    conn.Dispose();
            }
        }
    }
}
