using System.Data;
using FlexBackend.Infra.DBSetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlexBackend.Infra.Helpers
{
    /// <summary>
    /// 提供 Dapper 與 EF 共用連線的工具方法
    /// </summary>
    public static class DbConnectionHelper
    {
        /// <summary>
        /// 取得一個可用的資料庫連線：
        /// - 若 DbContext 有交易，則共用同一連線與交易
        /// - 否則用工廠建立新連線
        /// </summary>
        public static async Task<(IDbConnection conn, IDbTransaction? tx, bool needDispose)>
            GetConnectionAsync(DbContext db, ISqlConnectionFactory factory, CancellationToken ct = default)
        {
            var efConn = db.Database.GetDbConnection();

            if (db.Database.CurrentTransaction != null)
            {
                if (efConn.State != ConnectionState.Open)
                    await efConn.OpenAsync(ct);

                var efTx = db.Database.CurrentTransaction!.GetDbTransaction();
                return (efConn, efTx, false); // 交給 DbContext 管理，不需自行 Dispose
            }

            var conn = factory.Create();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            return (conn, null, true); // 需自行 Dispose
        }
    }
}
