using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.ViewComponents
{
	public class PageBlockListViewComponent : ViewComponent
	{
		private readonly tHerdDBContext _db;

		public PageBlockListViewComponent(tHerdDBContext db)
		{
			_db = db;
		}

		public async Task<IViewComponentResult> InvokeAsync(int pageId)
		{
			var blocks = await _db.CntPageBlocks
				.Where(b => b.PageId == pageId)
				.OrderBy(b => b.OrderSeq)
				.ToListAsync();

			ViewBag.PageId = pageId; // 讓 _BlockList.cshtml 能拿到
			return View("~/Areas/CNT/Views/PageBlocks/_BlockList.cshtml", blocks);
		}

	}
}
