using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Core.DTOs.PROD;
using tHerdBackend.Core.Models;

namespace tHerdBackend.Core.Interfaces.Products
{
    public interface IProdProductRepository
    {
        Task<(IEnumerable<ProdProductDto> list, int totalCount)> GetAllAsync(ProductFilterQueryDto query, CancellationToken ct = default);
        Task<ProdProductDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<int> AddAsync(ProdProductDetailDto product, CancellationToken ct = default);
        Task<bool> UpdateAsync(ProdProductDetailDto product, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
        Task<PagedResult<ProdProductDto>> QueryAsync(ProductQuery query, CancellationToken ct = default);

        Task<IEnumerable<LoadBrandOptionDto>> LoadBrandOptionsAsync(CancellationToken ct = default);
        Task<List<string>> GetDuplicateBarcodesAsync(IEnumerable<string> barcodes, IEnumerable<int> excludeSkuIds);

		Task<List<ProdProductTypeConfigDto>> GetAllProductTypesAsync(CancellationToken ct = default);
		Task<bool> GetByProductNameAsync(string name, int id, CancellationToken ct = default); // 檢查產品名稱是否重複
                                                                                               //Task<string> CheckUniqulByBarcodeAsync(List<string> barcodes, CancellationToken ct = default); // 檢查條碼是否重複
    }

	// 簡化的查詢模型與分頁結果
	public sealed class ProductQuery
    {
        public string? Keyword { get; set; }
        public bool? IsPublished { get; set; }
        public int? BrandId { get; set; }
        public int? SupplierId { get; set; }
        public int Page { get; set; } = 1;         // 1-based
        public int PageIndex { get; set; } = 1;     // 第幾頁（1-based）
        public int PageSize { get; set; } = 10;    // 每頁筆數（預設 10，自己調整）
        public string? OrderBy { get; set; }       // e.g. "CreatedDate desc"
    }
}
