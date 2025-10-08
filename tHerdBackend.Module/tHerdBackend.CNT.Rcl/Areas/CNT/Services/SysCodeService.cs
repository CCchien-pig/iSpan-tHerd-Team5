using tHerdBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace tHerdBackend.CNT.Rcl.Areas.CNT.Services
{
	public class SysCodeService
	{
		private readonly tHerdDBContext _db;

		public SysCodeService(tHerdDBContext db)
		{
			_db = db;
		}

		public async Task<List<SelectListItem>> GetSelectListAsync(string moduleId, string codeId, int? selected = null, bool includeAll = false)
		{
			var items = await _db.SysCodes
				.Where(c => c.ModuleId == moduleId && c.CodeId == codeId && c.IsActive)
				.OrderBy(c => c.CodeNo)
				.Select(c => new SelectListItem
				{
					Text = c.CodeDesc,
					Value = c.CodeNo,
					Selected = selected.HasValue && c.CodeNo == selected.Value.ToString()
				})
				.ToListAsync();

			if (includeAll)
			{
				items.Insert(0, new SelectListItem("全部", "", !selected.HasValue));
			}

			return items;
		}

		public async Task<string> GetCodeDescAsync(string moduleId, string codeId, string codeNo)
		{
			var code = await _db.SysCodes
				.FirstOrDefaultAsync(c => c.ModuleId == moduleId && c.CodeId == codeId && c.CodeNo == codeNo && c.IsActive);

			return code?.CodeDesc ?? codeNo;
		}
	}

}
