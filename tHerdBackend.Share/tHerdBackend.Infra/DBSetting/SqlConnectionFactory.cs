using Microsoft.Data.SqlClient;
using System.Data;

namespace tHerdBackend.Infra.DBSetting
{
    public sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connStr;
        public SqlConnectionFactory(string connStr) => _connStr = connStr;

        public IDbConnection Create() => new SqlConnection(_connStr);
    }
}
