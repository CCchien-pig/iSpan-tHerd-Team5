using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tHerdBackend.Core.Interfaces.CS;

namespace tHerdBackend.CS.Rcl.Areas.CS.Controllers
{
	[Area("CS")]
	[Authorize] 
	public class CsTicketsController : Controller
	{
		private readonly ICsTicketService _service;

		public CsTicketsController(ICsTicketService service)
		{
			_service = service;
		}

		// =====================================================
		// 🟩 1️⃣ 工單清單頁
		// =====================================================
		public async Task<IActionResult> Index()
		{
			var tickets = await _service.GetAllAsync();
			return View(tickets);
		}

		// =====================================================
		// 🟩 2️⃣ 單筆詳情頁（含Email、UserId、留言）
		// =====================================================
		public async Task<IActionResult> Details(int id)
		{
			var ticket = await _service.GetTicketByIdAsync(id);

			if (ticket == null)
				return NotFound("找不到該工單");

			return View(ticket);
		}

		// =====================================================
		// 🟩 3️⃣ 回覆信件（表單版）
		// =====================================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Reply(int ticketId, string replyText)
		{
			if (string.IsNullOrWhiteSpace(replyText))
			{
				TempData["Error"] = "回覆內容不可為空白";
				return RedirectToAction("Details", new { id = ticketId });
			}

			await _service.AddReplyAsync(ticketId, replyText);
			TempData["Success"] = "回覆已寄出！";

			return RedirectToAction("Details", new { id = ticketId });
		}

		// =====================================================
		// 🟩 4️⃣ 回覆信件（Ajax 版，前端 fetch 使用）
		// =====================================================
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
					message = "回覆信件已寄出！",
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
