using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.CS.Rcl.Areas.CS.Controllers
{
    [Area("CS")]
    public class DashboardController : Controller
    {
        // 只負責回傳 Razor View（/Areas/CS/Views/Dashboard/Index.cshtml）
        public IActionResult Index() => View();
    }
}
