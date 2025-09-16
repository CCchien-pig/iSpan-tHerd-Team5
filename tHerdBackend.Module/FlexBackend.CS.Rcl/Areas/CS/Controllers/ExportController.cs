using ClosedXML.Excel;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FlexBackend.CS.Rcl.Areas.CS.Controllers
{
	[Area("CS")]
	[Route("api/cs/export")]
	[ApiController]
	public class ExportController : ControllerBase
	{
		private readonly tHerdDBContext _context;

		public ExportController(tHerdDBContext context)
		{
			_context = context;
		}

		[HttpGet("kpi")]
		public IActionResult ExportKpi([FromQuery] int days = 30)
		{
			// TODO: 這裡可以從 _context 算出真實 KPI
			var kpi = new
			{
				TotalRevenue = 1234567,
				TotalOrders = 321,
				UnitsSold = 888,
				Aov = 1234
			};

			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("KPI 總表");

			// 標題
			ws.Cell(1, 1).Value = "指標";
			ws.Cell(1, 2).Value = "數值";

			// 資料
			ws.Cell(2, 1).Value = "營收";
			ws.Cell(2, 2).Value = kpi.TotalRevenue;

			ws.Cell(3, 1).Value = "訂單數";
			ws.Cell(3, 2).Value = kpi.TotalOrders;

			ws.Cell(4, 1).Value = "售出數量";
			ws.Cell(4, 2).Value = kpi.UnitsSold;

			ws.Cell(5, 1).Value = "平均客單價";
			ws.Cell(5, 2).Value = kpi.Aov;

			ws.Range("A1:B1").Style.Font.Bold = true;
			ws.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			wb.SaveAs(stream);
			stream.Seek(0, SeekOrigin.Begin);

			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"kpi_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
			);
		}
		/// <summary>
		/// 匯出訂單主表 (CSV)；days=0 代表不篩日期
		/// </summary>
		[HttpGet("orders-csv")]
		public async Task<IActionResult> ExportOrdersCsv([FromQuery] int days = 30)
		{
			var start = DateTime.Now.AddDays(-days);

			var q = _context.OrdOrders.AsNoTracking()
					.Where(o => (days <= 0) || o.CreatedDate >= start)
					.OrderByDescending(o => o.CreatedDate)
					.Select(o => new
					{
						訂單編號 = o.OrderId,
						訂單號碼 = o.OrderNo,
						建立時間 = o.CreatedDate,
						收件人 = o.ReceiverName,
						總金額 = o.Subtotal + o.ShippingFee - o.DiscountTotal
					});

			var rows = await q.ToListAsync();

			var csv = ToCsv(rows, new[] { "訂單編號", "訂單號碼", "建立時間", "收件人", "總金額" });
			return File(Utf8BomBytes(csv), "text/csv", $"orders_{DateTime.Now:yyyyMMdd_HHmm}.csv");
		}

		/// <summary>
		/// 匯出訂單商品明細 (CSV)；days=0 代表不篩日期
		/// </summary>
		[HttpGet("order-items-csv")]
		public async Task<IActionResult> ExportOrderItemsCsv([FromQuery] int days = 30)
		{
			var start = DateTime.Now.AddDays(-days);

			// 用 JOIN 明確關聯，避免導航屬性名稱/關聯設定造成篩不到
			var q =
				from i in _context.OrdOrderItems.AsNoTracking()
				join o in _context.OrdOrders.AsNoTracking() on i.OrderId equals o.OrderId
				where (days <= 0) || o.CreatedDate >= start
				orderby i.OrderId, i.OrderItemId
				select new
				{
					訂單編號 = i.OrderId,
					明細編號 = i.OrderItemId,
					商品代號 = i.ProductId,
					數量 = i.Qty,
					單價 = i.UnitPrice,
					小計 = i.Qty * i.UnitPrice
				};

			var rows = await q.ToListAsync();

			var csv = ToCsv(rows, new[] { "訂單編號", "明細編號", "商品代號", "數量", "單價", "小計" });
			return File(Utf8BomBytes(csv), "text/csv", $"order_items_{DateTime.Now:yyyyMMdd_HHmm}.csv");
		}


		// ===== Helpers =====

		private static byte[] Utf8BomBytes(string s)
		{
			var bom = System.Text.Encoding.UTF8.GetPreamble();
			var body = System.Text.Encoding.UTF8.GetBytes(s);
			return bom.Concat(body).ToArray();
		}

		private static string ToCsv<T>(IEnumerable<T> data, string[] headers)
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine(string.Join(",", headers.Select(Escape)));

			foreach (var row in data)
			{
				var vals = headers.Select(h =>
				{
					var prop = typeof(T).GetProperty(h);
					var v = prop?.GetValue(row, null);
					return v switch
					{
						DateTime dt => dt.ToString("yyyy/MM/dd HH:mm"),
						DateTimeOffset dto => dto.ToString("yyyy/MM/dd HH:mm"),
						decimal m => m.ToString("0.##"),
						double d => d.ToString("0.##"),
						float f => f.ToString("0.##"),
						_ => v?.ToString() ?? ""
					};
				});
				sb.AppendLine(string.Join(",", vals.Select(Escape)));
			}
			return sb.ToString();

			static string Escape(string s)
			{
				if (s == null) return "";
				var needQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
				s = s.Replace("\"", "\"\"");
				return needQuote ? $"\"{s}\"" : s;
			}
		}
	}
}
