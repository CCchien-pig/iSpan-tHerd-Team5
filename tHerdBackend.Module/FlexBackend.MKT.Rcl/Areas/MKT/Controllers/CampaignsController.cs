using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlexBackend.MKT.Rcl.Areas.MKT.Utils;

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

        // GET: MKT/Campaigns/Index
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: MKT/Campaigns/CreateCampaign
        [HttpGet]
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

        // GET: 取得活動事件
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var campaigns = await _context.MktCampaigns
                .AsNoTracking()
                .ToListAsync();

            var events = campaigns.Select(c => new
            {
                id = c.CampaignId,
                title = c.CampaignName,
                start = c.StartDate,
                end = c.EndDate.HasValue ? c.EndDate : null,
                color = ColorHelper.RandomColor()
            }).ToList();

            return Ok(events);
        }

        // GET: 取得單筆活動
        [HttpGet]
        public async Task<IActionResult> GetEventById(int id)
        {
            var campaign = await _context.MktCampaigns
                .AsNoTracking()
                .Where(c => c.CampaignId == id)
                .Select(c => new
                {
                    id = c.CampaignId,
                    title = c.CampaignName,
                    type = c.CampaignType,
                    description = c.CampaignDescription,
                    productType = c.ProductType,
                    status = c.Status,
                    startDate = c.StartDate,
                    endDate = c.EndDate,
                    ingId = c.ImgId,
                    isActive = c.IsActive,
                    creator = c.Creator,
                    createDate = c.CreatedDate,
                    reviser = c.Reviser,
                    revisedDate = c.RevisedDate
                })
                .FirstOrDefaultAsync();

            if (campaign == null)
                return NotFound();

            return Ok(campaign);
        }

        // POST: 刪除活動
        [HttpPost]
        public IActionResult DeleteCampaign(int id)
        {
            var campaign = _context.MktCampaigns.Find(id);
            if (campaign == null)
                return Json(new { success = false, message = "找不到活動" });

            _context.MktCampaigns.Remove(campaign);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        // GET: 取得活動總數
        [HttpGet]
        public IActionResult GetTotalCount()
        {
            var count = _context.MktCampaigns.Count();
            return Json(new { count });
        }
    }
}