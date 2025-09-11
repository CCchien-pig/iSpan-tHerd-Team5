using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using X.PagedList.Extensions;



namespace FlexBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[Area("CNT")]
	public class PagesController : Controller
	{
		private readonly tHerdDBContext _db;
		public PagesController(tHerdDBContext db)
		{
			_db = db;
		}
		// GET: PageController
		public IActionResult Index(int? page)
		{
			int pageNumber = page ?? 1;   // 預設第 1 頁
			int pageSize = 6;            // 每頁顯示 6 筆

			var pages = _db.CntPages
			   .OrderByDescending(p => p.CreatedDate)
			   .Select(p => new PageListVM
			   {
				   PageId = p.PageId,
				   Title = p.Title,
				   Status = p.Status,        // 只傳 Status
				   CreatedDate = p.CreatedDate
			   });


			return View(pages.ToPagedList(pageNumber, pageSize));
		}


		// GET: PageController/Details/5
		public ActionResult Details(int id)
		{
			return View();
		}

		// GET: PageController/Create
		public ActionResult Create()
		{
			return View();
		}

		// POST: PageController/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(IFormCollection collection)
		{
			try
			{
				return RedirectToAction(nameof(Index));
			}
			catch
			{
				return View();
			}
		}

		// GET: PageController/Edit/5
		public ActionResult Edit(int id)
		{
			return View();
		}

		// POST: PageController/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(int id, IFormCollection collection)
		{
			try
			{
				return RedirectToAction(nameof(Index));
			}
			catch
			{
				return View();
			}
		}

		// GET: PageController/Delete/5
		public ActionResult Delete(int id)
		{
			return View();
		}

		// POST: PageController/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Delete(int id, IFormCollection collection)
		{
			try
			{
				return RedirectToAction(nameof(Index));
			}
			catch
			{
				return View();
			}
		}
	}
}
