using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using tHerdBackend.Core.Interfaces.SYS;
using tHerdBackend.Infra.DBSetting;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.SharedApi.Controllers.Module.MKT
{
    [ApiController]
    [Route("api/mkt/[controller]")]
    public class NewsletterController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly tHerdDBContext _db;
        private readonly ISysAssetFileService _fileService;

        public NewsletterController(IEmailSender emailSender, tHerdDBContext db, ISysAssetFileService fileService)
        {
            _emailSender = emailSender;
            _db = db;
            _fileService = fileService;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email))
                return BadRequest(new { ok = false, message = "Email 不可為空" });

            // ✅ 撈出目前最新上架的廣告
            var ad = await _db.MktAds
                .Where(a => a.IsActive && a.AdType == "Carousel" && a.Status == "aActive" && a.StartDate <= DateTime.Now &&
                            (a.EndDate == null || a.EndDate >= DateTime.Now))
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefaultAsync();

            if (ad == null)
                return Ok(new { ok = false, message = "目前沒有廣告內容可寄送" });

            // ✅ 取得圖片 URL
            string imageUrl = string.Empty;
            if (ad.ImgId.HasValue)
            {
                var file = await _fileService.GetFileById(ad.ImgId.Value);
                if (file != null && !string.IsNullOrEmpty(file.FileUrl))
                {
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    imageUrl = $"<img src='{(file.FileUrl.StartsWith("http") ? file.FileUrl : baseUrl + file.FileUrl)}' alt='{ad.Title}' style='display:block;max-width:100%;border-radius:12px;margin-top:12px;'/>";
                }
            }

            // ✅ 加入按鈕（如果有設定）
            string buttonHtml = string.Empty;
            if (!string.IsNullOrEmpty(ad.ButtonText) && !string.IsNullOrEmpty(ad.ButtonLink))
            {
                buttonHtml = $@"
                <div style='margin-top:20px;'>
                    <a href='{ad.ButtonLink}' target='_blank' 
                       style='display:inline-block;background-color:#007083;color:white;
                              text-decoration:none;padding:10px 20px;border-radius:8px;'>
                        {ad.ButtonText}
                    </a>
                </div>";
            }

            
            // ✅ 組信件 HTML 內容（tHerd 品牌風格）
            var subject = ad.Title;
            

            var html = $@"
<div style='background-color:rgb(0,112,131); padding:40px 0; font-family:Arial, Helvetica, sans-serif;'>
  <div style='background-color:#ffffff; max-width:680px; margin:0 auto; border-radius:16px; overflow:hidden; box-shadow:0 6px 18px rgba(0,0,0,0.15);'>

    
    <!-- 標題 -->
    <div style='padding:16px 40px 0 40px; text-align:center;'>
      <h1 style='color:rgb(0,112,131); font-size:28px; margin-bottom:12px;'>{ad.Title}</h1>
    </div>

    <!-- 內文 -->
    <div style='padding:0 40px 20px 40px; text-align:center;'>
      <p style='font-size:18px; line-height:1.8; color:#333333; margin-bottom:20px;'>
        {ad.Content}
      </p>
    </div>

    <!-- 廣告圖片 -->
    <div style='text-align:center; padding:0 20px 24px 20px;'>
      {imageUrl}
    </div>


    <!-- 分隔線 -->
    <hr style='margin:0 40px; border:0; border-top:1px solid #ddd;' />

    <!-- Footer -->
    <div style='text-align:center; padding:20px 40px 30px 40px; font-size:13px; color:#777;'>
      <p>此信件由 <strong style='color:rgb(0,112,131);'>tHerd</strong> 自動寄出，請勿直接回覆。</p>
      <p style='margin-top:6px;'>© 2025 tHerd, All rights reserved.</p>
    </div>

  </div>
</div>";


            // ✅ 寄出信件
            await _emailSender.SendEmailAsync(dto.Email, subject, html);

            return Ok(new { ok = true, message = "電子報已寄出，請查收信箱。" });
        }

        public class SubscribeDto
        {
            public string Email { get; set; }
        }
    }
}
