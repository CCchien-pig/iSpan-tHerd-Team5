using Dapper;
using System.Data;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.PROD.Services
{
    /// <summary>
    /// 負責 SKU / 規格 / 分類維護
    /// </summary>
    public class ProductRelationService
    {
        public async Task UpsertRelationsAsync(IDbConnection conn, IDbTransaction tran, ProdProductDetailDto dto, tHerdDBContext db)
        {
            // 抽出 UpsertRelationsAsync() 的全部內容（SKU + 規格 + 分類）
            // 拆分成小方法：
            await UpsertSkusAsync(conn, tran, dto, db);
            await UpsertSpecificationsAsync(conn, tran, dto);
            await UpsertTypesAsync(conn, tran, dto);
        }

        private async Task UpsertSkusAsync(IDbConnection conn, IDbTransaction tran, ProdProductDetailDto dto, tHerdDBContext db)
        {
            // 原本的 SKU 建立 + 更新邏輯
        }

        private async Task UpsertSpecificationsAsync(IDbConnection conn, IDbTransaction tran, ProdProductDetailDto dto)
        {
            // 原本的 SpecConfigs / Options / SkuValue 建立邏輯
        }

        private async Task UpsertTypesAsync(IDbConnection conn, IDbTransaction tran, ProdProductDetailDto dto)
        {
            await conn.ExecuteAsync("DELETE FROM PROD_ProductType WHERE ProductId=@ProductId", new { dto.ProductId }, tran);
            foreach (var type in dto.Types ?? new())
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO PROD_ProductType (ProductTypeId, ProductId, IsPrimary)
                  VALUES (@ProductTypeId, @ProductId, @IsPrimary)",
                    new { type.ProductTypeId, dto.ProductId, type.IsPrimary }, tran);
            }
        }
    }
}
