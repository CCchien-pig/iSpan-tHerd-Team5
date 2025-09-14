using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.CS.Rcl.Areas.CS.Controllers
{
	[Area("CS")]
	public class DashboardController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
    [ApiController]
    [Route("api/cs/dashboard")] // => /api/cs/dashboard/overview
    public class DashboardApiController : ControllerBase
    {
        [HttpGet("overview")]
        public IActionResult Overview([FromQuery] int days = 30)
        {
            var rnd = new Random();
            var today = DateTime.Today;
            var labels = Enumerable.Range(0, days)
                .Select(i => today.AddDays(-(days - 1 - i)).ToString("yyyy-MM-dd")) // 給前端轉 zh-TW 顯示
                .ToArray();

            // 以基準值 + 少量波動 模擬：每日營收 / 訂單數
            decimal baseRevenue = 28000m; // 每日大約 2.8 萬
            int baseOrders = 80;

            var net = labels.Select(_ =>
            {
                var jitter = (decimal)rnd.Next(-3000, 3000);    // ±3,000 的波動
                return Math.Max(0, baseRevenue + jitter);
            }).ToArray();

            var orders = labels.Select(_ =>
            {
                var jitter = rnd.Next(-20, 20);
                return Math.Max(0, baseOrders + jitter);
            }).ToArray();

            // KPI（用近 30 天推估）
            var monthly = net.Sum();                 // 近 N 天總額，若 N=30 就是「本月」
            var annual = monthly * 12m;             // 粗估年營收
            var task = rnd.Next(35, 85);          // 完成率
            var pending = rnd.Next(5, 40);           // 待處理數

            // 收入來源比例（加總 100）
            var direct = rnd.Next(45, 65);
            var social = rnd.Next(15, 35);
            var referral = 100 - direct - social;

            return Ok(new
            {
                labels,
                net,
                orders,
                channelShare = new { Direct = direct, Social = social, Referral = referral },
                kpi = new { monthly, annual, task, pending }
            });
        }
        [HttpGet("tops")]
        public IActionResult Tops([FromQuery] int days = 30, [FromQuery] int top = 10)
        {
            // === 範例：用隨機資料模擬（要接真實資料時，把下方隨機邏輯換成資料庫彙總）===
            var rnd = new Random();

            // 商品樣本（實務上請從 DB 取出）
            var sampleProducts = new[]
            {
                new {Sku="A0001", Name="超能量貓糧 2kg", Category="寵物"},
                new {Sku="A0002", Name="純棉T-shirt", Category="服飾"},
                new {Sku="A0003", Name="保溫杯 500ml", Category="日用"},
                new {Sku="A0004", Name="高纖穀物麥片", Category="食品"},
                new {Sku="A0005", Name="魚油膠囊 120 粒", Category="保健"},
                new {Sku="A0006", Name="無線滑鼠", Category="3C"},
                new {Sku="A0007", Name="真無線藍牙耳機", Category="3C"},
                new {Sku="A0008", Name="運動短褲", Category="服飾"},
                new {Sku="A0009", Name="即溶咖啡 60 包", Category="食品"},
                new {Sku="A0010", Name="清潔濕紙巾", Category="日用"},
                new {Sku="A0011", Name="益生菌 30 包", Category="保健"},
                new {Sku="A0012", Name="寵物潔牙棒", Category="寵物"},
            };

            // 模擬最近 N 天銷售：為每個商品隨機出貨量與含稅營收
            var productSales = sampleProducts
                .Select(p =>
                {
                    var qty = rnd.Next(50, 1200);               // 總售出數量（近 N 天）
                    var price = rnd.Next(150, 2500);            // 假設平均單價
                    var rev = (decimal)qty * price;             // 營收
                    return new
                    {
                        p.Sku,
                        p.Name,
                        p.Category,
                        Qty = qty,
                        Revenue = rev
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // KPI
            var totalRevenue = productSales.Sum(x => x.Revenue);
            var unitsSold = productSales.Sum(x => x.Qty);
            var totalOrders = (int)Math.Max(1, unitsSold * 0.6);      // 模擬訂單數（可依你的商業規則）
            var aov = totalRevenue / Math.Max(1, totalOrders); // 平均客單價

            // 前 N 名商品
            var topProducts = productSales
                .Take(top)
                .Select((x, i) => new
                {
                    Rank = i + 1,
                    x.Sku,
                    x.Name,
                    x.Category,
                    Qty = x.Qty,
                    Revenue = Math.Round(x.Revenue, 0),
                    Share = totalRevenue == 0 ? 0 : Math.Round((x.Revenue / totalRevenue) * 100, 1)
                })
                .ToList();

            // 品類彙總
            var catAgg = productSales
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Qty = g.Sum(x => x.Qty),
                    Revenue = g.Sum(x => x.Revenue)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            return Ok(new
            {
                days,
                top,
                kpi = new
                {
                    totalOrders,
                    unitsSold,
                    aov = Math.Round(aov, 0),
                    totalRevenue = Math.Round(totalRevenue, 0)
                },
                topProducts,
                categories = new
                {
                    labels = catAgg.Select(x => x.Category).ToArray(),
                    qty = catAgg.Select(x => x.Qty).ToArray(),
                    revenue = catAgg.Select(x => Math.Round(x.Revenue, 0)).ToArray()
                }
            });
        }
    }
}


