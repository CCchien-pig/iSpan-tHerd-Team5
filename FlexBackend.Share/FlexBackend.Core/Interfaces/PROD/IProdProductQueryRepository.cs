using FlexBackend.Core.DTOs.PROD;

namespace FlexBackend.Core.Interfaces.Products
{
    public interface IProdProductQueryRepository
    {
        Task<IEnumerable<ProdProductQueryDto>> GetAllProductQueryListAsync(int ProductId, CancellationToken ct = default);
    }

    // 簡化的查詢模型與分頁結果
    public sealed class ProductQueryParam
    {
    }
}
