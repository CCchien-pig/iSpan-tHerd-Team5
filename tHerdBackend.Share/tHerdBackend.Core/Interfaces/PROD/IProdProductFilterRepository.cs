using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.PROD.sup;

namespace tHerdBackend.Core.Interfaces.Products
{
    public interface IProdProductFilterRepository
    {
        Task<List<SupBrandsDto>> GetBrandsAll();

        Task<List<SupBrandsDto>> SearchBrands(string keyword);

        Task<List<AttributeWithOptionsDto>> GetFilterAttributesAsync(CancellationToken ct = default);
    }
}
