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
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ExportKpi([FromQuery] int days = 30)
        {
            var end = DateTime.Now.Date.AddDays(1);                    // 明天 00:00
            var start = (days <= 0) ? DateTime.MinValue : end.AddDays(-days);

            // 僅已付款；大小寫都接受
            string[] PAID = new[] { "paid", "Paid", "PAID" };

            // 期間內且已付款的訂單
            var paidOrdersQ = _context.OrdOrders.AsNoTracking()
                .Where(o => (days <= 0) || (o.CreatedDate >= start && o.CreatedDate < end))
                .Where(o => PAID.Contains(o.PaymentStatus));

            // 以明細口徑計算（和儀表板一致）
            var revenue = await (from i in _context.OrdOrderItems.AsNoTracking()
                                 join o in paidOrdersQ on i.OrderId equals o.OrderId
                                 select (decimal?)(i.UnitPrice * i.Qty)).SumAsync() ?? 0m;

            var unitsSold = await (from i in _context.OrdOrderItems.AsNoTracking()
                                   join o in paidOrdersQ on i.OrderId equals o.OrderId
                                   select (int?)i.Qty).SumAsync() ?? 0;

            var orderCount = await paidOrdersQ.CountAsync();

            var aov = orderCount == 0 ? 0m : revenue / orderCount;

            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("KPI 總表");
            ws.Cell(1, 1).Value = "指標"; ws.Cell(1, 2).Value = "數值";
            ws.Cell(2, 1).Value = "營收"; ws.Cell(2, 2).Value = revenue;
            ws.Cell(3, 1).Value = "訂單數"; ws.Cell(3, 2).Value = orderCount;
            ws.Cell(4, 1).Value = "售出數量"; ws.Cell(4, 2).Value = unitsSold;
            ws.Cell(5, 1).Value = "平均客單價"; ws.Cell(5, 2).Value = aov;
            ws.Range("A1:B1").Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            var fileName = $"kpi_{(days <= 0 ? "all" : $"{start:yyyyMMdd}-{end.AddDays(-1):yyyyMMdd}")}.xlsx";
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
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
