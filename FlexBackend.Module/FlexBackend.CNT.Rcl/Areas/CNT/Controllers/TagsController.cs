using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.Infra; // 你的 DbContext
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[Area("CNT")]
	public class TagsController : Controller
	{
		private readonly tHerdDBContext _db;
		public TagsController(tHerdDBContext db)
		{
			_db = db;
		}

		// GET: CNT/Tags
		public IActionResult Index()
		{
			var tags = _db.CntTags
				.OrderBy(t => t.TagId)
				.Select(t => new TagListVM
				{
					TagId = t.TagId,
					TagName = t.TagName,
					IsActive = t.IsActive,
					Revisor = t.Revisor,
					RevisedDate = t.RevisedDate
				})
				.ToList();

			return View(tags);
		}

		// GET: CNT/Tags/Create
		public IActionResult Create()
		{
			return View();
		}

		// POST: CNT/Tags/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(TagEditVM model)
		{
			if (!ModelState.IsValid) return View(model);

			var tag = new CntTag
			{
				TagName = model.TagName,
				IsActive = model.IsActive,
				Revisor = "Admin", // TODO: 從登入者帳號取得
				RevisedDate = DateTime.Now
			};

			_db.CntTags.Add(tag);
			_db.SaveChanges();

			TempData["Msg"] = "標籤新增成功";
			return RedirectToAction(nameof(Index));
		}

		// GET: CNT/Tags/Edit/5
		public IActionResult Edit(int id)
		{
			var tag = _db.CntTags.Find(id);
			if (tag == null) return NotFound();

			var vm = new TagEditVM
			{
				TagId = tag.TagId,
				TagName = tag.TagName,
				IsActive = tag.IsActive,
				Revisor = tag.Revisor
			};

			return View(vm);
		}

		// POST: CNT/Tags/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(TagEditVM model)
		{
			if (!ModelState.IsValid) return View(model);

			var tag = _db.CntTags.Find(model.TagId);
			if (tag == null) return NotFound();

			tag.TagName = model.TagName;
			tag.IsActive = model.IsActive;
			tag.Revisor = "Admin"; // TODO: 從登入者帳號取得
			tag.RevisedDate = DateTime.Now;

			_db.SaveChanges();

			TempData["Msg"] = "標籤更新成功";
			return RedirectToAction(nameof(Index));
		}

		//[HttpPost]
		//[ValidateAntiForgeryToken]
		//public IActionResult ToggleActive(int id, bool isActive)
		//{
		//	var tag = _db.CntTags.Find(id);
		//	if (tag == null)
		//	{
		//		return Json(new { success = false, message = "找不到標籤" });
		//	}

		//	tag.IsActive = isActive;
		//	tag.Revisor = User.Identity?.Name ?? "System";
		//	tag.RevisedDate = DateTime.Now;

		//	_db.SaveChanges();

		//	return Json(new { success = true, message = $"標籤「{tag.TagName}」已{(isActive ? "啟用" : "停用")}" });
		//}

		// GET: CNT/Tags/Details/5
		public IActionResult Details(int id)
		{
			var tag = _db.CntTags
				.Where(t => t.TagId == id)
				.Select(t => new TagDetailVM
				{
					TagId = t.TagId,
					TagName = t.TagName,
					IsActive = t.IsActive,
					Revisor = t.Revisor,
					RevisedDate = t.RevisedDate,
					BoundPages = _db.CntPageTags
									.Where(pt => pt.TagId == id)
									.Select(pt => pt.Page.Title)
									.ToList()
				})
				.FirstOrDefault();

			if (tag == null) return NotFound();

			return View(tag);
		}

		// GET: CNT/Tags/Delete/5
		public IActionResult Delete(int id)
		{
			var tag = _db.CntTags.FirstOrDefault(t => t.TagId == id);
			if (tag == null) return NotFound();

			var vm = new TagDeleteVM
			{
				TagId = tag.TagId,
				TagName = tag.TagName,
				IsActive = tag.IsActive,
				BoundPages = _db.CntPageTags
								.Where(pt => pt.TagId == id)
								.Select(pt => pt.Page.Title)
								.ToList()
			};

			return View(vm);
		}

		// POST: CNT/Tags/DeleteConfirmed/5
		[HttpPost, ActionName("DeleteConfirmed")]
		[ValidateAntiForgeryToken]
		public IActionResult DeleteConfirmed(int id)
		{
			var tag = _db.CntTags.Find(id);
			if (tag == null) return NotFound();

			bool hasBound = _db.CntPageTags.Any(pt => pt.TagId == id);
			if (hasBound)
			{
				var vm = new TagDeleteVM
				{
					TagId = tag.TagId,
					TagName = tag.TagName,
					IsActive = tag.IsActive,
					BoundPages = _db.CntPageTags
									.Where(pt => pt.TagId == id)
									.Select(pt => pt.Page.Title)
									.ToList()
				};
				ModelState.AddModelError("", "無法刪除：此標籤仍被文章使用");
				return View("Delete", vm);
			}

			_db.CntTags.Remove(tag);
			_db.SaveChanges();

			TempData["Msg"] = "標籤已刪除";
			return RedirectToAction(nameof(Index));
		}


	}
}
