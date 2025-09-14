using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels.Returns;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
    [Area("ORD")]
    public class RmaController : Controller
    {
        private readonly string _cnStr;

        // MOD/CODEID 常數（依你們實際 SysCode 設定可調整）
        private const string MOD = "ORD";
        private const string CID_RR_STATUS = "06"; // ReturnRequest.Status: pending/review/refunding/reshipping/done/rejected
        private const string CID_RR_TYPE = "08"; // ReturnRequest.RequestType: refund / reship
        private const string CID_RR_SCOPE = "10"; // ReturnRequest.RefundScope: order / items

        public RmaController(IConfiguration cfg)
        {
            _cnStr = cfg.GetConnectionString("tHerdDB")
                  ?? cfg.GetConnectionString("therd_db")
                  ?? cfg.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("Missing connection string: tHerdDB / therd_db / DefaultConnection");
        }

        // 狀態群組
        private static readonly Dictionary<string, string[]> StatusGroups =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["pending"] = new[] { "pending", "review" },
                ["approved"] = new[] { "reshipping", "refunding" },
                ["done"] = new[] { "done" },
                ["rejected"] = new[] { "rejected" }
            };

        // 參數複製工具：避免同一個 SqlParameter 被加入兩個命令造成 ArgumentException
        private static void AddParametersCloned(SqlCommand cmd, IEnumerable<SqlParameter> src)
        {
            foreach (var p in src)
            {
                var np = new SqlParameter(p.ParameterName, p.SqlDbType)
                {
                    Size = p.Size,
                    Value = p.Value ?? DBNull.Value
                };
                cmd.Parameters.Add(np);
            }
        }

        // GET: /ORD/Rma
        [HttpGet]
        public async Task<IActionResult> Index(string? group, string? keyword, int page = 1, int pageSize = 10)
        {
            var vm = new ReturnListPageVM
            {
                Group = group,
                Keyword = keyword,
                Page = page < 1 ? 1 : page,
                PageSize = pageSize is < 5 or > 100 ? 10 : pageSize
            };

            await using var cn = new SqlConnection(_cnStr);
            await cn.OpenAsync();

            await FillTabsAsync(cn, vm);
            await FillListAsync(cn, vm);

            return View("Index", vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetReturnDetail(int id)
        {
            if (id <= 0) return Json(new { ok = false, message = "id 無效" });

            await using var cn = new SqlConnection(_cnStr);
            await cn.OpenAsync();

            // --- Master with CodeDesc ---
            const string m = @"
                SELECT 
                  rr.ReturnRequestId, rr.RmaId, rr.OrderId, o.OrderNo,
                  rr.RequestType, ct.CodeDesc  AS RequestTypeName,
                  rr.RefundScope, cs.CodeDesc  AS RefundScopeName,
                  rr.Status,      ISNULL(cst.CodeDesc, rr.Status) AS StatusName,
                  rr.CreatedDate, rr.ReasonText
                FROM dbo.ORD_ReturnRequest rr
                JOIN dbo.ORD_Order o ON o.OrderId = rr.OrderId
                LEFT JOIN dbo.SYS_Code ct  ON ct.ModuleId=@mod AND ct.CodeId=@cid_type  AND ct.CodeNo=rr.RequestType
                LEFT JOIN dbo.SYS_Code cs  ON cs.ModuleId=@mod AND cs.CodeId=@cid_scope AND cs.CodeNo=rr.RefundScope
                LEFT JOIN dbo.SYS_Code cst ON cst.ModuleId=@mod AND cst.CodeId=@cid_status AND cst.CodeNo=rr.Status
                WHERE rr.ReturnRequestId = @id;";

            var rma = new
            {
                ReturnRequestId = 0,
                RmaId = "",
                OrderId = 0,
                OrderNo = "",
                RequestType = "",
                RequestTypeName = "",
                RefundScope = "",
                RefundScopeName = "",
                Status = "",
                StatusName = "",
                CreatedDate = DateTime.MinValue,
                ReasonText = (string?)null
            };

            await using (var cmd = new SqlCommand(m, cn))
            {
                cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                cmd.Parameters.Add(new SqlParameter("@mod", SqlDbType.NVarChar, 10) { Value = MOD });
                cmd.Parameters.Add(new SqlParameter("@cid_type", SqlDbType.NVarChar, 10) { Value = CID_RR_TYPE });
                cmd.Parameters.Add(new SqlParameter("@cid_scope", SqlDbType.NVarChar, 10) { Value = CID_RR_SCOPE });
                cmd.Parameters.Add(new SqlParameter("@cid_status", SqlDbType.NVarChar, 10) { Value = CID_RR_STATUS });

                using var rd = await cmd.ExecuteReaderAsync();
                if (!await rd.ReadAsync()) return Json(new { ok = false, message = "資料不存在" });

                rma = new
                {
                    ReturnRequestId = rd.GetInt32(0),
                    RmaId = rd.IsDBNull(1) ? "" : rd.GetString(1),
                    OrderId = rd.GetInt32(2),
                    OrderNo = rd.GetString(3),
                    RequestType = rd.GetString(4),
                    RequestTypeName = rd.IsDBNull(5) ? rd.GetString(4) : rd.GetString(5),
                    RefundScope = rd.GetString(6),
                    RefundScopeName = rd.IsDBNull(7) ? rd.GetString(6) : rd.GetString(7),
                    Status = rd.GetString(8),
                    StatusName = rd.IsDBNull(9) ? rd.GetString(8) : rd.GetString(9),
                    CreatedDate = rd.GetDateTime(10),
                    ReasonText = rd.IsDBNull(11) ? null : rd.GetString(11)
                };
            }

            // --- Return items  ---
            const string itemsSql = @"
                SELECT RmaItemId, OrderItemId, Qty, ApprovedQty, RefundQty, ReshipQty, RefundUnitAmount
                FROM dbo.ORD_ReturnItem
                WHERE ReturnRequestId = @id
                ORDER BY RmaItemId;";
            var items = new List<object>();
            await using (var ci = new SqlCommand(itemsSql, cn))
            {
                ci.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                using var rd = await ci.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    items.Add(new
                    {
                        RmaItemId = rd.GetInt32(0),
                        OrderItemId = rd.GetInt32(1),
                        Qty = rd.GetInt32(2),
                        ApprovedQty = rd.GetInt32(3),
                        RefundQty = rd.GetInt32(4),
                        ReshipQty = rd.GetInt32(5),
                        RefundUnitAmount = rd.IsDBNull(6) ? (decimal?)null : rd.GetDecimal(6)
                    });
                }
            }

            return Json(new { ok = true, rma, items });
        }


        // POST: /ORD/Rma/Approve 
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string nextStatus)
        {
            if (id <= 0) return Json(new { ok = false, message = "id 無效" });
            nextStatus = (nextStatus ?? "").Trim().ToLowerInvariant();

            var allowedNext = new[] { "refunding", "reshipping" };
            if (!allowedNext.Contains(nextStatus))
                return Json(new { ok = false, message = "nextStatus 僅能為 refunding 或 reshipping" });

            await using var cn = new SqlConnection(_cnStr);
            await cn.OpenAsync();

            const int MAX_RETRY = 3;
            for (int attempt = 1; attempt <= MAX_RETRY; attempt++)
            {
                await using var tx = await cn.BeginTransactionAsync(IsolationLevel.Serializable);
                try
                {
                    const string qSql = @"
                        SELECT rr.RmaId, rr.Status
                        FROM dbo.ORD_ReturnRequest rr WITH (UPDLOCK, ROWLOCK)
                        WHERE rr.ReturnRequestId = @id;";

                    string? currentRmaId = null;
                    string? currentStatus = null;

                    await using (var q = new SqlCommand(qSql, cn, (SqlTransaction)tx))
                    {
                        q.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                        using var rd = await q.ExecuteReaderAsync();
                        if (!await rd.ReadAsync())
                            return Json(new { ok = false, message = $"ReturnRequestId={id} 不存在" });

                        currentRmaId = rd.IsDBNull(0) ? null : rd.GetString(0);
                        currentStatus = rd.GetString(1);
                    }

                    if (!(currentStatus == "pending" || currentStatus == "review"))
                        return Json(new { ok = false, message = $"目前狀態為 {currentStatus} ，不可批准" });

                    var rmaId = currentRmaId ?? await GenerateRmaIdAsync(cn, (SqlTransaction)tx);

                    const string uSql = @"
                        UPDATE dbo.ORD_ReturnRequest
                        SET 
                            RmaId  = CASE WHEN RmaId IS NULL THEN @rmaId ELSE RmaId END,
                            Status = @nextStatus
                        WHERE ReturnRequestId = @id;";

                    await using (var u = new SqlCommand(uSql, cn, (SqlTransaction)tx))
                    {
                        u.Parameters.Add(new SqlParameter("@rmaId", SqlDbType.NVarChar, 30) { Value = rmaId });
                        u.Parameters.Add(new SqlParameter("@nextStatus", SqlDbType.NVarChar, 20) { Value = nextStatus });
                        u.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                        await u.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    return Json(new { ok = true, message = "批准成功", id, rmaId, status = nextStatus });
                }
                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                {
                    await tx.RollbackAsync(); // RmaId 衝突重試
                    if (attempt == MAX_RETRY)
                        return Json(new { ok = false, message = "RMA 產號衝突，請重試" });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    return Json(new { ok = false, message = ex.Message });
                }
            }

            return Json(new { ok = false, message = "未知錯誤" });
        }

        // POST: /ORD/Rma/Reject
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            if (id <= 0) return Json(new { ok = false, message = "id 無效" });

            await using var cn = new SqlConnection(_cnStr);
            await cn.OpenAsync();

            await using var tx = await cn.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                const string qSql = @"
                    SELECT rr.Status
                    FROM dbo.ORD_ReturnRequest rr WITH (UPDLOCK, ROWLOCK)
                    WHERE rr.ReturnRequestId = @id;";

                string? currentStatus = null;
                await using (var q = new SqlCommand(qSql, cn, (SqlTransaction)tx))
                {
                    q.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                    using var rd = await q.ExecuteReaderAsync();
                    if (!await rd.ReadAsync())
                        return Json(new { ok = false, message = $"ReturnRequestId={id} 不存在" });
                    currentStatus = rd.GetString(0);
                }

                if (!(currentStatus == "pending" || currentStatus == "review"))
                    return Json(new { ok = false, message = $"目前狀態為 {currentStatus} ，不可拒絕" });

                var sql = string.IsNullOrWhiteSpace(reason)
                    ? @"UPDATE dbo.ORD_ReturnRequest SET Status='rejected' WHERE ReturnRequestId=@id;"
                    : @"UPDATE dbo.ORD_ReturnRequest SET Status='rejected', ReasonText=@reason WHERE ReturnRequestId=@id;";

                await using (var u = new SqlCommand(sql, cn, (SqlTransaction)tx))
                {
                    u.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                    if (!string.IsNullOrWhiteSpace(reason))
                        u.Parameters.Add(new SqlParameter("@reason", SqlDbType.NVarChar, 400) { Value = reason! });
                    await u.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return Json(new { ok = true, message = "已駁回", id, status = "rejected" });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { ok = false, message = ex.Message });
            }
        }

        // POST: /ORD/Rma/Complete
        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            if (id <= 0) return Json(new { ok = false, message = "id 無效" });

            await using var cn = new SqlConnection(_cnStr);
            await cn.OpenAsync();

            await using var tx = await cn.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                const string q = @"SELECT Status FROM dbo.ORD_ReturnRequest WITH(UPDLOCK,ROWLOCK) WHERE ReturnRequestId=@id;";
                string? st;
                await using (var cmd = new SqlCommand(q, cn, (SqlTransaction)tx))
                {
                    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                    st = (string?)await cmd.ExecuteScalarAsync();
                }
                if (st is null) return Json(new { ok = false, message = "資料不存在" });
                if (st != "refunding" && st != "reshipping")
                    return Json(new { ok = false, message = $"目前狀態為 {st}，不可結單" });

                const string u = @"UPDATE dbo.ORD_ReturnRequest SET Status='done' WHERE ReturnRequestId=@id;";
                await using (var cmd = new SqlCommand(u, cn, (SqlTransaction)tx))
                {
                    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return Json(new { ok = true, message = "已結單 (done)", status = "done" });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { ok = false, message = ex.Message });
            }
        }




        // ----------------------------------------------------
        // 以下為私有輔助：tabs、清單(含 CodeDesc)、where、產號
        // ----------------------------------------------------

        private static async Task FillTabsAsync(SqlConnection cn, ReturnListPageVM vm)
        {
            const string sql = @"
                SELECT 
                  COUNT(*) AS AllCount,
                  SUM(CASE WHEN rr.Status IN ('pending','review') THEN 1 ELSE 0 END) AS Pending,
                  SUM(CASE WHEN rr.Status IN ('reshipping','refunding') THEN 1 ELSE 0 END) AS Approved,
                  SUM(CASE WHEN rr.Status IN ('done') THEN 1 ELSE 0 END) AS Done,       
                  SUM(CASE WHEN rr.Status IN ('rejected') THEN 1 ELSE 0 END) AS Rejected
                FROM dbo.ORD_ReturnRequest rr;";

            using var cmd = new SqlCommand(sql, cn);
            using var rd = await cmd.ExecuteReaderAsync();
            if (await rd.ReadAsync())
            {
                vm.Tabs = new ReturnTabsCountVM
                {
                    All = rd.GetInt32(0),
                    Pending = rd.GetInt32(1),
                    Approved = rd.GetInt32(2),
                    Done = rd.GetInt32(3), 
                    Rejected = rd.GetInt32(4)
                };
            }

        }

        private static (string where, List<SqlParameter> ps) BuildWhere(ReturnListPageVM vm)
        {
            var where = "WHERE 1=1";
            var ps = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(vm.Keyword))
            {
                where += " AND (rr.RmaId LIKE '%' + @kw + '%' OR o.OrderNo LIKE '%' + @kw + '%')";
                ps.Add(new SqlParameter("@kw", SqlDbType.NVarChar, 50) { Value = vm.Keyword! });
            }

            if (!string.IsNullOrWhiteSpace(vm.Group) && StatusGroups.TryGetValue(vm.Group!, out var arr))
            {
                var names = new List<string>();
                for (var i = 0; i < arr.Length; i++)
                {
                    var p = $"@s{i}";
                    names.Add(p);
                    ps.Add(new SqlParameter(p, SqlDbType.NVarChar, 20) { Value = arr[i] });
                }
                where += $" AND rr.Status IN ({string.Join(",", names)})";
            }

            return (where, ps);
        }

        // 取 CodeDesc → 對應到 *Name 三欄
        private static async Task FillListAsync(SqlConnection cn, ReturnListPageVM vm)
        {
            var (where, ps) = BuildWhere(vm);

            var countSql = $@"
                SELECT COUNT(*)
                FROM dbo.ORD_ReturnRequest rr
                JOIN dbo.ORD_Order o ON o.OrderId = rr.OrderId
                {where};";

            await using (var cmdCount = new SqlCommand(countSql, cn))
            {
                AddParametersCloned(cmdCount, ps);
                vm.Total = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
            }

            var listSql = $@"
                SELECT 
                  rr.ReturnRequestId, rr.RmaId, rr.OrderId, o.OrderNo,

                  rr.RequestType, ct.CodeDesc  AS RequestTypeName,
                  rr.RefundScope, cs.CodeDesc  AS RefundScopeName,
                  rr.Status,      ISNULL(cst.CodeDesc, rr.Status) AS StatusName,

                  rr.CreatedDate, rr.ReasonText
                FROM dbo.ORD_ReturnRequest rr
                JOIN dbo.ORD_Order o ON o.OrderId = rr.OrderId
                LEFT JOIN dbo.SYS_Code ct  ON ct.ModuleId=@mod AND ct.CodeId=@cid_type  AND ct.CodeNo=rr.RequestType
                LEFT JOIN dbo.SYS_Code cs  ON cs.ModuleId=@mod AND cs.CodeId=@cid_scope AND cs.CodeNo=rr.RefundScope
                LEFT JOIN dbo.SYS_Code cst ON cst.ModuleId=@mod AND cst.CodeId=@cid_status AND cst.CodeNo=rr.Status
                {where}
                ORDER BY rr.ReturnRequestId DESC
                OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;";

            await using var cmdList = new SqlCommand(listSql, cn);
            AddParametersCloned(cmdList, ps);
            cmdList.Parameters.Add(new SqlParameter("@mod", SqlDbType.NVarChar, 10) { Value = MOD });
            cmdList.Parameters.Add(new SqlParameter("@cid_type", SqlDbType.NVarChar, 10) { Value = CID_RR_TYPE });
            cmdList.Parameters.Add(new SqlParameter("@cid_scope", SqlDbType.NVarChar, 10) { Value = CID_RR_SCOPE });
            cmdList.Parameters.Add(new SqlParameter("@cid_status", SqlDbType.NVarChar, 10) { Value = CID_RR_STATUS });
            cmdList.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = (vm.Page - 1) * vm.PageSize });
            cmdList.Parameters.Add(new SqlParameter("@take", SqlDbType.Int) { Value = vm.PageSize });

            await using var rd = await cmdList.ExecuteReaderAsync();
            var list = new List<ReturnListItemVM>();
            while (await rd.ReadAsync())
            {
                list.Add(new ReturnListItemVM
                {
                    ReturnRequestId = rd.GetInt32(0),
                    RmaId = rd.IsDBNull(1) ? "" : rd.GetString(1),
                    OrderId = rd.GetInt32(2),
                    OrderNo = rd.GetString(3),

                    RequestType = rd.GetString(4),                           // 原碼
                    RequestTypeName = rd.IsDBNull(5) ? rd.GetString(4) : rd.GetString(5),
                    RefundScope = rd.GetString(6),
                    RefundScopeName = rd.IsDBNull(7) ? rd.GetString(6) : rd.GetString(7),
                    Status = rd.GetString(8),
                    StatusName = rd.IsDBNull(9) ? rd.GetString(8) : rd.GetString(9),

                    CreatedDate = rd.GetDateTime(10),
                    ReasonText = rd.IsDBNull(11) ? null : rd.GetString(11)
                });
            }
            vm.Items = list;
        }

        private static async Task<string> GenerateRmaIdAsync(SqlConnection cn, SqlTransaction tx)
        {
            var today = DateTime.UtcNow;
            var prefix = "RMA" + today.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            const string sql = @"
                SELECT MAX(RmaId)
                FROM dbo.ORD_ReturnRequest WITH (UPDLOCK, HOLDLOCK)
                WHERE RmaId LIKE @prefix + '%';";

            string? maxId;
            await using (var cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.Add(new SqlParameter("@prefix", SqlDbType.NVarChar, 16) { Value = prefix });
                maxId = (string?)await cmd.ExecuteScalarAsync();
            }

            int next = 1;
            if (!string.IsNullOrEmpty(maxId) && maxId.StartsWith(prefix, StringComparison.Ordinal))
            {
                var tail = maxId[prefix.Length..];
                if (int.TryParse(tail, out var n)) next = n + 1;
            }
            return $"{prefix}{next:D4}";
        }
    }
}
