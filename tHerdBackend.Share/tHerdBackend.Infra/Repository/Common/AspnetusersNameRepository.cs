using Dapper;
using tHerdBackend.Core.DTOs.Common;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.Common
{
    /// <summary>
    /// 查詢使用者名稱清單
    /// </summary>
    public class AspnetusersNameRepository
    {
        private readonly ISqlConnectionFactory _factory;     // Dapper
        private readonly tHerdDBContext _db;                 // EF

        public AspnetusersNameRepository(ISqlConnectionFactory factory, tHerdDBContext db)
        {
            _factory = factory;
            _db = db;
        }

        /// <summary>
        /// 查詢使用者名稱清單
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<UserNameInfoDto>> GetAllUserNameAsync(CancellationToken ct = default)
        {
            string sql = @"select UserNumberId, LastName, FirstName from aspnetusers;";

            var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory, ct);
            try
            {
                var cmd = new CommandDefinition(sql, transaction: tx, cancellationToken: ct);
                return await conn.QueryAsync<UserNameInfoDto>(cmd);
            }
            finally
            {
                if (needDispose) conn.Dispose();
            }
        }
    }
}
