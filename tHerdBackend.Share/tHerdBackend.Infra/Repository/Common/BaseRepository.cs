using System.Data;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.Common
{
    public abstract class BaseRepository
    {
        protected readonly ISqlConnectionFactory _factory; // Dapper
        protected readonly tHerdDBContext _db; // EF

        protected BaseRepository(ISqlConnectionFactory factory, tHerdDBContext db)
        {
            _factory = factory;
            _db = db;
        }

        protected async Task<(IDbConnection conn, IDbTransaction? tx, bool needDispose)>
            GetConnAsync(CancellationToken ct = default)
            => await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
    }
}
