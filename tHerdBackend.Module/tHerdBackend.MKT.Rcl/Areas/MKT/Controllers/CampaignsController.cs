using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.MKT.Rcl.Areas.MKT.Utils;

namespace tHerdBackend.MKT.Rcl.Areas.MKT.Controllers
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

        [HttpPost]
        public IActionResult CreateCampaign([FromBody]MktCampaign model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            // 檢查開始與結束時間
            if (model.EndDate.HasValue && model.StartDate > model.EndDate.Value)
            {
                return Json(new { success = false, message = "開始時間不可晚於結束時間" });
            }
            if (string.IsNullOrWhiteSpace(model.CampaignType))
            {
                model.CampaignType = "cmd"; // 預設值
            }
            model.CreatedDate = DateTime.Now;
            _context.MktCampaigns.Add(model);
            

            try
            {
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // 這裡可以針對資料庫例外再做處理
                var inner = ex;
                while (inner.InnerException != null) inner = inner.InnerException;
                return Json(new { success = false, message = "新增失敗：" + inner.Message });
            }

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
                color = c.IsActive ? ColorHelper.RandomColor() : "#9e9e9e"
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
                    imgId = c.ImgId,
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

        [HttpPost]
        public IActionResult UpdateCampaign([FromBody] MktCampaign model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "資料驗證失敗" });

            // 檢查開始與結束時間
            if (model.EndDate.HasValue && model.StartDate > model.EndDate.Value)
            {
                return Json(new { success = false, message = "開始時間不可晚於結束時間" });
            }

            // 強制固定活動類型
            model.CampaignType = "cmd";

            // 更新時間
            model.RevisedDate = DateTime.Now;

            try
            {
                _context.MktCampaigns.Update(model);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                var inner = ex;
                while (inner.InnerException != null) inner = inner.InnerException;
                return Json(new { success = false, message = "更新失敗：" + inner.Message });
            }
        }
    }
}