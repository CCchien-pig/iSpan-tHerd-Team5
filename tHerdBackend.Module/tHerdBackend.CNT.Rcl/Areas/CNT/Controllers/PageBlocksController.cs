using tHerdBackend.CNT.Rcl.Areas.CNT.ViewModels;
using tHerdBackend.Infra.Models; // 依你的命名空間調整
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace tHerdBackend.CNT.Rcl.Areas.CNT.Controllers
{
	[Area("CNT")]
	public class PageBlocksController : Controller
	{
		private readonly tHerdDBContext _db;
		private readonly IWebHostEnvironment _env; // 若要暫存上傳檔案

		public PageBlocksController(tHerdDBContext db, IWebHostEnvironment env)
		{
			_db = db;
			_env = env;
		}

		// 供 Partial 使用的清單
		// GET: /CNT/PageBlocks/List?pageId=123
		public IActionResult List(int pageId)
		{
			var blocks = _db.CntPageBlocks
				.Where(b => b.PageId == pageId)
				.OrderBy(b => b.OrderSeq)
				.ToList();
			ViewBag.PageId = pageId;
			return PartialView("_BlockList", blocks);
		}

		// GET: /CNT/PageBlocks/Add?pageId=123
		public IActionResult Add(int pageId)
		{
			// 預設把 OrderSeq 放最後
			var maxOrder = _db.CntPageBlocks.Where(b => b.PageId == pageId).Select(b => (int?)b.OrderSeq).Max() ?? 0;
			var vm = new PageBlockEditVM
			{
				PageId = pageId,
				OrderSeq = maxOrder + 1,
				BlockType = "richtext"
			};
			return View(vm);
		}

		// POST: /CNT/PageBlocks/Add
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Add(PageBlockEditVM model, IFormFile? imageFile)
		{
			if (!_db.CntPages.Any(p => p.PageId == model.PageId))
			{
				ModelState.AddModelError("", "找不到對應文章 PageId");
			}

			// 驗證 BlockType
			var allowed = new[] { "richtext", "image", "video", "cta" };
			if (string.IsNullOrWhiteSpace(model.BlockType) || !allowed.Contains(model.BlockType))
			{
				ModelState.AddModelError(nameof(model.BlockType), "BlockType 必須是 richtext / image / video / cta");
			}

			if (!ModelState.IsValid) return View(model);

			string content = null;

			switch (model.BlockType)
			{
				case "richtext":
					content = model.NewBlockContent ?? model.Content;
					break;

				case "image":
					if (imageFile != null && imageFile.Length > 0)
					{
						var uploads = Path.Combine(_env.WebRootPath, "uploads");
						Directory.CreateDirectory(uploads);
						var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName)}";
						var filePath = Path.Combine(uploads, fileName);

						using (var fs = System.IO.File.Create(filePath))
							await imageFile.CopyToAsync(fs);

						content = $"/uploads/{fileName}";
					}
					break;

				case "video":
					content = model.VideoUrl;
					break;

				case "cta":
					content = System.Text.Json.JsonSerializer.Serialize(new
					{
						text = model.CtaText,
						url = model.CtaUrl
					});
					break;
			}

			// 若未填排序 → 自動最後一個
			if (model.OrderSeq <= 0)
			{
				var maxOrder = _db.CntPageBlocks
					.Where(b => b.PageId == model.PageId)
					.Select(b => (int?)b.OrderSeq)
					.Max() ?? 0;

				model.OrderSeq = maxOrder + 1;
			}

			// 轉換成 Entity
			var block = new CntPageBlock
			{
				PageId = model.PageId,
				BlockType = model.BlockType,
				Content = content,
				OrderSeq = model.OrderSeq,
				CreatedDate = DateTime.Now
			};

			_db.CntPageBlocks.Add(block);
			await _db.SaveChangesAsync();

			return RedirectToAction("Edit", "Pages", new { area = "CNT", id = model.PageId });
		}
		// GET: /CNT/PageBlocks/Edit/1001
		public IActionResult Edit(int id)
		{
			var block = _db.CntPageBlocks.FirstOrDefault(b => b.PageBlockId == id);
			if (block == null) return NotFound();

			var vm = new PageBlockEditVM
			{
				PageBlockId = block.PageBlockId,
				PageId = block.PageId,
				BlockType = block.BlockType,
				Content = block.Content,
				OrderSeq = block.OrderSeq,
				NewBlockContent = block.BlockType == "richtext" ? block.Content : string.Empty
			};

			// 如果是 CTA，解析 JSON
			if (block.BlockType == "cta" && !string.IsNullOrWhiteSpace(block.Content))
			{
				try
				{
					var cta = System.Text.Json.JsonSerializer
						.Deserialize<Dictionary<string, string>>(block.Content);

					if (cta != null)
					{
						if (cta.TryGetValue("text", out var text)) vm.CtaText = text;
						if (cta.TryGetValue("url", out var url)) vm.CtaUrl = url;
					}
				}
				catch
				{
					// JSON 格式錯誤就跳過
				}
			}

			return View(vm);
		}

		// POST: /CNT/PageBlocks/Edit/1001
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(PageBlockEditVM model, IFormFile? imageFile)
		{
			var block = _db.CntPageBlocks.FirstOrDefault(b => b.PageBlockId == model.PageBlockId);
			if (block == null) return NotFound();

			string content = block.Content;

			switch (model.BlockType)
			{
				case "richtext":
					content = model.NewBlockContent ?? model.Content;
					break;

				case "image":
					if (imageFile != null && imageFile.Length > 0)
					{
						var uploads = Path.Combine(_env.WebRootPath, "uploads");
						Directory.CreateDirectory(uploads);
						var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName)}";
						var filePath = Path.Combine(uploads, fileName);

						using var fs = System.IO.File.Create(filePath);
						await imageFile.CopyToAsync(fs);

						content = $"/uploads/{fileName}";
					}
					break;

				case "video":
					content = model.VideoUrl;
					break;

				case "cta":
					content = System.Text.Json.JsonSerializer.Serialize(new
					{
						text = model.CtaText ?? "未命名 CTA",
						url = model.CtaUrl ?? "#"
					});
					break;
			}

			block.BlockType = model.BlockType;
			block.Content = content;
			block.OrderSeq = model.OrderSeq <= 0 ? block.OrderSeq : model.OrderSeq;
			block.RevisedDate = DateTime.Now;

			await _db.SaveChangesAsync();

			return RedirectToAction("Edit", "Pages", new { area = "CNT", id = block.PageId });
		}
		// GET: /CNT/PageBlocks/Detail/1001
		public IActionResult Details(int id)
		{
			var page = _db.CntPages
						.Include(p => p.CntPageBlocks)
						.FirstOrDefault(p => p.PageId == id);

			if (page == null) return NotFound();

			// ⚡ 在後端排序，避免 View 去跑 LINQ
			page.CntPageBlocks = page.CntPageBlocks
				.OrderBy(b => b.OrderSeq)
				.ToList();

			return View(page);

		}

		// GET: /CNT/PageBlocks/Delete/1001
		public IActionResult Delete(int id)
		{
			var block = _db.CntPageBlocks.Include(b => b.Page).FirstOrDefault(b => b.PageBlockId == id);
			if (block == null) return NotFound();
			return View(block);
		}

		// POST: /CNT/PageBlocks/Delete/1001
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var block = _db.CntPageBlocks.FirstOrDefault(b => b.PageBlockId == id);
			if (block == null) return NotFound();

			var pageId = block.PageId;
			_db.CntPageBlocks.Remove(block);
			await _db.SaveChangesAsync();

			// 刪除後把同頁的 OrderSeq 重新壓緊（1,2,3,...）
			var blocks = _db.CntPageBlocks.Where(b => b.PageId == pageId).OrderBy(b => b.OrderSeq).ToList();
			for (int i = 0; i < blocks.Count; i++)
			{
				blocks[i].OrderSeq = i + 1;
			}
			await _db.SaveChangesAsync();

			return RedirectToAction("Edit", "Pages", new { area = "CNT", id = pageId });
		}

		// POST: /CNT/PageBlocks/Reorder
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Reorder([FromBody] List<BlockOrderVM> order)
		{
			if (order == null || !order.Any())
				return BadRequest(new { message = "沒有收到排序資料" });

			// 1. 把所有涉及的區塊取出來
			var ids = order.Select(o => o.Id).ToList();
			var blocks = await _db.CntPageBlocks
								  .Where(b => ids.Contains(b.PageBlockId))
								  .ToListAsync();

			// 2. 套用新順序
			foreach (var item in order)
			{
				var block = blocks.FirstOrDefault(b => b.PageBlockId == item.Id);
				if (block != null)
				{
					block.OrderSeq = item.OrderSeq;
					block.RevisedDate = DateTime.Now;
				}
			}

			// 3. 儲存變更（非同步）
			await _db.SaveChangesAsync();

			return Json(new { success = true, message = "排序已更新" });
		}
	}

	// DTO (用來接收前端傳來的 JSON)
	public class BlockOrderVM
	{
		public int Id { get; set; }
		public int OrderSeq { get; set; }
	}
}
