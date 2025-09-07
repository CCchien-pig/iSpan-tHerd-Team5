using FlexBackend.Core.DTOs.PROD;

namespace FlexBackend.Core.Interfaces.Products
{
    public interface IProdProductRepository
    {
        Task<IEnumerable<ProdProductDto>> GetAllAsync(CancellationToken ct = default);
        Task<ProdProductDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<int> AddAsync(ProdProductDto product, CancellationToken ct = default);
        Task<bool> UpdateAsync(ProdProductDto product, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
        Task<PagedResult<ProdProductDto>> QueryAsync(ProductQuery query, CancellationToken ct = default);
    }

    // 簡化的查詢模型與分頁結果
    public sealed class ProductQuery
    {
        public string? Keyword { get; set; }
        public bool? IsPublished { get; set; }
        public int? BrandId { get; set; }
        public int? SupplierId { get; set; }
        public int Page { get; set; } = 1;         // 1-based
        public int PageSize { get; set; } = 20;
        public string? OrderBy { get; set; }       // e.g. "CreatedDate desc"
    }

    public sealed class PagedResult<T>
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public IEnumerable<T> Items { get; init; } = [];
    }
}
