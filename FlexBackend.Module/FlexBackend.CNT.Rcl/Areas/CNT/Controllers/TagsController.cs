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
				RevisedDate = DateTime.UtcNow
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
			tag.RevisedDate = DateTime.UtcNow;

			_db.SaveChanges();

			TempData["Msg"] = "標籤更新成功";
			return RedirectToAction(nameof(Index));
		}
	}
}
