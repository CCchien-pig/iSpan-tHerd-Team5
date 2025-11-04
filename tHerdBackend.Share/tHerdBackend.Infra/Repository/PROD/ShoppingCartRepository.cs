using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.PROD.ord;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Core.Models;
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
                // 1️ 取得使用者資訊
                int? userNumberId = null;

                if (_userMgr != null)
                {
                    var user = await _userMgr.Users.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == _currentUser.Id, ct);
                    userNumberId = user?.UserNumberId;
                }
                else
                {
                    // 若無 Identity，改以 JWT Id 或匿名 0
                    userNumberId = int.TryParse(_currentUser.Id, out var parsed) ? parsed : 0;
                }

                // 若無 SessionId，建立新的 Guid
                var sessionId = Guid.NewGuid().ToString(); // 暫時強制生成新的 SessionId

                // 2️ 嘗試取得現有購物車
                var cartId = await conn.ExecuteScalarAsync<int?>(
                    @"SELECT CartId FROM ORD_ShoppingCart 
                      WHERE (UserNumberId = @UserNumberId OR SessionId = @SessionId)
                      ORDER BY CreatedDate DESC",
                    new { UserNumberId = userNumberId, SessionId = sessionId }, tran);

                // 3️ 若沒有購物車則建立
                if (cartId == null)
                {
                    cartId = await conn.ExecuteScalarAsync<int>(
                        @"INSERT INTO ORD_ShoppingCart (UserNumberId, SessionId, MaxItemsAllowed, CreatedDate)
                          OUTPUT INSERTED.CartId
                          VALUES (@UserNumberId, @SessionId, 20, SYSDATETIME())",
                        new { UserNumberId = userNumberId, SessionId = sessionId }, tran);
                }

                // 4️ 檢查是否已有相同 SKU
                var existItemId = await conn.ExecuteScalarAsync<int?>(
                    @"SELECT CartItemId FROM ORD_ShoppingCartItem 
                      WHERE CartId = @CartId AND SkuId = @SkuId",
                    new { CartId = cartId, SkuId = dto.SkuId }, tran);

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
                    // 新增新項目
                    await conn.ExecuteAsync(
                        @"INSERT INTO ORD_ShoppingCartItem (CartId, ProductId, SkuId, Qty, UnitPrice, CreatedDate)
                          VALUES (@CartId, @ProductId, @SkuId, @Qty, @UnitPrice, SYSDATETIME())",
                        new
                        {
                            CartId = cartId,
                            dto.ProductId,
                            dto.SkuId,
                            dto.Qty,
                            dto.UnitPrice
                        }, tran);
                }

                // 5️ 更新購物車修改時間
                await conn.ExecuteAsync(
                    @"UPDATE ORD_ShoppingCart 
                      SET RevisedDate = SYSDATETIME()
                      WHERE CartId = @CartId",
                    new { CartId = cartId }, tran);

                tran.Commit();
                return cartId.Value;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }
    }
}
