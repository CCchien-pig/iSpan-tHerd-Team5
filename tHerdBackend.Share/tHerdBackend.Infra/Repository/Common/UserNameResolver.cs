using tHerdBackend.Core.Interfaces.PROD;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.Common
{
    public class UserNameResolver
    {
        private readonly AspnetusersNameRepository _repo;
        private Dictionary<int, string>? _cache;

        public UserNameResolver(ISqlConnectionFactory factory, tHerdDBContext db)
            => _repo = new AspnetusersNameRepository(factory, db);

        public async Task LoadAsync(CancellationToken ct = default)
        {
            var users = await _repo.GetAllUserNameAsync(ct);
            _cache = users.ToDictionary(u => u.UserNumberId, u => u.FullName);
        }

		// 泛型統一版本，可套用 ProdProductDto 或 ProdProductDetailDto
		public void Apply<T>(T item) where T : IProductUserInfo
		{
			if (_cache == null || item == null) return;

			if (item.Creator != 0 && _cache.TryGetValue(item.Creator, out var c))
				item.CreatorNm = c;

			if (item.Reviser.HasValue && _cache.TryGetValue(item.Reviser.Value, out var r))
				item.ReviserNm = r;
		}

		// 集合版本
		public void Apply<T>(IEnumerable<T> list) where T : IProductUserInfo
		{
			foreach (var item in list)
				Apply(item);
		}
	}
}