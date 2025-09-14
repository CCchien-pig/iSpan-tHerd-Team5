using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace FlexBackend.CS.Rcl.Areas.CS.Controllers
{
    /// <summary>
    /// 後台儀表板 API（只回 JSON，不走 Razor）
    /// </summary>
    [ApiController]
    [Area("CS")]
    [Produces("application/json")]
    [Route("api/cs/dashboard/[action]")]   // e.g. /api/cs/dashboard/overview
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
            const string sql = @"
SELECT
    CAST(o.CreatedDate AS date) AS Dte,
    SUM(CAST(o.Subtotal AS decimal(20,2))
      + CAST(o.ShippingFee AS decimal(20,2))
      + CAST(o.DiscountTotal AS decimal(20,2))) AS Net,  -- DiscountTotal 可能為負
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

            // 依 labels 補 0
            var netSeries = new decimal[days];
            var ordersSeries = new int[days];
            for (int i = 0; i < days; i++)
            {
                var d = start.AddDays(i).Date;
                netSeries[i] = netMap.TryGetValue(d, out var n) ? n : 0m;
                ordersSeries[i] = orderMap.TryGetValue(d, out var o) ? o : 0;
            }

            var monthly = netSeries.Sum();
            var annual = monthly * 12m;
            var avgDailyOrders = ordersSeries.Average();

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
        /// Top 熱銷與品類彙總（真資料）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Tops([FromQuery] int days = 30, [FromQuery] int top = 10)
        {
            if (days < 1) days = 1;
            if (top < 1) top = 10;

            var end = DateTime.Today;
            var start = end.AddDays(-(days - 1));
            var endPlusOne = end.AddDays(1);

            // 1) KPI（訂單數 / 銷量 / 營收）
            // 2) 依商品彙總取前 N 名
            // 3) 依主分類彙總
            const string sql = @"
DECLARE @start datetime2 = @p_start;
DECLARE @endPlusOne datetime2 = @p_endPlusOne;
DECLARE @top int = @p_top;

WITH valid_orders AS (
    SELECT o.OrderId
    FROM dbo.ORD_Order o
    WHERE o.PaymentStatus = 'paid'
      AND o.CreatedDate >= @start
      AND o.CreatedDate <  @endPlusOne
)
-- 1) KPI
SELECT
    COUNT(DISTINCT vo.OrderId) AS TotalOrders,
    SUM(oi.Qty) AS UnitsSold,
    SUM(CAST(oi.UnitPrice AS decimal(20,2)) * oi.Qty
        + ISNULL(oia.Amount, 0)) AS TotalRevenue
FROM dbo.ORD_OrderItem oi
JOIN valid_orders vo ON vo.OrderId = oi.OrderId
LEFT JOIN dbo.ORD_OrderItemAdjustment oia ON oia.OrderItemId = oi.OrderItemId;

-- 2) Top 產品（用主分類，若無分類則顯示 '未分類'）
SELECT TOP (@top)
    p.ProductId,
    p.ProductCode,
    p.ProductName,
    COALESCE(cat.ProductTypeName, N'未分類') AS Category,
    SUM(oi.Qty) AS Qty,
    SUM(CAST(oi.UnitPrice AS decimal(20,2)) * oi.Qty
        + ISNULL(oia.Amount, 0)) AS Revenue
FROM dbo.ORD_OrderItem oi
JOIN valid_orders vo ON vo.OrderId = oi.OrderId
LEFT JOIN dbo.ORD_OrderItemAdjustment oia ON oia.OrderItemId = oi.OrderItemId
JOIN dbo.PROD_Product p ON p.ProductId = oi.ProductId
OUTER APPLY (
    SELECT TOP(1) ptc.ProductTypeName
    FROM dbo.PROD_ProductType pt
    JOIN dbo.PROD_ProductTypeConfig ptc ON pt.ProductTypeId = ptc.ProductTypeId
    WHERE pt.ProductId = p.ProductId
    ORDER BY pt.IsPrimary DESC, ptc.OrderSeq ASC, ptc.ProductTypeName
) AS cat
GROUP BY p.ProductId, p.ProductCode, p.ProductName, cat.ProductTypeName
ORDER BY Revenue DESC;

-- 3) 分類彙總
SELECT
    COALESCE(cat.ProductTypeName, N'未分類') AS Category,
    SUM(oi.Qty) AS Qty,
    SUM(CAST(oi.UnitPrice AS decimal(20,2)) * oi.Qty
        + ISNULL(oia.Amount, 0)) AS Revenue
FROM dbo.ORD_OrderItem oi
JOIN valid_orders vo ON vo.OrderId = oi.OrderId
LEFT JOIN dbo.ORD_OrderItemAdjustment oia ON oia.OrderItemId = oi.OrderItemId
OUTER APPLY (
    SELECT TOP(1) ptc.ProductTypeName
    FROM dbo.PROD_ProductType pt
    JOIN dbo.PROD_ProductTypeConfig ptc ON pt.ProductTypeId = ptc.ProductTypeId
    WHERE pt.ProductId = oi.ProductId
    ORDER BY pt.IsPrimary DESC, ptc.OrderSeq ASC, ptc.ProductTypeName
) AS cat
GROUP BY cat.ProductTypeName
ORDER BY Revenue DESC;";

            var kpi = new { totalOrders = 0, unitsSold = 0, totalRevenue = 0m, aov = 0m };
            var topProducts = new List<object>();
            var catRows = new List<(string Category, int Qty, decimal Revenue)>();

            await using (var cn = new SqlConnection(_connStr))
            await using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add(new SqlParameter("@p_start", SqlDbType.DateTime2) { Value = start });
                cmd.Parameters.Add(new SqlParameter("@p_endPlusOne", SqlDbType.DateTime2) { Value = endPlusOne });
                cmd.Parameters.Add(new SqlParameter("@p_top", SqlDbType.Int) { Value = top });

                await cn.OpenAsync();
                using var rd = await cmd.ExecuteReaderAsync();

                // 1) KPI
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

                // 2) Top 產品
                await rd.NextResultAsync();
                while (await rd.ReadAsync())
                {
                    var code = rd.IsDBNull(rd.GetOrdinal("ProductCode")) ? "" : rd.GetString(rd.GetOrdinal("ProductCode"));
                    var name = rd.GetString(rd.GetOrdinal("ProductName"));
                    var cat = rd.GetString(rd.GetOrdinal("Category"));
                    var qty = rd.IsDBNull(rd.GetOrdinal("Qty")) ? 0 : rd.GetInt32(rd.GetOrdinal("Qty"));
                    var revenue = rd.IsDBNull(rd.GetOrdinal("Revenue")) ? 0m : rd.GetDecimal(rd.GetOrdinal("Revenue"));

                    topProducts.Add(new
                    {
                        Sku = code,         // 你的資料表是 ProductCode，對齊前端欄位名稱用 Sku
                        Name = name,
                        Category = cat,
                        Qty = qty,
                        Revenue = Math.Round(revenue, 0)
                    });
                }

                // 3) 分類彙總
                await rd.NextResultAsync();
                while (await rd.ReadAsync())
                {
                    var cat = rd.GetString(rd.GetOrdinal("Category"));
                    var qty = rd.IsDBNull(rd.GetOrdinal("Qty")) ? 0 : rd.GetInt32(rd.GetOrdinal("Qty"));
                    var revenue = rd.IsDBNull(rd.GetOrdinal("Revenue")) ? 0m : rd.GetDecimal(rd.GetOrdinal("Revenue"));
                    catRows.Add((cat, qty, revenue));
                }
            }

            // 依總營收計算 Top 產品的占比
            var totalRev = catRows.Sum(x => x.Revenue);
            var topProductsWithShare = topProducts
                .Cast<dynamic>()
                .Select((x, i) => new
                {
                    Rank = i + 1,
                    Sku = (string)x.Sku,
                    Name = (string)x.Name,
                    Category = (string)x.Category,
                    Qty = (int)x.Qty,
                    Revenue = (decimal)x.Revenue,
                    Share = totalRev == 0 ? 0m : Math.Round(((decimal)x.Revenue / totalRev) * 100m, 1)
                })
                .ToList();

            var catLabels = catRows.Select(x => x.Category).ToArray();
            var catQty = catRows.Select(x => x.Qty).ToArray();
            var catRev = catRows.Select(x => Math.Round(x.Revenue, 0)).ToArray();

            return Ok(new
            {
                days,
                top,
                kpi,
                topProducts = topProductsWithShare,
                categories = new
                {
                    labels = catLabels,
                    qty = catQty,
                    revenue = catRev
                }
            });
        }
    }
}
