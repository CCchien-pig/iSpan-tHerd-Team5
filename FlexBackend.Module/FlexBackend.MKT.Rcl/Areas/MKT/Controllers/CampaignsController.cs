using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // GET: Campaigns/Index
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: Campaigns/CreatCampaign
        [HttpGet]
        public IActionResult CreatCampaign()
        {
            return View();
        }

        // POST: Campaigns/CreatCampaign
        [HttpPost]
        public IActionResult CreatCampaign([FromBody] MktCampaign model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                  .Select(e => e.ErrorMessage)
                                                  .ToList();
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                // 將前端多選 ProductType 字串直接存入資料庫
                model.CreatedDate = DateTime.Now;

                _context.MktCampaigns.Add(model);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (DbUpdateException dbEx)
            {
                // 回傳 inner exception 訊息方便 debug
                return Json(new { success = false, message = dbEx.InnerException?.Message ?? dbEx.Message });
            }
        }
    }
}
