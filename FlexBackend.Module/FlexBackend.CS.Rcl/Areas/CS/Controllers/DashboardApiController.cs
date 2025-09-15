using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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

        /// <summary>
        /// Top 熱銷與品類彙總（真資料；帶入主分類，若無則回「未分類」）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Tops([FromQuery] int days = 30, [FromQuery] int top = 10)
        {
            if (days < 1) days = 1;
            if (top < 1) top = 10;

            var end = DateTime.Today;            // 含當天
            var start = end.AddDays(-(days - 1));  // 起日（含）
            var endPlusOne = end.AddDays(1);            // < end+1

            // 說明：
            // 1) #valid_orders：取最近 N 天且 PaymentStatus='paid' 的有效訂單
            // 2) #item_adj    ：彙總每個 OrderItem 的調整金額（避免重複相加）
            // 3) KPI          ：訂單數 / 銷量 / 營收
            // 4) Top 產品     ：串接商品與「主分類」，沒有主分類就取排序最前的一個，再沒有就回「未分類」
            // 5) 分類彙總     ：同樣以主分類（或最前的一個）計入，並回傳各分類的數量與營收
            //
            // ※ 這裡用「暫存表」(#valid_orders / #item_adj) 讓多個查詢結果共用條件；
            //   若用 CTE，作用域只限緊接的那一個查詢，會出現 invalid object name 的錯。
            const string sql = @"
DECLARE @start      datetime2 = @p_start;
DECLARE @endPlusOne datetime2 = @p_endPlusOne;
DECLARE @top        int       = @p_top;

-- 保險：若先前遺留就先移除
IF OBJECT_ID('tempdb..#valid_orders') IS NOT NULL DROP TABLE #valid_orders;
IF OBJECT_ID('tempdb..#item_adj')     IS NOT NULL DROP TABLE #item_adj;

-- 1) 有效訂單（付款完成）
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
    SELECT TOP(1) ptc.ProductTypeName
    FROM dbo.PROD_ProductType       pt
    JOIN dbo.PROD_ProductTypeConfig ptc ON pt.ProductTypeId = ptc.ProductTypeId
    WHERE pt.ProductId = p.ProductId
    ORDER BY pt.IsPrimary DESC, ptc.OrderSeq ASC, ptc.ProductTypeName
) AS cat
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
    SELECT TOP(1) ptc.ProductTypeName
    FROM dbo.PROD_ProductType       pt
    JOIN dbo.PROD_ProductTypeConfig ptc ON pt.ProductTypeId = ptc.ProductTypeId
    WHERE pt.ProductId = oi.ProductId
    ORDER BY pt.IsPrimary DESC, ptc.OrderSeq ASC, ptc.ProductTypeName
) AS cat
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
        }
    }
}
}
