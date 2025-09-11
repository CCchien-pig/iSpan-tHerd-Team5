using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using FlexBackend.ORD.Rcl.Areas.ORD.ViewModels;
// ★ SysCode 的命名空間（你剛貼的那個）
using FlexBackend.Infra.Models;

namespace FlexBackend.ORD.Rcl.Areas.ORD.Controllers
{
	[Area("ORD")]
	public class OrdersController : Controller
	{
		// ★ 換成你的 DbContext 型別名稱
		private readonly tHerdDBContext _db;
		public OrdersController(tHerdDBContext db) => _db = db;

		// 付款狀態碼（ORD/04）查詢
		private IQueryable<SysCode> OrdCodes(string codeId) =>
			_db.SysCodes.Where(c => c.ModuleId == "ORD" && c.CodeId == codeId);

		// -------- LIST --------
		public async Task<IActionResult> Index(string? q, bool showHidden = false)
		{
			var query = _db.OrdOrders.AsNoTracking().AsQueryable();

			if (!showHidden)
				query = query.Where(o => o.IsVisibleToMember);

			if (!string.IsNullOrWhiteSpace(q))
				query = query.Where(o =>
					o.OrderNo.Contains(q) ||
					o.ReceiverName.Contains(q) ||
					o.ReceiverPhone.Contains(q));

			var list = await query
				.OrderByDescending(o => o.CreatedDate)
				.Select(o => new OrderListVM
				{
					OrderId = o.OrderId,
					OrderNo = o.OrderNo,
					UserNumberId = o.UserNumberId,
					PaymentStatus = o.PaymentStatus,     // string（直接對 SYS_CODE.04 的 CodeNo）
					ShippingStatusId = o.ShippingStatusId,  // int（暫顯示數字）
					Subtotal = o.Subtotal,
					DiscountTotal = o.DiscountTotal,
					ShippingFee = o.ShippingFee,
					CreatedDate = o.CreatedDate,
					IsVisibleToMember = o.IsVisibleToMember
				})
				.Take(300)
				.ToListAsync();

			// 只建立付款狀態字典（string→中文）
			ViewBag.PaymentStatusMap = await OrdCodes("04")
				.ToDictionaryAsync(c => c.CodeNo, c => c.CodeDesc);

			// 其餘兩個暫時不做字典（因為你欄位是 int，SysCode 是 string）
			ViewBag.ShippingStatusMap = null;
			ViewBag.OrderStatusMap = null;

			ViewBag.Query = q;
			ViewBag.ShowHidden = showHidden;
			return View(list);
		}

		// -------- DETAILS --------
		public async Task<IActionResult> Details(int id)
		{
			var o = await _db.OrdOrders.AsNoTracking()
									   .FirstOrDefaultAsync(x => x.OrderId == id);
			if (o == null) return NotFound();
			return View(o);
		}

		// -------- CREATE：關閉（依你的規則） --------
		public IActionResult Create() => NotFound();
		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult Create(object _) => StatusCode(405);

		// -------- EDIT --------
		public async Task<IActionResult> Edit(int id)
		{
			var o = await _db.OrdOrders.FindAsync(id);
			if (o == null) return NotFound();

			await LoadSelectsAsync();

			var vm = new EditOrderVM
			{
				OrderId = o.OrderId,
				OrderNo = o.OrderNo,
				UserNumberId = o.UserNumberId,
				OrderStatusId = o.OrderStatusId,
				PaymentStatus = o.PaymentStatus,
				ShippingStatusId = o.ShippingStatusId,
				Subtotal = o.Subtotal,
				DiscountTotal = o.DiscountTotal,
				ShippingFee = o.ShippingFee,
				PaymentConfigId = o.PaymentConfigId,
				LogisticsId = o.LogisticsId,
				ReceiverName = o.ReceiverName,
				ReceiverPhone = o.ReceiverPhone,
				ReceiverAddress = o.ReceiverAddress,
				IsVisibleToMember = o.IsVisibleToMember
			};
			return View(vm);
		}

		[HttpPost, ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, EditOrderVM vm)
		{
			if (id != vm.OrderId) return BadRequest();
			if (!ModelState.IsValid) { await LoadSelectsAsync(); return View(vm); }

			var o = await _db.OrdOrders.FindAsync(id);
			if (o == null) return NotFound();

			o.OrderStatusId = vm.OrderStatusId;
			o.PaymentStatus = vm.PaymentStatus;
			o.ShippingStatusId = vm.ShippingStatusId;
			o.Subtotal = vm.Subtotal;
			o.DiscountTotal = vm.DiscountTotal;
			o.ShippingFee = vm.ShippingFee;
			o.PaymentConfigId = vm.PaymentConfigId;
			o.LogisticsId = vm.LogisticsId;
			o.ReceiverName = vm.ReceiverName;
			o.ReceiverPhone = vm.ReceiverPhone;
			o.ReceiverAddress = vm.ReceiverAddress;
			o.IsVisibleToMember = vm.IsVisibleToMember;
			o.RevisedDate = DateTime.Now;

			await _db.SaveChangesAsync();
			TempData["ok"] = "已更新";
			return RedirectToAction(nameof(Index));
		}

		// -------- DELETE（軟刪） --------
		public async Task<IActionResult> Delete(int id)
		{
			var o = await _db.OrdOrders.AsNoTracking()
									   .FirstOrDefaultAsync(x => x.OrderId == id);
			if (o == null) return NotFound();
			return View(o);
		}

		[HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var o = await _db.OrdOrders.FindAsync(id);
			if (o == null) return NotFound();

			o.IsVisibleToMember = false;
			// ※ 這裡先不自動切「cancelled」，因為我們不知道 int ↔ CodeNo 的對照
			o.RevisedDate = DateTime.Now;

			await _db.SaveChangesAsync();
			TempData["ok"] = "已軟刪除";
			return RedirectToAction(nameof(Index));
		}

		// 產下拉（不使用 SysCode 的 PK；先用現有資料值）
		private async Task LoadSelectsAsync()
		{
			// 付款狀態（04）：字典（CodeNo→CodeDesc），Edit.cshtml 會用它做 <option>
			ViewBag.PaymentStatusMap = await OrdCodes("04")
				.ToDictionaryAsync(c => c.CodeNo, c => c.CodeDesc);

			// 配送/訂單狀態：用資料表現有的 int 值先頂著（顯示數字）
			var shipVals = await _db.OrdOrders.Select(x => x.ShippingStatusId)
								  .Distinct().OrderBy(x => x).ToListAsync();
			ViewBag.ShippingStatusList = shipVals
				.Select(v => new SelectListItem { Value = v.ToString(), Text = v.ToString() })
				.ToList();

			var orderVals = await _db.OrdOrders.Select(x => x.OrderStatusId)
								   .Distinct().OrderBy(x => x).ToListAsync();
			ViewBag.OrderStatusList = orderVals
				.Select(v => new SelectListItem { Value = v.ToString(), Text = v.ToString() })
				.ToList();
		}
	}
}
