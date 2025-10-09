using tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels;
using tHerdBackend.Infra; // 你的 DbContext
using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using X.PagedList.Extensions; // 分頁套件

namespace tHerdBackend.CNT.Rcl.Areas.CNT.Controllers
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
		public IActionResult Index(string keyword, string status, int? page, int pageSize = 10)
		{
			var query = _db.CntTags.AsQueryable();

			// 關鍵字搜尋
			if (!string.IsNullOrEmpty(keyword))
			{
				query = query.Where(t => t.TagName.Contains(keyword));
			}

			// 狀態篩選
			if (!string.IsNullOrEmpty(status))
			{
				if (status == "active")
					query = query.Where(t => t.IsActive);
				else if (status == "inactive")
					query = query.Where(t => !t.IsActive);
			}

			// 分頁處理
			int pageNumber = page ?? 1;
			var pagedList = query
				.OrderByDescending(t => t.TagId)
				.Select(t => new TagListVM
				{
					TagId = t.TagId,
					TagName = t.TagName,
					IsActive = t.IsActive,
					Revisor = t.Revisor,
					RevisedDate = t.RevisedDate
				})
				.ToPagedList(pageNumber, pageSize);

			// 傳遞給 View
			ViewBag.Keyword = keyword;
			ViewBag.PageSize = pageSize;
			ViewBag.Status = status;

			// ⭐ 顯示中文狀態名稱
			ViewBag.StatusName = status switch
			{
				"active" => "啟用",
				"inactive" => "停用",
				_ => ""
			};

			return View(pagedList);
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

			// 檢查是否已存在相同名稱（不區分大小寫）
			bool exists = _db.CntTags
							 .Any(t => t.TagName == model.TagName);
			if (exists)
			{
				ModelState.AddModelError("TagName", "已存在相同名稱的標籤");
				return View(model);
			}

			var tag = new CntTag
			{
				TagName = model.TagName,
				IsActive = model.IsActive,
				Revisor = "Admin",
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

			bool exists = _db.CntTags
							 .Any(t => t.TagName == model.TagName && t.TagId != model.TagId);
			if (exists)
			{
				ModelState.AddModelError("TagName", "已存在相同名稱的標籤");
				return View(model);
			}

			var tag = _db.CntTags.Find(model.TagId);
			if (tag == null) return NotFound();

			tag.TagName = model.TagName;
			tag.IsActive = model.IsActive;
			tag.Revisor = "Admin";
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
		public IActionResult DeleteConfirmed(int tagId)
		{
			var tag = _db.CntTags.Find(tagId);
			if (tag == null) return NotFound();

			bool hasBound = _db.CntPageTags.Any(pt => pt.TagId == tagId);
			if (hasBound)
			{
				var vm = new TagDeleteVM
				{
					TagId = tag.TagId,
					TagName = tag.TagName,
					IsActive = tag.IsActive,
					BoundPages = _db.CntPageTags
									.Where(pt => pt.TagId == tagId)
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
