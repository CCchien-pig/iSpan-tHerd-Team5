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
        // ===== KPI + 同期比較（對上期/去年同期）+ 每日走勢 =====
        // GET /api/cs/dashboard/kpi?days=30&mode=prev&includeSim=1
        [HttpGet("kpi")]
        public async Task<IActionResult> Kpi(
            [FromQuery] int days = 30,
            [FromQuery] string mode = "prev",      // prev=對上期, yoy=去年同期
            [FromQuery] bool includeSim = true     // 先預設含測試款，Demo 比較有數字；上線可改 false
        )
        {
            if (days < 1) days = 30;
            mode = (mode ?? "prev").ToLower();
            if (mode != "prev" && mode != "yoy") mode = "prev";

            var sql = @"
DECLARE @Days int = @p_days;
DECLARE @CompareMode varchar(8) = @p_mode;
DECLARE @IncludeSim bit = @p_includeSim;

DECLARE @today date = CAST(GETDATE() AS date);
DECLARE @curr_start date = DATEADD(day, 1-@Days, @today);
DECLARE @curr_end   date = DATEADD(day, 1, @today);   -- 右開 [start, end)

DECLARE @prev_start date, @prev_end date;
IF (@CompareMode='yoy')
BEGIN
    SET @prev_start = DATEADD(year, -1, @curr_start);
    SET @prev_end   = DATEADD(year, -1, @curr_end);
END
ELSE
BEGIN
    SET @prev_start = DATEADD(day, -@Days, @curr_start);
    SET @prev_end   = @curr_start;
END;

IF OBJECT_ID('tempdb..#pay')      IS NOT NULL DROP TABLE #pay;
IF OBJECT_ID('tempdb..#curr_pay') IS NOT NULL DROP TABLE #curr_pay;
IF OBJECT_ID('tempdb..#prev_pay') IS NOT NULL DROP TABLE #prev_pay;

-- 付款母集合（僅 success/refund；includeSim=0 時會排除模擬付款）
SELECT
    p.OrderId,
    CAST(COALESCE(p.TradeDate, p.CreatedDate) AS date) AS PayDate,
    CAST(p.Amount AS decimal(20,2)) AS Amount,
    p.Status
INTO #pay
FROM dbo.ORD_Payment p
WHERE p.Status IN ('success','refund')
  AND ( @IncludeSim = 1 OR p.SimulatePaid = 0 )
  AND COALESCE(p.TradeDate, p.CreatedDate) >= @prev_start
  AND COALESCE(p.TradeDate, p.CreatedDate) <  @curr_end;

-- 分期
SELECT * INTO #curr_pay FROM #pay WHERE PayDate >= @curr_start AND PayDate < @curr_end;
SELECT * INTO #prev_pay FROM #pay WHERE PayDate >= @prev_start AND PayDate < @prev_end;

;WITH cs AS (
    SELECT
        SUM(CASE WHEN p.Status='success' THEN p.Amount ELSE 0 END) AS SuccessRevenue,  -- 成功收款總額
        SUM(CASE WHEN p.Status='refund'  THEN p.Amount ELSE 0 END) AS RefundAmount,   -- 退款總額
        SUM(CASE WHEN p.Status='success' THEN p.Amount ELSE 0 END)
      - SUM(CASE WHEN p.Status='refund'  THEN p.Amount ELSE 0 END) AS Revenue,        -- 淨額（維持給營收用）
        COUNT(DISTINCT CASE WHEN p.Status='success' THEN p.OrderId END)       AS Orders,
        COUNT(DISTINCT CASE WHEN p.Status='success' THEN o.UserNumberId END)  AS Customers
    FROM #curr_pay p
    LEFT JOIN dbo.ORD_Order o ON o.OrderId = p.OrderId
),
ps AS (
    SELECT
        SUM(CASE WHEN p.Status='success' THEN p.Amount ELSE 0 END) AS SuccessRevenue,
        SUM(CASE WHEN p.Status='refund'  THEN p.Amount ELSE 0 END) AS RefundAmount,
        SUM(CASE WHEN p.Status='success' THEN p.Amount ELSE 0 END)
      - SUM(CASE WHEN p.Status='refund'  THEN p.Amount ELSE 0 END) AS Revenue,
        COUNT(DISTINCT CASE WHEN p.Status='success' THEN p.OrderId END)       AS Orders,
        COUNT(DISTINCT CASE WHEN p.Status='success' THEN o.UserNumberId END)  AS Customers
    FROM #prev_pay p
    LEFT JOIN dbo.ORD_Order o ON o.OrderId = p.OrderId
)
-- 結果集 1：KPI（本期/同期 + AOV）
SELECT 
    ISNULL(cs.Revenue,0)  AS curr_revenue,       -- 仍然是淨額
    ISNULL(ps.Revenue,0)  AS comp_revenue,
    ISNULL(cs.Orders,0)   AS curr_orders,
    ISNULL(ps.Orders,0)   AS comp_orders,
    ISNULL(cs.Customers,0) AS curr_customers,
    ISNULL(ps.Customers,0) AS comp_customers,
    CAST(ISNULL(cs.SuccessRevenue,0) / NULLIF(ISNULL(cs.Orders,0),0) AS decimal(20,2)) AS curr_aov, -- ★ AOV 用成功收款
    CAST(ISNULL(ps.SuccessRevenue,0) / NULLIF(ISNULL(ps.Orders,0),0) AS decimal(20,2)) AS comp_aov  -- ★
FROM cs CROSS JOIN ps;


-- 結果集 2：每日走勢（本期/同期）
SELECT
    'current' AS which, PayDate AS [date],
    SUM(CASE WHEN Status='success' THEN Amount ELSE 0 END)
      - SUM(CASE WHEN Status='refund'  THEN Amount ELSE 0 END) AS Revenue,
    COUNT(DISTINCT CASE WHEN Status='success' THEN OrderId END) AS Orders
FROM #curr_pay
GROUP BY PayDate

UNION ALL

SELECT
    'previous', PayDate,
    SUM(CASE WHEN Status='success' THEN Amount ELSE 0 END)
      - SUM(CASE WHEN Status='refund'  THEN Amount ELSE 0 END) AS Revenue,
    COUNT(DISTINCT CASE WHEN Status='success' THEN OrderId END) AS Orders
FROM #prev_pay
GROUP BY PayDate
ORDER BY which, [date];

DROP TABLE #prev_pay; DROP TABLE #curr_pay; DROP TABLE #pay;
";

            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@p_days", days);
            cmd.Parameters.AddWithValue("@p_mode", mode);
            cmd.Parameters.AddWithValue("@p_includeSim", includeSim ? 1 : 0);

            using var rd = await cmd.ExecuteReaderAsync();

            // 讀 KPI
            decimal curRevenue = 0, prevRevenue = 0, curAov = 0, prevAov = 0;
            int curOrders = 0, prevOrders = 0, curCustomers = 0, prevCustomers = 0;
            if (await rd.ReadAsync())
            {
                curRevenue = rd.GetDecimal(0);
                prevRevenue = rd.GetDecimal(1);
                curOrders = rd.GetInt32(2);
                prevOrders = rd.GetInt32(3);
                curCustomers = rd.GetInt32(4);
                prevCustomers = rd.GetInt32(5);
                curAov = rd.IsDBNull(6) ? 0 : rd.GetDecimal(6);
                prevAov = rd.IsDBNull(7) ? 0 : rd.GetDecimal(7);
            }

            // 讀每日走勢
            var cur = new List<object>();
            var prev = new List<object>();
            await rd.NextResultAsync();
            while (await rd.ReadAsync())
            {
                var which = rd.GetString(0);
                var d = rd.GetDateTime(1);
                var rev = rd.GetDecimal(2);
                var ord = rd.GetInt32(3);
                var row = new { date = d.ToString("yyyy-MM-dd"), revenue = rev, orders = ord };
                if (which == "current") cur.Add(row); else prev.Add(row);
            }

            decimal pct(decimal a, decimal b) => b == 0 ? (a == 0 ? 0 : 100) : Math.Round((a - b) / b * 100, 2);

            return Ok(new
            {
                mode,
                days,
                includeSim,
                current = new { revenue = curRevenue, orders = curOrders, customers = curCustomers, aov = curAov },
                previous = new { revenue = prevRevenue, orders = prevOrders, customers = prevCustomers, aov = prevAov },
                delta = new
                {
                    revenue_pct = pct(curRevenue, prevRevenue),
                    orders_pct = pct(curOrders, prevOrders),
                    customers_pct = pct(curCustomers, prevCustomers),
                    aov_pct = pct(curAov, prevAov)
                },
                trend = new { current = cur, previous = prev }
            });
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

[HttpGet]
    public async Task<IActionResult> Anomalies([FromQuery] bool demo = true, [FromQuery] int days = 30)
    {
        // --- 方案B：預設 demo=true，方便上課/組內展示 ---
        if (demo)
        {
            return Ok(new[]
            {
            "：AOV 為 0（請檢查訂單金額或退款資料）",
            "：有 3 個 SKU 庫存為負數",
            "：有 5 張發票金額與訂單金額不符",
            "：7 天內有 12 批即期品仍上架"
        });
        }

        var list = new List<string>();
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        // 1) AOV 檢查（近 N 天）
        cmd.CommandText = @"
        SELECT CAST(SUM(Subtotal - DiscountTotal + ShippingFee) AS DECIMAL(20,2)) / NULLIF(COUNT(*),0)
        FROM ORD_Order
        WHERE CreatedDate >= DATEADD(DAY, -@days, SYSUTCDATETIME())
          AND PaymentStatus = 'paid'";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@days", days);
        var aovObj = await cmd.ExecuteScalarAsync();
        var aov = (aovObj == DBNull.Value || aovObj == null) ? 0m : (decimal)aovObj;
        if (aov == 0m) list.Add("AOV 為 0（請檢查訂單金額或退款資料）");

        // 2) 庫存為負
        cmd.CommandText = @"SELECT COUNT(*) FROM PROD_ProductSku WHERE StockQty < 0";
        var negCount = (int)(await cmd.ExecuteScalarAsync());
        if (negCount > 0) list.Add($"有 {negCount} 個 SKU 庫存為負數");

        // 3) 訂單 vs 發票金額不符（容許 1 元誤差）
        cmd.CommandText = @"
        SELECT COUNT(*)
        FROM ORD_Order o
        JOIN ORD_Invoice i ON i.OrderId = o.OrderId
        WHERE ABS((o.Subtotal - o.DiscountTotal + o.ShippingFee) - i.Amount) > 1";
        var misAmt = (int)(await cmd.ExecuteScalarAsync());
        if (misAmt > 0) list.Add($"有 {misAmt} 張發票金額與訂單金額不符");

        // 4) 即期品仍上架（7天內到期）
        cmd.CommandText = @"
        SELECT COUNT(*)
        FROM SUP_StockBatch sb
        JOIN PROD_ProductSku s ON s.SkuId = sb.SkuId
        WHERE sb.ExpireDate IS NOT NULL
          AND sb.ExpireDate < DATEADD(DAY, 7, SYSUTCDATETIME())
          AND s.IsActive = 1";
        var nearExp = (int)(await cmd.ExecuteScalarAsync());
        if (nearExp > 0) list.Add($"7 天內有 {nearExp} 批即期品仍上架");

        return Ok(list);
    }



}
}
