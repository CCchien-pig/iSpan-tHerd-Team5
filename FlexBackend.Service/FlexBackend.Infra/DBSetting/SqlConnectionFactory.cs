using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexBackend.Infra.DBSetting
{
    public sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connStr;
        public SqlConnectionFactory(string connStr) => _connStr = connStr;

        public IDbConnection Create() => new SqlConnection(_connStr);
    }
}
