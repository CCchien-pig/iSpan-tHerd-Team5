using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.DTOs.SYS;
using tHerdBackend.Core.Models;

namespace tHerdBackend.Core.Interfaces.PROD
{
    public interface IProductService
    {
        Task<(IEnumerable<ProdProductDto> list, int totalCount)> GetAllAsync(ProductFilterQueryDto query, CancellationToken ct = default);
        Task<ProdProductDetailDto?> GetByIdAsync(int productId);
        Task<int> CreateAsync(ProdProductDetailDto dto);
        Task UpdateAsync(ProdProductDetailDto dto);
        Task DeleteAsync(int productId);

        Task<IEnumerable<LoadBrandOptionDto>> LoadBrandOptionsAsync();
        Task<IEnumerable<SysCodeDto>> GetSysCodes(string mod, List<string> ids);
        Task<(bool IsValid, string ErrorMessage)> ValidateProductAsync(ProdProductDetailDto dto);

		Task<List<ProdProductTypeConfigDto>> GetAllProductTypesAsync(CancellationToken ct = default);
	}
}
