using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.MKT.Rcl.Areas.MKT.Controllers
{
    [Area("MKT")]
    public class CampaignsController : Controller
    {
        private readonly tHerdDBContext _context;

        public CampaignsController(tHerdDBContext context)
        {
            _context = context;
        }

        // GET: Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: 新增活動頁面
        public IActionResult CreateCampaign()
        {
            return View();
        }

        // POST: 新增活動
        [HttpPost]
        public IActionResult CreateCampaign([FromBody] MktCampaign model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            model.CreatedDate = DateTime.Now;
            _context.MktCampaigns.Add(model);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }

}
