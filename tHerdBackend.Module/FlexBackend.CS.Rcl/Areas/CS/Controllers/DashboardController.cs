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
            var labels = Enumerable.Range(0, days)
                .Reverse()
                .Select(i => DateTime.Today.AddDays(-i).ToString("yyyy-MM-dd"))
                .ToList();

            var rnd = new Random();
            var net = new List<int>();
            var orders = new List<int>();
            double baseVal = 25000 + rnd.Next(0, 20000);

            foreach (var _ in labels)
            {
                baseVal *= 1 + ((rnd.NextDouble() - 0.48) * 0.02);
                net.Add((int)Math.Round(baseVal + rnd.Next(0, 5000)));
                orders.Add(rnd.Next(40, 150));
            }

            int direct = rnd.Next(40, 70);
            int social = rnd.Next(10, 30);
            int referral = Math.Max(0, 100 - direct - social);

            int monthly = net.Sum();
            int annual = (int)Math.Round(monthly * 12 * (0.9 + rnd.NextDouble() * 0.2));
            int task = rnd.Next(30, 90);
            int pending = rnd.Next(8, 30);

            return Ok(new
            {
                labels,
                net,
                orders,
                channelShare = new { Direct = direct, Social = social, Referral = referral },
                kpi = new { monthly, annual, task, pending }
            });
        }
    }
}

