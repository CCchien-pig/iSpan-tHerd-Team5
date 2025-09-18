using FlexBackend.CS.Rcl.Areas.CS.ViewModels;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;



namespace FlexBackend.CS.Rcl.Areas.CS.Controllers
{
    [Area("CS")]
    public class FaqsController : Controller
    {
        private readonly tHerdDBContext _context;
        public FaqsController(tHerdDBContext context) => _context = context;

        
        public async Task<IActionResult> Index()//改中文標題
        {
            var list = await _context.CsFaqs
                .Include(f => f.Category)
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedDate)
                .Select(f => new FaqListItemVM
                {
                    FaqId = f.FaqId,
                    Title = f.Title,
                    AnswerHtml = f.AnswerHtml,
                    OrderSeq = f.OrderSeq,
                    LastPublishedTime = f.LastPublishedTime,
                    IsActive = f.IsActive,
                    CreatedDate = f.CreatedDate,
                    RevisedDate = f.RevisedDate,
                    CategoryId = f.CategoryId,
                    CategoryName = f.Category.CategoryName
                })
                .ToListAsync();

            return View(list);
        }
        // ========== Views (CRUD) ==========

        // GET: CS/Faqs/Details/1001
        public async Task<IActionResult> Details(int? id, string? returnUrl)
        {
            if (id == null) return NotFound();

            var faq = await _context.CsFaqs
                .Include(f => f.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.FaqId == id);

            if (faq == null) return NotFound();
               // 帶回跳網址（避免 open redirect）
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                ViewBag.ReturnUrl = returnUrl;

            return View(faq);
        }

        // GET: CS/Faqs/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories.AsNoTracking(), "CategoryId", "CategoryName");
            return View();
        }

        // POST: CS/Faqs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FaqId,Title,AnswerHtml,Status,CategoryId,OrderSeq,LastPublishedTime,IsActive")] CsFaq csFaq)
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.CsFaqCategories.AsNoTracking(), "CategoryId", "CategoryName", csFaq.CategoryId);
                return View(csFaq);
            }

            // ★ 改用本地時間，避免與 UI 時區不一致
            csFaq.CreatedDate = DateTime.Now;
            csFaq.RevisedDate = DateTime.Now;

            // 若「建立時」就啟用，且未輸入 LastPublishedTime，幫忙補現在
            if (csFaq.IsActive && csFaq.LastPublishedTime == null)
                csFaq.LastPublishedTime = DateTime.Now;

            _context.Add(csFaq);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: CS/Faqs/Edit/1001
        public async Task<IActionResult> Edit(int? id, string? returnUrl)
        {
            if (id == null) return NotFound();

            var f = await _context.CsFaqs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FaqId == id);
            if (f == null) return NotFound();

            // 讀取此 FAQ 既有關鍵字
            var keywords = await _context.CsFaqKeywords
                .AsNoTracking()
                .Where(k => k.FaqId == f.FaqId)
                .OrderBy(k => k.Keyword)
                .Select(k => k.Keyword)
                .ToListAsync();

            var vm = new FaqEditVM
            {
                FaqId = f.FaqId,
                Title = f.Title,
                AnswerHtml = f.AnswerHtml,
                CategoryId = f.CategoryId,
                OrderSeq = f.OrderSeq,
                LastPublishedTime = f.LastPublishedTime,
                IsActive = f.IsActive,
                Keywords = keywords,
                  // ★ 新增：提供給畫面顯示
    CreatedDate = f.CreatedDate,
                RevisedDate = f.RevisedDate
            };

            ViewData["CategoryId"] = new SelectList(
                _context.CsFaqCategories.AsNoTracking(),
                "CategoryId", "CategoryName", vm.CategoryId);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                ViewBag.ReturnUrl = returnUrl;

            // ★ 這裡的 Edit.cshtml 之後要把 @model 換成 FaqEditVM
            return View(vm);
        }


        // POST: CS/Faqs/Edit/1001
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            FaqEditVM vm,                 // ★ 用 VM 接表單（包含 Keywords）
            bool publishNow = false,
            string? returnUrl = null
        )
        {
            if (id != vm.FaqId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(
                    _context.CsFaqCategories.AsNoTracking(),
                    "CategoryId", "CategoryName", vm.CategoryId);
                ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
                return View(vm);
            }

            var db = await _context.CsFaqs.FirstOrDefaultAsync(x => x.FaqId == id);
            if (db == null) return NotFound();

            // --- 更新 FAQ 本體 ---
            db.Title = vm.Title;
            db.AnswerHtml = vm.AnswerHtml;
            db.CategoryId = vm.CategoryId;
            db.OrderSeq = vm.OrderSeq;
            db.IsActive = vm.IsActive;

            if (publishNow)
            {
                db.IsActive = true;
                db.LastPublishedTime ??= DateTime.Now;
            }
            else if (db.IsActive && db.LastPublishedTime == null)
            {
                db.LastPublishedTime = DateTime.Now;
            }
            else
            {
                db.LastPublishedTime = vm.LastPublishedTime;
            }

            db.RevisedDate = DateTime.Now;

            // --- 關鍵字差異同步 ---
            // 1) 正規化輸入
            var incoming = (vm.Keywords ?? new List<string>())
                .Select(s => (s ?? "").Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(50)                               // 上限保護
                .ToList();

            // 2) 目前 DB 中的關鍵字
            var currentEntities = await _context.CsFaqKeywords
                .Where(k => k.FaqId == id)
                .ToListAsync();

            var currentSet = currentEntities
                .Select(k => k.Keyword)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 3) 找出要新增/刪除
            var toAdd = incoming.Where(s => !currentSet.Contains(s))
                .Select(s => new CsFaqKeyword
                {
                    FaqId = id,
                    Keyword = s.Trim(),
                    CreatedDate = DateTime.Now
                }).ToList();

            var toDel = currentEntities
                .Where(k => !incoming.Contains(k.Keyword, StringComparer.OrdinalIgnoreCase))
                .ToList();

            _context.CsFaqKeywords.RemoveRange(toDel);
            _context.CsFaqKeywords.AddRange(toAdd);

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }


        // GET: CS/Faqs/Delete/1001（保留）
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var csFaq = await _context.CsFaqs
                .Include(f => f.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.FaqId == id);

            if (csFaq == null) return NotFound();

            return View(csFaq);
        }

        // POST: CS/Faqs/Delete/1001（保留）
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var csFaq = await _context.CsFaqs.FindAsync(id);
            if (csFaq != null)
            {
                _context.CsFaqs.Remove(csFaq);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ========== AJAX Endpoints ==========

        /// <summary>切換 IsActive（從停用→啟用且未發布過時，自動補發布時間）</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, bool isActive)
        {
            var faq = await _context.CsFaqs.FirstOrDefaultAsync(x => x.FaqId == id);
            if (faq == null)
                return NotFound(new { ok = false, message = "找不到該 FAQ。" });

            bool wasActive = faq.IsActive;
            faq.IsActive = isActive;

            if (!wasActive && isActive && faq.LastPublishedTime == null)
                faq.LastPublishedTime = DateTime.Now; // ★ 本地時間

            faq.RevisedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { ok = true, isActive = faq.IsActive });
        }

        /// <summary>單筆刪除（供列表 AJAX 使用）</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var faq = await _context.CsFaqs.FindAsync(id);
            if (faq == null)
                return NotFound(new { ok = false, message = "找不到該 FAQ。" });

            try
            {
                _context.CsFaqs.Remove(faq);
                await _context.SaveChangesAsync();
                return Json(new { ok = true });
            }
            catch (DbUpdateException)
            {
                return Json(new { ok = false, message = "此 FAQ 已被引用，無法刪除。" });
            }
        }

        /// <summary>多筆刪除（ids[]=1&ids[]=2...）</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteManyAjax([FromForm] int[] ids)
        {
            if (ids == null || ids.Length == 0)
                return Json(new { ok = false, message = "沒有要刪除的項目。" });

            var list = await _context.CsFaqs.Where(f => ids.Contains(f.FaqId)).ToListAsync();
            if (list.Count == 0)
                return Json(new { ok = false, message = "找不到要刪除的項目。" });

            try
            {
                _context.CsFaqs.RemoveRange(list);
                await _context.SaveChangesAsync();
                return Json(new { ok = true, count = list.Count });
            }
            catch (DbUpdateException)
            {
                return Json(new { ok = false, message = "有資料被引用而無法刪除。" });
            }
        }

        private bool CsFaqExists(int id) => _context.CsFaqs.Any(e => e.FaqId == id);
    }
}
