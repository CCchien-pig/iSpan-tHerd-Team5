using tHerdBackend.Infra.DBSetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace tHerdBackend.Infra.Helpers
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
        public static Task<(IDbConnection conn, IDbTransaction? tx, bool shouldDispose)>
                    GetConnectionAsync(DbContext db, ISqlConnectionFactory factory, CancellationToken ct = default)
        {
            var efConn = db.Database.GetDbConnection();

            // EF 目前有交易 → 共用 EF 連線與交易
            if (db.Database.CurrentTransaction != null)
            {
                if (efConn.State != ConnectionState.Open)
                    efConn.Open();  // 改回同步 Open()

                var efTx = db.Database.CurrentTransaction.GetDbTransaction();
                return Task.FromResult<(IDbConnection, IDbTransaction?, bool)>((efConn, efTx, false));
            }

            // 沒有交易 → 用工廠建立新連線
            var conn = factory.Create();
            if (conn.State != ConnectionState.Open)
                conn.Open();  // 改回同步 Open()

            return Task.FromResult<(IDbConnection, IDbTransaction?, bool)>((conn, null, true));
        }
    }
}
