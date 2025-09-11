using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
		public IActionResult Index()
		{
			var pages = _db.CntPages
				.OrderByDescending(p => p.CreatedDate)
				.Select(p => new PageListVM
				{
					PageId = p.PageId,
					Title = p.Title,
					Status = p.Status,
					CreatedDate = p.CreatedDate
				})
				.ToList();

			return View(pages);
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
