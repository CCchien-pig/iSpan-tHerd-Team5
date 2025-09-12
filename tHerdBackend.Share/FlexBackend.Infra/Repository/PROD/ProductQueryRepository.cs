using FlexBackend.Infra.DBSetting;
using FlexBackend.Infra.Models;

namespace FlexBackend.Infra.Repository.PROD
{
	public class ProductQueryRepository
	{
		private readonly ISqlConnectionFactory _factory;     // 給「純查詢」或「無交易時」使用
		public ProductQueryRepository(ISqlConnectionFactory factory, tHerdDBContext db)
		{
			_factory = factory;
		}

		public async Task<ProductDetailDto?> GetProductDetailAsync(int productId)
		{
			string sql = @"
                SELECT p.ProductId, p.ProductName, p.Price, s.SpecId, s.SpecName, s.SpecValue, i.ImageUrl, st.StockQty
                FROM PROD_Product p
                LEFT JOIN PROD_Specification s ON p.ProductId = s.ProductId
                LEFT JOIN PROD_Image i ON p.ProductId = i.ProductId
                LEFT JOIN PROD_Stock st ON p.ProductId = st.ProductId
                WHERE p.ProductId = @ProductId;
            ";

			var lookup = new Dictionary<int, ProductDetailDto>();

			var result = await _db.QueryAsync<ProductDetailDto, SpecificationDto, string, int, ProductDetailDto>(
				sql,
				(prod, spec, img, stock) =>
				{
					if (!lookup.TryGetValue(prod.ProductId, out var dto))
					{
						dto = prod;
						dto.Specifications = new List<SpecificationDto>();
						dto.Images = new List<string>();
						lookup.Add(dto.ProductId, dto);
					}

					if (spec != null && !dto.Specifications.Any(x => x.SpecId == spec.SpecId))
						dto.Specifications.Add(spec);

					if (!string.IsNullOrEmpty(img) && !dto.Images.Contains(img))
						dto.Images.Add(img);

					dto.StockQty = stock;
					return dto;
				},
				new { ProductId = productId },
				splitOn: "SpecId,ImageUrl,StockQty"
			);

			return lookup.Values.FirstOrDefault();
		}
	}
}
