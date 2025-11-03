using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Interfaces.CS;

namespace tHerdBackend.CS.Rcl.Areas.CS.Controllers
{
    [Area("CS")]
    public class CsTicketsController : Controller
    {
        private readonly ICsTicketService _service;

        public CsTicketsController(ICsTicketService service)
        {
            _service = service;
        }

        // ✅ 顯示工單清單
        public async Task<IActionResult> Index()
        {
            var tickets = await _service.GetAllAsync();
            return View(tickets);
        }

        // ✅ 查看詳情 + 回覆
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _service.GetTicketByIdAsync(id);
            return View(ticket);
        }

        // ✅ 提交回覆
        [HttpPost]
        public async Task<IActionResult> Reply(int ticketId, string replyText)
        {
            await _service.AddReplyAsync(ticketId, replyText);
            return RedirectToAction("Detail", new { id = ticketId });
        }

		[HttpPost]
	
		public async Task<IActionResult> ReplyAjax(int ticketId, string replyText)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(replyText))
					return Json(new { ok = false, message = "回覆內容不可為空白" });

				await _service.AddReplyAsync(ticketId, replyText);

				return Json(new
				{
					ok = true,
					message = "回覆已寄出",
					redirectUrl = Url.Action("Index", "CsTickets", new { area = "CS" })
				});
			}
			catch (Exception ex)
			{
				return Json(new { ok = false, message = "寄信失敗：" + ex.Message });
			}
		}



	}
}
