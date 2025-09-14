using Dapper;
using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Infra.DBSetting;
using FlexBackend.Infra.Helpers;
using FlexBackend.Infra.Models;
using ISpan.eMiniHR.DataAccess.Helpers;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.Infra.Repository.PROD
{
    public class ProdProductRepository : IProdProductRepository
    {
        private readonly ISqlConnectionFactory _factory;     // 給「純查詢」或「無交易時」使用
        private readonly tHerdDBContext _db;                 // 寫入與交易來源

        public ProdProductRepository(ISqlConnectionFactory factory, tHerdDBContext db)
        {
            _factory = factory;
            _db = db;
        }

        public async Task<IEnumerable<ProdProductDto>> GetAllAsync(CancellationToken ct = default)
        {
            string sql = @"SELECT p.ProductId, p.ProductName, su.SupplierId, su.SupplierName,
                p.BrandId, s.BrandName, p.SeoId, p.ProductCode,
                p.ShortDesc, p.FullDesc, p.IsPublished,
                p.Weight, p.VolumeCubicMeter, p.VolumeUnit, p.Creator,
                p.CreatedDate, p.Reviser, p.RevisedDate, tc.ProductTypeName
                FROM PROD_Product p
                JOIN SUP_Brand s ON s.BrandId=p.BrandId 
                LEFT JOIN SUP_Supplier su ON su.SupplierId=s.SupplierId
                LEFT JOIN PROD_ProductType t ON t.ProductId=p.ProductId
                LEFT JOIN PROD_ProductTypeConfig tc ON tc.ProductTypeId=t.ProductTypeId;";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var cmd = new CommandDefinition(sql, transaction: tx, cancellationToken: ct);
                var repo = new AspnetusersNameRepository(_factory, _db);
                var emp = await repo.GetAllUserNameAsync(ct);
                // 原始查詢結果 (會有重複 ProductId)
                var raw = conn.Query<ProdProductDto, string, ProdProductDto>(
                    sql,
                    (p, typeName) =>
                    {
                        p.ProductTypeDesc = new List<string>();
                        if (!string.IsNullOrEmpty(typeName))
                            p.ProductTypeDesc.Add(typeName);
                        return p;
                    },
                    splitOn: "ProductTypeName",
                    transaction: tx);

                // GroupBy → 合併 ProductTypeNames
                var list = raw
                    .GroupBy(p => p.ProductId)
                    .Select(g =>
                    {
                        var first = g.First();
                        first.ProductTypeDesc = g.SelectMany(x => x.ProductTypeDesc).Distinct().ToList();
                        return first;
                    })
                    .ToList();

                foreach (var item in list) {
                    item.CreatorNm = emp.FirstOrDefault(e => e.UserNumberId == item.Creator)?.FullName;
                    item.ReviserNm = emp.FirstOrDefault(e => e.UserNumberId == item.Reviser)?.FullName;
                }
                return list;
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }

		public async Task<ProdProductDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT ProductId, BrandId, SeoId, ProductName, ShortDesc, FullDesc, IsPublished,
                       Weight, VolumeCubicMeter, VolumeUnit, Creator, CreatedDate, Reviser, RevisedDate
                FROM   PROD_Product WITH (NOLOCK)
                WHERE  ProductId = @Id;";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var cmd = new CommandDefinition(sql, new { Id = id }, tx, cancellationToken: ct);
                return await conn.QueryFirstOrDefaultAsync<ProdProductDto>(cmd);
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }

        public async Task<int> AddAsync(ProdProductDto dto, CancellationToken ct = default)
        {
            var entity = dto.Adapt<ProdProduct>(MapsterConfig.Default);

            // 部分更新（PATCH：忽略 null）
            dto.Adapt(entity, MapsterConfig.Patch);
            await _db.SaveChangesAsync(ct);

            return entity.ProductId;            // 不用 await
        }

        public async Task<PagedResult<ProdProductDto>> QueryAsync(ProductQuery query, CancellationToken ct = default)
        {
            // 例：分頁 + 條件（查詢仍走 Dapper）
            var sb = new System.Text.StringBuilder(@"
                    SELECT ProductId, BrandId, SeoId, ProductName, ShortDesc, FullDesc, IsPublished,
                           Weight, VolumeCubicMeter, VolumeUnit, Creator, CreatedDate, Reviser, RevisedDate
                    FROM   PROD_Product WITH (NOLOCK)
                    WHERE  1 = 1 ");

            var param = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                sb.Append(" AND ProductName LIKE @kw ");
                param.Add("kw", $"%{query.Keyword}%");
            }
            if (query.IsPublished.HasValue)
            {
                sb.Append(" AND IsPublished = @pub ");
                param.Add("pub", query.IsPublished);
            }

            sb.Append(" ORDER BY ProductId DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY; ");
            param.Add("skip", (query.PageIndex - 1) * query.PageSize);
            param.Add("take", query.PageSize);

            const string sqlCount = @"SELECT COUNT(1) FROM PROD_Product WHERE 1=1
                                      /* 同條件…這裡可重用組裝或寫成 SP */";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var list = await conn.QueryAsync<ProdProductDto>(
                    new CommandDefinition(sb.ToString(), param, tx, cancellationToken: ct));

                // 這裡示範：為了簡潔，直接再跑一次 Count（正式可抽共用）
                var total = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(sqlCount, param, tx, cancellationToken: ct));

                return new PagedResult<ProdProductDto>
                {
                    Items = list.ToList(),
                    TotalCount = total,
                    Page = query.PageIndex,
                    PageSize = query.PageSize
                };
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }

        public async Task<bool> UpdateAsync(ProdProductDto dto, CancellationToken ct = default)
        {
            var entity = await _db.ProdProducts.FirstOrDefaultAsync(x => x.ProductId == dto.ProductId, ct);
            if (entity == null) return false;

            // 映射更新欄位
            entity.BrandId = dto.BrandId;
            entity.SeoId = dto.SeoId;
            entity.ProductName = dto.ProductName;
            entity.ShortDesc = dto.ShortDesc;
            entity.FullDesc = dto.FullDesc;
            entity.IsPublished = dto.IsPublished;
            entity.Weight = dto.Weight;
            entity.VolumeCubicMeter = dto.VolumeCubicMeter;
            entity.VolumeUnit = dto.VolumeUnit;
            //entity.Reviser = dto.Reviser;
            entity.RevisedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = _db.ProdProducts.Find(new object?[] { id }, ct);
            if (entity == null) return false;

            _db.ProdProducts.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
