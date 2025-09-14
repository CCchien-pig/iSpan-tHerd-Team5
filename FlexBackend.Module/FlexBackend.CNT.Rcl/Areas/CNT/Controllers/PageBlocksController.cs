using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels;
using FlexBackend.Infra.Models; // 依你的命名空間調整
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.CNT.Rcl.Areas.CNT.Controllers
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
			if (string.IsNullOrWhiteSpace(model.BlockType) ||
				(model.BlockType != "richtext" && model.BlockType != "image"))
			{
				ModelState.AddModelError(nameof(model.BlockType), "BlockType 只能是 richtext 或 image");
			}

			// 處理圖片
			if (model.BlockType == "image" && imageFile != null && imageFile.Length > 0)
			{
				var uploads = Path.Combine(_env.WebRootPath, "uploads");
				Directory.CreateDirectory(uploads);
				var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName)}";
				var filePath = Path.Combine(uploads, fileName);

				using (var fs = System.IO.File.Create(filePath))
					await imageFile.CopyToAsync(fs);

				model.Content = $"/uploads/{fileName}"; // 存 URL
			}
			else if (model.BlockType == "richtext")
			{
				model.Content = model.NewBlockContent; // 用輸入的 HTML
			}

			if (!ModelState.IsValid) return View(model);

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
				Content = model.Content,
				OrderSeq = model.OrderSeq,
				CreatedDate = DateTime.UtcNow
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
			return View(block);
		}

		// POST: /CNT/PageBlocks/Edit/1001
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, CntPageBlock input, IFormFile? imageFile)
		{
			var block = _db.CntPageBlocks.FirstOrDefault(b => b.PageBlockId == id);
			if (block == null) return NotFound();

			if (string.IsNullOrWhiteSpace(input.BlockType) ||
				(input.BlockType != "richtext" && input.BlockType != "image"))
			{
				ModelState.AddModelError(nameof(input.BlockType), "BlockType 只能是 richtext 或 image");
			}

			// 若是 image 且有新檔上傳 → 覆蓋 Content 為新圖URL
			if (input.BlockType == "image" && imageFile != null && imageFile.Length > 0)
			{
				var uploads = Path.Combine(_env.WebRootPath, "uploads");
				Directory.CreateDirectory(uploads);
				var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName)}";
				var filePath = Path.Combine(uploads, fileName);
				using (var fs = System.IO.File.Create(filePath))
					await imageFile.CopyToAsync(fs);

				block.Content = $"/uploads/{fileName}";
			}
			else
			{
				block.Content = input.Content;
			}

			block.BlockType = input.BlockType;
			block.OrderSeq = input.OrderSeq <= 0 ? block.OrderSeq : input.OrderSeq;
			block.RevisedDate = DateTime.UtcNow;

			await _db.SaveChangesAsync();

			return RedirectToAction("Edit", "Pages", new { area = "CNT", id = block.PageId });
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

		// 調整排序：上移
		// POST: /CNT/PageBlocks/MoveUp/1001
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MoveUp(int id)
		{
			var block = _db.CntPageBlocks.FirstOrDefault(b => b.PageBlockId == id);
			if (block == null) return NotFound();

			var prev = _db.CntPageBlocks
				.Where(b => b.PageId == block.PageId && b.OrderSeq < block.OrderSeq)
				.OrderByDescending(b => b.OrderSeq)
				.FirstOrDefault();
			if (prev != null)
			{
				(prev.OrderSeq, block.OrderSeq) = (block.OrderSeq, prev.OrderSeq);
				await _db.SaveChangesAsync();
			}

			return RedirectToAction("Edit", "Pages", new { area = "CNT", id = block.PageId });
		}

		// 調整排序：下移
		// POST: /CNT/PageBlocks/MoveDown/1001
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MoveDown(int id)
		{
			var block = _db.CntPageBlocks.FirstOrDefault(b => b.PageBlockId == id);
			if (block == null) return NotFound();

			var next = _db.CntPageBlocks
				.Where(b => b.PageId == block.PageId && b.OrderSeq > block.OrderSeq)
				.OrderBy(b => b.OrderSeq)
				.FirstOrDefault();
			if (next != null)
			{
				(next.OrderSeq, block.OrderSeq) = (block.OrderSeq, next.OrderSeq);
				await _db.SaveChangesAsync();
			}

			return RedirectToAction("Edit", "Pages", new { area = "CNT", id = block.PageId });
		}
	}
}
