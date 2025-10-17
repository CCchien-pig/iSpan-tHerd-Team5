using Microsoft.EntityFrameworkCore;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.PROD.Assemblers
{
    /// <summary>
    /// 商品規格組裝
    /// </summary>
    public static class ProductSpecAssembler
    {
        public static async Task<List<ProdSpecificationConfigDto>> BuildSpecConfigsAsync(
            tHerdDBContext db, List<ProdProductSkuDto> skus)
        {
            var specOptionIds = skus
                .SelectMany(s => s.SpecValues.Select(v => v.SpecificationOptionId))
                .Distinct()
                .ToList();

            if (!specOptionIds.Any()) return new();

            var options = await db.ProdSpecificationOptions
                .Where(o => specOptionIds.Contains(o.SpecificationOptionId))
                .ToListAsync();

            var configs = await db.ProdSpecificationConfigs
                .Where(c => options.Select(o => o.SpecificationConfigId).Distinct().Contains(c.SpecificationConfigId))
                .ToListAsync();

            return configs.Select(c => new ProdSpecificationConfigDto
            {
                SpecificationConfigId = c.SpecificationConfigId,
                GroupName = c.GroupName,
                OrderSeq = c.OrderSeq,
                SpecOptions = options
                    .Where(o => o.SpecificationConfigId == c.SpecificationConfigId)
                    .Select(o => new ProdSpecificationOptionDto
                    {
                        SpecificationOptionId = o.SpecificationOptionId,
                        SpecificationConfigId = o.SpecificationConfigId,
                        OptionName = o.OptionName,
                        OrderSeq = o.OrderSeq,
                        SkuId = skus.FirstOrDefault(s => s.SpecValues.Any(v => v.SpecificationOptionId == o.SpecificationOptionId))?.SkuId ?? 0
                    }).ToList()
            }).ToList();
        }
    }
}
