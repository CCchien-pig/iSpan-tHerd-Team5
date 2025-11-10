using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using tHerdBackend.Core.Abstractions;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.PROD.sup;
using tHerdBackend.Core.DTOs.USER;
using tHerdBackend.Core.Interfaces.Products;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;
using tHerdBackend.Infra.Repository.Common;

namespace tHerdBackend.Infra.Repository.PROD
{
    /// <summary>
    /// 多維查詢（品牌 + 屬性 + 屬性選項）
    /// </summary>
    public class ProdProductFilterRepository : BaseRepository, IProdProductFilterRepository
    {
        public ProdProductFilterRepository(
            ISqlConnectionFactory factory,
            tHerdDBContext db,
            ICurrentUser currentUser,
            UserManager<ApplicationUser>? userMgr = null,
            SignInManager<ApplicationUser>? signInMgr = null)
            : base(factory, db)
        {
        }

        /// <summary>
        /// 取得所有啟用品牌清單（前台使用）
        /// </summary>
        public async Task<List<SupBrandsDto>> GetBrandsAll()
        {
            return await _db.SupBrands
                .Where(b => b.IsActive == true)
                .OrderBy(b => b.BrandName)
                .Select(b => new SupBrandsDto
                {
                    BrandId = b.BrandId,
                    BrandName = b.BrandName,
                    BrandCode = b.BrandCode
                })
                .ToListAsync();
        }

        /// <summary>
        /// 搜尋品牌名稱（關鍵字）
        /// </summary>
        public async Task<List<SupBrandsDto>> SearchBrands(string keyword)
        {
            keyword = keyword?.Trim().ToLower() ?? string.Empty;

            return await _db.SupBrands
                .Where(b =>
                    b.IsActive == true &&
                    (string.IsNullOrEmpty(keyword) ||
                     b.BrandName.ToLower().StartsWith(keyword))
                )
                .OrderBy(b => b.BrandName)
                .Select(b => new SupBrandsDto
                {
                    BrandId = b.BrandId,
                    BrandName = b.BrandName,
                    BrandCode = b.BrandCode
                })
                .ToListAsync();
        }

        /// <summary>
        /// 取得所有屬性及其選項（DataType 為 select 或 check）
        /// 用於前台商品篩選 Sidebar
        /// </summary>
        public async Task<List<AttributeWithOptionsDto>> GetFilterAttributesAsync(CancellationToken ct = default)
        {
            const string sql = @"
        SELECT 
            a.AttributeId, a.AttributeName, a.DataType,
            o.AttributeOptionId, o.OptionName, o.OptionValue, o.OrderSeq
        FROM PROD_Attribute a
        LEFT JOIN PROD_AttributeOption o ON a.AttributeId = o.AttributeId
        WHERE a.DataType IN ('select', 'check')
        ORDER BY a.AttributeId, o.OrderSeq;
    ";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);

            try
            {
                var lookup = new Dictionary<int, AttributeWithOptionsDto>();

                await conn.QueryAsync<AttributeWithOptionsDto, AttributeOptionDto, AttributeWithOptionsDto>(
                    sql,
                    (attr, opt) =>
                    {
                        if (!lookup.TryGetValue(attr.AttributeId, out var entry))
                        {
                            entry = attr;
                            entry.Options = new List<AttributeOptionDto>();
                            lookup.Add(attr.AttributeId, entry);
                        }

                        if (opt != null)
                            entry.Options.Add(opt);

                        return entry;
                    },
                    transaction: tx,
                    splitOn: "AttributeOptionId"
                );

                return lookup.Values.ToList();
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }
    }
}
