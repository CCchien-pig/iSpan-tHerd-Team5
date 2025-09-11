using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace therdPractice01.Controllers
{
    public class MKTCampaignController : Controller
    {
        private readonly THerdDBContext _context;

        public MKTCampaignController (THerdDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Test()
        {
            return View();
        }

        [HttpGet]
        public IActionResult TestEvent() 
        { 
            return View();
        }

        [HttpPost]
        public IActionResult TestAddActive(MktCampaign model)
        {
            try
            {
                // 驗證 ModelState
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                  .Select(e => e.ErrorMessage)
                                                  .ToList();
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                model.CreatedDate = DateTime.Now;
                _context.MktCampaigns.Add(model);
                _context.SaveChanges();

                // 成功回傳 JSON
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult TestCoupon()
        {
            return View();
        }

        [HttpGet]
        public IActionResult TestAd()
        {
            return View();
        }

    }
}
