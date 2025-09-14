using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.CNT.Rcl.Areas.CNT.Controllers
{
	public class PageBlocksController : Controller
	{
		// GET: PageBlocksController
		public ActionResult Index()
		{
			return View();
		}

		// GET: PageBlocksController/Details/5
		public ActionResult Details(int id)
		{
			return View();
		}

		// GET: PageBlocksController/Create
		public ActionResult Create()
		{
			return View();
		}

		// POST: PageBlocksController/Create
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

		// GET: PageBlocksController/Edit/5
		public ActionResult Edit(int id)
		{
			return View();
		}

		// POST: PageBlocksController/Edit/5
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

		// GET: PageBlocksController/Delete/5
		public ActionResult Delete(int id)
		{
			return View();
		}

		// POST: PageBlocksController/Delete/5
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
