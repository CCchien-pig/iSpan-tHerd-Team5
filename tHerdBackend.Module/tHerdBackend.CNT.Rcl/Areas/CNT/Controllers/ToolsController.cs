using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.Controllers
{
	public class ToolsController : Controller
	{
		private readonly tHerdDBContext _db;

		public ToolsController(tHerdDBContext db)
		{
			_db = db;
		}

		// GET: /CNT/Tools/MoveBlocks
		public IActionResult MoveBlocks()
		{
			return View();
		}

		// POST: /CNT/Tools/MoveBlocks
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult MoveBlocks(int fromPageId, int toPageId)
		{
			if (fromPageId == toPageId)
			{
				TempData["Msg"] = "來源與目標不能相同";
				return RedirectToAction(nameof(MoveBlocks));
			}

			var blocks = _db.CntPageBlocks.Where(b => b.PageId == fromPageId).ToList();

			if (!blocks.Any())
			{
				TempData["Msg"] = "來源頁沒有 Blocks";
				return RedirectToAction(nameof(MoveBlocks));
			}

			foreach (var block in blocks)
			{
				block.PageId = toPageId;
			}

			_db.SaveChanges();
			TempData["Msg"] = $"已將 {blocks.Count} 個 Blocks 從 Page {fromPageId} 轉移到 Page {toPageId}";
			return RedirectToAction(nameof(MoveBlocks));
		}
	}
}
