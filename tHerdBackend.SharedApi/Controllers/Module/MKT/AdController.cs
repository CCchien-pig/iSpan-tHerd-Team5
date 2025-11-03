using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SharedApi.Controllers.Module.MKT
{
    [Route("api/mkt/[controller]")]
    [ApiController]
    public class AdController : ControllerBase
    {
        private readonly tHerdDBContext _db;

        public AdController(tHerdDBContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 取得前台輪播圖廣告清單（AdType=Carousel, 狀態為上架且在有效日期內）
        /// </summary>
        [HttpGet("CarouselList")]
        public async Task<IActionResult> GetCarouselList()
        {
            var today = DateTime.Now;

            var ads = await _db.MktAds
                .Include(a => a.Img)
                .Where(a => a.AdType == "Carousel"
                            && a.IsActive == true
                            && a.Status == "aActive"
                            && a.StartDate <= today
                            && (a.EndDate == null || a.EndDate >= today))
                .Select(a => new
                {
                    id = a.AdId,
                    title = a.Title,
                    description = a.Content,
                    buttonText = a.ButtonText,
                    image = a.Img != null ? a.Img.FileUrl : null,
                    link = a.ButtonLink
                })
                .OrderBy(a => a.id)
                .ToListAsync();

            return Ok(ads);
        }

        [HttpGet("PopupList")]
        public async Task<IActionResult> GetPopupList()
        {
            var today = DateTime.Now;

            var ads = await _db.MktAds
                .Include(a => a.Img)
                .Where(a => a.AdType == "Popup"
                            && a.IsActive == true
                            && a.Status == "aActive"
                            && a.StartDate <= today
                            && (a.EndDate == null || a.EndDate >= today))
                .Select(a => new
                {
                    id = a.AdId,
                    imageUrl = a.Img != null ? a.Img.FileUrl : null,
                    link = a.ButtonLink
                })
                .OrderBy(a => a.id)
                .ToListAsync();

            return Ok(ads);
        }

        [HttpGet("MarqueeList")]
        public async Task<IActionResult> GetMarqueeList()
        {
            var today = DateTime.Now;

            var ads = await _db.MktAds
                .Where(a => a.AdType == "Marquee"
                            && a.IsActive == true
                            && a.Status == "aActive"
                            && a.StartDate <= today
                            && (a.EndDate == null || a.EndDate >= today))
                .Select(a => new
                {
                    id = a.AdId,
                    title = a.Title ?? "(未命名公告)",
                    description = a.Content ?? ""
                })
                .OrderBy(a => a.id)
                .ToListAsync();

            return Ok(ads);
        }

    }
}
