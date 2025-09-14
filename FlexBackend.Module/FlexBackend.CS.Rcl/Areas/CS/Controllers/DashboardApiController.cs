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

            // 依天彙總淨額與訂單數（欄位若為 nvarchar 用 TRY_CONVERT 避免拋例外）
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
                    var netVal = rd.IsDBNull(rd.GetOrdinal("Net")) ? 0m : rd.GetDecimal(rd.GetOrdinal("Net"));
                    var orderCnt = rd.IsDBNull(rd.GetOrdinal("Orders")) ? 0 : rd.GetInt32(rd.GetOrdinal("Orders"));
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
        /// Top 熱銷與品類彙總（目前不依賴分類表，皆以「未分類」回傳）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Tops([FromQuery] int days = 30, [FromQuery] int top = 10)
        {
            if (days < 1) days = 1;
            if (top < 1) top = 10;

            var end = DateTime.Today;
            var start = end.AddDays(-(days - 1));
            var endPlusOne = end.AddDays(1);

            const string sql = @"
DECLARE @start datetime2 = @p_start;
DECLARE @endPlusOne datetime2 = @p_endPlusOne;
DECLARE @top int = @p_top;

-- 有效訂單（付款完成）
SELECT o.OrderId
INTO #valid_orders
FROM dbo.ORD_Order o
WHERE o.PaymentStatus = 'paid'
  AND o.CreatedDate >= @start
  AND o.CreatedDate <  @endPlusOne;

-- 每筆 OrderItem 的調整金額彙總
SELECT oi.OrderItemId,
       SUM(COALESCE(TRY_CONVERT(decimal(20,2), oia.Amount), 0)) AS AdjAmount
INTO #item_adj
FROM dbo.ORD_OrderItem oi
LEFT JOIN dbo.ORD_OrderItemAdjustment oia ON oia.OrderItemId = oi.OrderItemId
GROUP BY oi.OrderItemId;

-- 1) KPI
SELECT
    COUNT(DISTINCT vo.OrderId) AS TotalOrders,
    SUM(oi.Qty)               AS UnitsSold,
    SUM(
        COALESCE(TRY_CONVERT(decimal(20,2), oi.UnitPrice), 0) * oi.Qty
      + COALESCE(ia.AdjAmount, 0)
    ) AS TotalRevenue
FROM dbo.ORD_OrderItem oi
JOIN #valid_orders vo ON vo.OrderId = oi.OrderId
LEFT JOIN #item_adj ia ON ia.OrderItemId = oi.OrderItemId;

-- 2) Top 產品（先固定為未分類）
SELECT TOP (@top)
    p.ProductId,
    p.ProductCode,
    p.ProductName,
    N'未分類' AS Category,
    SUM(oi.Qty) AS Qty,
    SUM(
        COALESCE(TRY_CONVERT(decimal(20,2), oi.UnitPrice), 0) * oi.Qty
      + COALESCE(ia.AdjAmount, 0)
    ) AS Revenue
FROM dbo.ORD_OrderItem oi
JOIN #valid_orders vo ON vo.OrderId = oi.OrderId
LEFT JOIN #item_adj ia ON ia.OrderItemId = oi.OrderItemId
LEFT JOIN dbo.PROD_Product p ON p.ProductId = oi.ProductId
GROUP BY p.ProductId, p.ProductCode, p.ProductName
ORDER BY Revenue DESC;

-- 3) 品類彙總（皆視為未分類；此段不需要 GROUP BY，避免 164 錯誤）
SELECT
    N'未分類' AS Category,
    SUM(oi.Qty) AS Qty,
    SUM(
        COALESCE(TRY_CONVERT(decimal(20,2), oi.UnitPrice), 0) * oi.Qty
      + COALESCE(ia.AdjAmount, 0)
    ) AS Revenue
FROM dbo.ORD_OrderItem oi
JOIN #valid_orders vo ON vo.OrderId = oi.OrderId
LEFT JOIN #item_adj ia ON ia.OrderItemId = oi.OrderItemId;

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
                    var name = rd.IsDBNull(rd.GetOrdinal("ProductName")) ? "" : rd.GetString(rd.GetOrdinal("ProductName"));
                    var cat = rd.GetString(rd.GetOrdinal("Category")); // 未分類
                    var qty = rd.IsDBNull(rd.GetOrdinal("Qty")) ? 0 : rd.GetInt32(rd.GetOrdinal("Qty"));
                    var revenue = rd.IsDBNull(rd.GetOrdinal("Revenue")) ? 0m : rd.GetDecimal(rd.GetOrdinal("Revenue"));

                    topProducts.Add(new
                    {
                        Sku = code,
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

                var totalRev = catRows.Sum(x => x.Revenue);
                var topProductsWithShare = topProducts
                    .Select((x, i) =>
                    {
                        dynamic d = x;
                        decimal rev = d.Revenue;
                        return new
                        {
                            Rank = i + 1,
                            Sku = (string)d.Sku,
                            Name = (string)d.Name,
                            Category = (string)d.Category,
                            Qty = (int)d.Qty,
                            Revenue = rev,
                            Share = totalRev == 0 ? 0m : Math.Round(rev / totalRev * 100m, 1)
                        };
                    })
                    .ToList();

                return Ok(new
                {
                    days,
                    top,
                    kpi,
                    topProducts = topProductsWithShare,
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
                // 開發期保留，方便排查 500；上線可移除
                return Problem(title: "Tops failed", detail: ex.ToString(), statusCode: 500);
            }
        }
    }
}
