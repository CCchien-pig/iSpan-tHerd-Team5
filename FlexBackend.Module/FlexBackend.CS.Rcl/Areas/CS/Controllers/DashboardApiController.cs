using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FlexBackend.CS.Rcl.Areas.CS.Controllers
{
    /// <summary>
    /// 後台儀表板 API（只回 JSON，不走 Razor）
    /// </summary>
    [ApiController]
    [Area("CS")]
    [Produces("application/json")]
    [Route("api/cs/dashboard/[action]")]
    [Route("api/cs/dashboard")] // 基底路由
    public class DashboardApiController : ControllerBase
    {
        private readonly string _connStr;

        /// <summary>
        /// 連線字串：優先 THerdDB，找不到就用 DefaultConnection
        /// </summary>
        public DashboardApiController(IConfiguration cfg)
        {
            _connStr =
                cfg.GetConnectionString("THerdDB")
                ?? cfg.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("找不到資料庫連線字串（THerdDB 或 DefaultConnection）。");
        }

        /// <summary>
        /// 折線圖：近 N 天每日淨營收(net)與訂單數(orders) + KPI
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Overview([FromQuery] int days = 30)
        {
            if (days < 1) days = 1;

            var end = DateTime.Today;               // 含當天
            var start = end.AddDays(-(days - 1));   // 起日（含）
            var endPlusOne = end.AddDays(1);        // < end+1

            // labels（yyyy-MM-dd，前端可再轉 zh-TW 顯示）
            var labels = Enumerable.Range(0, days)
                                   .Select(i => start.AddDays(i).ToString("yyyy-MM-dd"))
                                   .ToArray();

            // 依天彙總淨額與訂單數
            // ※ 欄位若為 nvarchar 用 TRY_CONVERT 避免拋例外
            const string sql = @"
SELECT
  CAST(o.CreatedDate AS date) AS Dte,
  SUM(
      TRY_CONVERT(decimal(20,2), o.Subtotal)
    + TRY_CONVERT(decimal(20,2), o.ShippingFee)
    + TRY_CONVERT(decimal(20,2), o.DiscountTotal)
  ) AS Net,
  COUNT(*) AS Orders
FROM dbo.ORD_Order o
WHERE o.PaymentStatus = 'paid'
  AND o.CreatedDate >= @start
  AND o.CreatedDate <  @endPlusOne
GROUP BY CAST(o.CreatedDate AS date)
ORDER BY Dte;";

            var netMap = new Dictionary<DateTime, decimal>();
            var orderMap = new Dictionary<DateTime, int>();

            await using (var cn = new SqlConnection(_connStr))
            await using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime2) { Value = start });
                cmd.Parameters.Add(new SqlParameter("@endPlusOne", SqlDbType.DateTime2) { Value = endPlusOne });

                await cn.OpenAsync();
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var dte = rd.GetDateTime(rd.GetOrdinal("Dte")).Date;

                    var netVal = rd.IsDBNull(rd.GetOrdinal("Net"))
                        ? 0m
                        : rd.GetDecimal(rd.GetOrdinal("Net"));

                    var orderCnt = rd.IsDBNull(rd.GetOrdinal("Orders"))
                        ? 0
                        : rd.GetInt32(rd.GetOrdinal("Orders"));

                    netMap[dte] = netVal;
                    orderMap[dte] = orderCnt;
                }
            }

            // 依 labels 補 0（避免時間斷層造成圖表缺值）
            var netSeries = new decimal[days];
            var ordersSeries = new int[days];
            for (int i = 0; i < days; i++)
            {
                var d = start.AddDays(i).Date;
                netSeries[i] = netMap.TryGetValue(d, out var n) ? n : 0m;
                ordersSeries[i] = orderMap.TryGetValue(d, out var o) ? o : 0;
            }

            // KPI（本期總額、年化、日均訂單）
            var monthly = netSeries.Sum();
            var annual = monthly * 12m;
            var avgDailyOrders = ordersSeries.Length == 0 ? 0 : ordersSeries.Average();

            return Ok(new
            {
                labels,
                net = netSeries,
                orders = ordersSeries,
                kpi = new
                {
                    monthly = Math.Round(monthly, 0),
                    annual = Math.Round(annual, 0),
                    avgDailyOrders = Math.Round((decimal)avgDailyOrders, 0)
                }
            });
        }

        // ───────────────────────────────────────────────────────────────────────────
        //  產品品類清單（提供前端下拉選單）
        //  - 回傳實際存在的 ProductTypeCode / ProductTypeName
        //  - 若偵測到商品未掛任何品類，會補 __NULL__/未分類
        // ───────────────────────────────────────────────────────────────────────────
        public record CodeName(string code, string name);
        // 明確路徑：/api/cs/dashboard/productTypes
        [HttpGet("productTypes")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductTypes()
        {
            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            // 1) 讀出實際有對應設定的品類（避免出現孤兒設定）
            // 子分類（leaf）：與右側長條、Top 表格一致（例：草本補充品 HERB）
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT ProductTypeCode AS Code,
               ProductTypeName AS Name
        FROM PROD_ProductTypeConfig
        WHERE ParentId IS NOT NULL AND IsActive = 1
        ORDER BY ParentId, OrderSeq, ProductTypeName;";



            var list = new List<CodeName>();
            using (var rdr = await cmd.ExecuteReaderAsync())
            {
                while (await rdr.ReadAsync())
                {
                    list.Add(new CodeName(
                        rdr.GetString(0),
                        rdr.GetString(1)
                    ));
                }
            }

            // 2) 檢查是否有「未分類」的產品（沒掛到任何品類）
            using var cmdNull = conn.CreateCommand();
            cmdNull.CommandText = @"
SELECT TOP(1) 1
FROM dbo.PROD_Product p
LEFT JOIN dbo.PROD_ProductType pt ON pt.ProductId = p.ProductId
WHERE pt.ProductId IS NULL;";
            var hasUnclassified = (await cmdNull.ExecuteScalarAsync()) != null;
            if (hasUnclassified)
            {
                list.Add(new CodeName("__NULL__", "未分類"));
            }

            return Ok(list);
        }

        // ───────────────────────────────────────────────────────────────────────────
        //  Top 熱銷與品類彙總（真資料；帶入主分類，若無則回「未分類」）
        //  參數：
        //    - days：區間天數
        //    - top ：取前幾名（Top N）
        //    - category：""=全部 / "__NULL__"=未分類 / 其餘=ProductTypeCode
        //
        //  作法：
        //    1) #valid_orders：取最近 N 天且 PaymentStatus='paid' 的有效訂單
        //    2) #item_adj    ：彙總每個 OrderItem 的調整金額（避免重複相加）
        //    3) KPI          ：訂單數 / 銷量 / 營收
        //    4) Top 產品     ：串接商品與「主分類」，沒有主分類就取排序最前的一個，再沒有就回「未分類」
        //    5) 分類彙總     ：同樣以主分類（或最前的一個）計入，並回傳各分類的數量與營收
        //    6) 兩個查詢都加入「依 category 篩選」條件
        // ───────────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Tops(
            [FromQuery] int days = 30,
            [FromQuery] int top = 10,
            [FromQuery] string category = "")
        {
            if (days < 1) days = 1;
            if (top < 1) top = 10;

            var end = DateTime.Today;               // 含當天
            var start = end.AddDays(-(days - 1));   // 起日（含）
            var endPlusOne = end.AddDays(1);        // < end+1

            const string sql = @"
DECLARE @start       datetime2 = @p_start;
DECLARE @endPlusOne  datetime2 = @p_endPlusOne;
DECLARE @top         int       = @p_top;
DECLARE @category    nvarchar(50) = @p_category;

-- 保險：若先前遺留就先移除
IF OBJECT_ID('tempdb..#valid_orders') IS NOT NULL DROP TABLE #valid_orders;
IF OBJECT_ID('tempdb..#item_adj')     IS NOT NULL DROP TABLE #item_adj;

-- 1) 有效訂單（付款完成；用 CreatedDate 與 Overview 保持一致）
SELECT o.OrderId
INTO #valid_orders
FROM dbo.ORD_Order o
WHERE o.PaymentStatus = 'paid'
  AND o.CreatedDate >= @start
  AND o.CreatedDate <  @endPlusOne;

-- 2) 每筆 OrderItem 的調整金額彙總
SELECT
    oi.OrderItemId,
    SUM(COALESCE(TRY_CONVERT(decimal(20,2), oia.Amount), 0)) AS AdjAmount
INTO #item_adj
FROM dbo.ORD_OrderItem oi
LEFT JOIN dbo.ORD_OrderItemAdjustment oia ON oia.OrderItemId = oi.OrderItemId
GROUP BY oi.OrderItemId;

-- 3) KPI：訂單 / 銷量 / 營收
SELECT
    COUNT(DISTINCT vo.OrderId) AS TotalOrders,         -- 訂單數
    SUM(oi.Qty)               AS UnitsSold,           -- 銷量
    SUM(                                             -- 營收（單價*數量 + 調整）
        COALESCE(TRY_CONVERT(decimal(20,2), oi.UnitPrice), 0) * oi.Qty
      + COALESCE(ia.AdjAmount, 0)
    ) AS TotalRevenue
FROM dbo.ORD_OrderItem oi
JOIN #valid_orders vo ON vo.OrderId = oi.OrderId
LEFT JOIN #item_adj ia ON ia.OrderItemId = oi.OrderItemId;

-- 4) Top 產品（帶主分類；若沒有就取排序最前的一個，最後以 '未分類' 補）
SELECT TOP (@top)
    p.ProductId,
    p.ProductCode,
    p.ProductName,
    COALESCE(cat.ProductTypeName, N'未分類') AS Category,  -- 分類名稱
    SUM(oi.Qty) AS Qty,
    SUM(
        COALESCE(TRY_CONVERT(decimal(20,2), oi.UnitPrice), 0) * oi.Qty
      + COALESCE(ia.AdjAmount, 0)
    ) AS Revenue
FROM dbo.ORD_OrderItem oi
JOIN #valid_orders vo ON vo.OrderId = oi.OrderId
LEFT JOIN #item_adj ia ON ia.OrderItemId = oi.OrderItemId
JOIN dbo.PROD_Product p ON p.ProductId = oi.ProductId
OUTER APPLY (                                            -- 取主分類（IsPrimary=1），若沒有就取排序最前的一個
    SELECT TOP(1)
           ptc.ProductTypeId,
           ptc.ProductTypeCode,
           ptc.ProductTypeName
    FROM dbo.PROD_ProductType       pt
    JOIN dbo.PROD_ProductTypeConfig ptc ON pt.ProductTypeId = ptc.ProductTypeId
    WHERE pt.ProductId = p.ProductId
    ORDER BY pt.IsPrimary DESC, ptc.OrderSeq ASC, ptc.ProductTypeName
) AS cat
WHERE
    (
          @category = ''                                         -- 全部
       OR (@category = '__NULL__' AND cat.ProductTypeId IS NULL) -- 未分類
       OR (cat.ProductTypeCode = @category)                      -- 指定品類
    )
GROUP BY p.ProductId, p.ProductCode, p.ProductName, cat.ProductTypeName
ORDER BY Revenue DESC;

-- 5) 依分類彙總（同樣以主分類/排序最前者為準，沒有就 '未分類'）
SELECT
    COALESCE(cat.ProductTypeName, N'未分類') AS Category,
    SUM(oi.Qty) AS Qty,
    SUM(
        COALESCE(TRY_CONVERT(decimal(20,2), oi.UnitPrice), 0) * oi.Qty
      + COALESCE(ia.AdjAmount, 0)
    ) AS Revenue
FROM dbo.ORD_OrderItem oi
JOIN #valid_orders vo ON vo.OrderId = oi.OrderId
LEFT JOIN #item_adj ia ON ia.OrderItemId = oi.OrderItemId
OUTER APPLY (                                            -- 以每筆項目所屬商品，找出其主分類/排序最前的一個
    SELECT TOP(1)
           ptc.ProductTypeId,
           ptc.ProductTypeCode,
           ptc.ProductTypeName
    FROM dbo.PROD_ProductType       pt
    JOIN dbo.PROD_ProductTypeConfig ptc ON pt.ProductTypeId = ptc.ProductTypeId
    WHERE pt.ProductId = oi.ProductId
    ORDER BY pt.IsPrimary DESC, ptc.OrderSeq ASC, ptc.ProductTypeName
) AS cat
WHERE
    (
          @category = ''                                         -- 全部
       OR (@category = '__NULL__' AND cat.ProductTypeId IS NULL) -- 未分類
       OR (cat.ProductTypeCode = @category)                      -- 指定品類
    )
GROUP BY cat.ProductTypeName
ORDER BY Revenue DESC;

-- 清掉暫存表
DROP TABLE #item_adj;
DROP TABLE #valid_orders;
";

            try
            {
                object kpi = new { totalOrders = 0, unitsSold = 0, totalRevenue = 0m, aov = 0m };
                var topProducts = new List<object>();
                var catRows = new List<(string Category, int Qty, decimal Revenue)>();

                await using var cn = new SqlConnection(_connStr);
                await using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@p_start", SqlDbType.DateTime2) { Value = start });
                cmd.Parameters.Add(new SqlParameter("@p_endPlusOne", SqlDbType.DateTime2) { Value = endPlusOne });
                cmd.Parameters.Add(new SqlParameter("@p_top", SqlDbType.Int) { Value = top });
                cmd.Parameters.Add(new SqlParameter("@p_category", SqlDbType.NVarChar, 50) { Value = (object?)category ?? "" });

                await cn.OpenAsync();
                using var rd = await cmd.ExecuteReaderAsync();

                // 3) KPI
                if (await rd.ReadAsync())
                {
                    var totalOrders = rd.IsDBNull(0) ? 0 : rd.GetInt32(0);
                    var unitsSold = rd.IsDBNull(1) ? 0 : rd.GetInt32(1);
                    var totalRevenue = rd.IsDBNull(2) ? 0m : rd.GetDecimal(2);
                    var aov = totalOrders == 0 ? 0m : totalRevenue / totalOrders;

                    kpi = new
                    {
                        totalOrders,
                        unitsSold,
                        totalRevenue = Math.Round(totalRevenue, 0),
                        aov = Math.Round(aov, 0)
                    };
                }

                // 4) Top 產品
                await rd.NextResultAsync();
                while (await rd.ReadAsync())
                {
                    string sku = rd.IsDBNull(rd.GetOrdinal("ProductCode")) ? "" : rd.GetString(rd.GetOrdinal("ProductCode"));
                    string name = rd.IsDBNull(rd.GetOrdinal("ProductName")) ? "" : rd.GetString(rd.GetOrdinal("ProductName"));
                    string cat = rd.IsDBNull(rd.GetOrdinal("Category")) ? "未分類" : rd.GetString(rd.GetOrdinal("Category"));
                    int qty = rd.IsDBNull(rd.GetOrdinal("Qty")) ? 0 : rd.GetInt32(rd.GetOrdinal("Qty"));
                    decimal rev = rd.IsDBNull(rd.GetOrdinal("Revenue")) ? 0m : rd.GetDecimal(rd.GetOrdinal("Revenue"));

                    topProducts.Add(new { Sku = sku, Name = name, Category = cat, Qty = qty, Revenue = Math.Round(rev, 0) });
                }

                // 5) 分類彙總
                await rd.NextResultAsync();
                while (await rd.ReadAsync())
                {
                    string cat = rd.IsDBNull(rd.GetOrdinal("Category")) ? "未分類" : rd.GetString(rd.GetOrdinal("Category"));
                    int qty = rd.IsDBNull(rd.GetOrdinal("Qty")) ? 0 : rd.GetInt32(rd.GetOrdinal("Qty"));
                    decimal rev = rd.IsDBNull(rd.GetOrdinal("Revenue")) ? 0m : rd.GetDecimal(rd.GetOrdinal("Revenue"));
                    catRows.Add((cat, qty, rev));
                }

                // 計算 Top 產品的占比（以分類彙總的 totalRevenue 當分母）
                var totalRev = catRows.Sum(x => x.Revenue);
                var withShare = topProducts.Select((x, i) =>
                {
                    dynamic d = x;
                    decimal r = d.Revenue;
                    return new
                    {
                        Rank = i + 1,
                        Sku = (string)d.Sku,
                        Name = (string)d.Name,
                        Category = (string)d.Category,
                        Qty = (int)d.Qty,
                        Revenue = r,
                        Share = totalRev == 0 ? 0m : Math.Round(r / totalRev * 100m, 1)
                    };
                }).ToList();

                return Ok(new
                {
                    days,
                    top,
                    category,
                    kpi,
                    topProducts = withShare,
                    categories = new
                    {
                        labels = catRows.Select(x => x.Category).ToArray(),
                        qty = catRows.Select(x => x.Qty).ToArray(),
                        revenue = catRows.Select(x => Math.Round(x.Revenue, 0)).ToArray()
                    }
                });
            }
            catch (Exception ex)
            {
                // 開發期方便查 500 錯誤來源；上線前可移除或改為統一錯誤訊息
                return Problem(title: "Tops failed", detail: ex.ToString(), statusCode: 500);
            }
        }// using System.Data;
         // using Microsoft.Data.SqlClient;

        [HttpGet("memberPurchaseStats")]
        public async Task<IActionResult> MemberPurchaseStats([FromQuery] int minUser = 1001, [FromQuery] int maxUser = 1100, [FromQuery] int days = 30)
        {
            var end = DateTime.Now.Date.AddDays(1);
            var start = (days <= 0) ? DateTime.MinValue : end.AddDays(-days);

            const string sql = @"
SELECT 
    o.UserNumberId AS MemberId,
    COUNT(o.OrderId) AS OrderCount,
    SUM(o.Subtotal + o.ShippingFee) AS TotalAmount,
    AVG(o.Subtotal + o.ShippingFee) AS AvgAmount
FROM dbo.ORD_Order o
WHERE o.UserNumberId BETWEEN @minUser AND @maxUser
  AND o.CreatedDate >= @start AND o.CreatedDate < @end
GROUP BY o.UserNumberId
HAVING COUNT(o.OrderId) > 0
ORDER BY o.UserNumberId;";

            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@minUser", minUser);
            cmd.Parameters.AddWithValue("@maxUser", maxUser);
            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);

            var labels = new List<string>();
            var orders = new List<int>();
            var totals = new List<decimal>();
            var avgs = new List<decimal>();

            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                labels.Add(rdr["MemberId"].ToString()!);
                orders.Add((int)rdr["OrderCount"]);
                totals.Add((decimal)rdr["TotalAmount"]);
                avgs.Add((decimal)rdr["AvgAmount"]);
            }

            return Ok(new
            {
                labels,
                datasets = new object[]
                {
            new { label = "訂單數", data = orders },
            new { label = "消費總額", data = totals },
            new { label = "平均消費", data = avgs }
                }
            });
        }

        [HttpGet("logisticsUsage")]
        public async Task<IActionResult> LogisticsUsage([FromQuery] int days = 30,
                                                       [FromQuery] string? orderNoFrom = null,
                                                       [FromQuery] string? orderNoTo = null)
        {
            // 支援二選一過濾：1) 以 CreatedDate 最近 N 天 2) 以 OrderNo 字串區間
            var end = DateTime.Now.Date.AddDays(1);
            var start = (days <= 0) ? DateTime.MinValue : end.AddDays(-days);

            // 兩種 where 子句擇一
            var where = string.IsNullOrWhiteSpace(orderNoFrom) || string.IsNullOrWhiteSpace(orderNoTo)
                ? "o.CreatedDate >= @start AND o.CreatedDate < @end"
                : "o.OrderNo BETWEEN @orderNoFrom AND @orderNoTo";

            var sql = $@"
SELECT 
    ISNULL(l.LogisticsName, N'未指定') AS LogisticsName,
    COUNT(o.OrderId) AS UseCount,
    AVG(o.ShippingFee) AS AvgShippingFee,
    SUM(o.Subtotal + o.ShippingFee) AS GrossRevenue
FROM dbo.ORD_Order o
LEFT JOIN dbo.SUP_Logistics l ON o.LogisticsId = l.LogisticsId
WHERE {where}
GROUP BY l.LogisticsName
ORDER BY COUNT(o.OrderId) DESC;";

            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            if (where.Contains("CreatedDate"))
            {
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
            }
            else
            {
                cmd.Parameters.AddWithValue("@orderNoFrom", orderNoFrom!);
                cmd.Parameters.AddWithValue("@orderNoTo", orderNoTo!);
            }

            var labels = new List<string>();
            var counts = new List<int>();
            var avgFees = new List<decimal>();
            var revenues = new List<decimal>();

            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                labels.Add(rdr["LogisticsName"].ToString()!);
                counts.Add((int)rdr["UseCount"]);
                avgFees.Add((decimal)rdr["AvgShippingFee"]);
                revenues.Add((decimal)rdr["GrossRevenue"]);
            }

            return Ok(new
            {
                labels,
                // 給圓餅圖（使用次數），也順便帶其他可切換的資料集
                datasets = new object[]
                {
            new { label = "使用次數", data = counts },
            new { label = "平均運費", data = avgFees },
            new { label = "總營業額", data = revenues }
                }
            });
        }

    }
}
