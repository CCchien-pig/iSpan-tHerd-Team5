using Dapper;
using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Core.Interfaces.Products;
using FlexBackend.Infra.DBSetting;

namespace FlexBackend.Infra.Repository.PROD
{
    public class ProdProductRepository : IProdProductRepository
    {
        private readonly ISqlConnectionFactory _factory;
        public ProdProductRepository(ISqlConnectionFactory factory)
            => _factory = factory;

        public Task<int> AddAsync(ProdProductDto product, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ProdProductDto>> GetAllAsync(CancellationToken ct = default)
        {
            using var db = _factory.Create();

            string sql = @"SELECT ProductId, 
                      BrandId, SeoId, ProductName, ShortDesc,
                      FullDesc, IsPublished, Weight,
                      VolumeCubicMeter, VolumeUnit,
                      Creator, CreatedDate, Reviser,
                      RevisedDate
                    FROM PROD_Product;";

            return db.Query<ProdProductDto>(sql);
        }

        public Task<ProdProductDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<ProdProductDto>> QueryAsync(ProductQuery query, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(ProdProductDto product, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
