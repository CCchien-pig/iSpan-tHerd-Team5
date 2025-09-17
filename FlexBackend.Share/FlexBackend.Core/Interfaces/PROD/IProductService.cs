using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.DTOs.SYS;

namespace FlexBackend.Core.Interfaces.PROD
{
    public interface IProductService
    {
        Task<IEnumerable<ProdProductDto>> GetAllAsync();
        Task<ProdProductDto?> GetByIdAsync(int productId);
        Task<int> CreateAsync(ProdProductDto dto);
        Task UpdateAsync(ProdProductDto dto);
        Task DeleteAsync(int productId);

        Task<IEnumerable<LoadBrandOptionDto>> LoadBrandOptionsAsync();
        Task<IEnumerable<SysCodeDto>> GetSysCodes(string mod, List<string> ids);
        Task<(bool IsValid, string ErrorMessage)> ValidateProductAsync(ProdProductDto dto);

	}
}
