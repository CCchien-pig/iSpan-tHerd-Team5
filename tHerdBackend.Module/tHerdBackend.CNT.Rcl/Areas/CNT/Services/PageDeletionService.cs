using tHerdBackend.Infra.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace tHerdBackend.CNT.Rcl.Areas.CNT.Services
{
	public class PageDeletionService
	{
		private readonly tHerdDBContext _db;
		private readonly int HomePageTypeId = 1; // ⚠️ 請依你實際的首頁 TypeId

		public PageDeletionService(tHerdDBContext db)
		{
			_db = db;
		}

		/// <summary>
		/// 永久刪除文章，包含相關子資料（Block/Tag/Schedule）。
		/// </summary>
		public bool PermanentlyDeletePage(int pageId, out string error)
		{
			error = string.Empty;

			var page = _db.CntPages
				.Include(p => p.CntPageBlocks)
				.Include(p => p.CntPageTags)
				.Include(p => p.CntSchedules)
				.FirstOrDefault(p => p.PageId == pageId);

			if (page == null)
			{
				error = "找不到文章";
				return false;
			}

			if (page.PageTypeId == HomePageTypeId)
			{
				error = "首頁不能刪除";
				return false;
			}

			// 🚨 檢查是否有交易或分享紀錄
			if (_db.CntPurchases.Any(p => p.PageId == pageId))
			{
				error = "該文章已有購買紀錄，無法永久刪除";
				return false;
			}
			if (_db.CntShareClicks.Any(s => s.PageId == pageId))
			{
				error = "該文章已有分享紀錄，無法永久刪除";
				return false;
			}

			// 移除關聯資料
			_db.CntPageBlocks.RemoveRange(page.CntPageBlocks);
			_db.CntPageTags.RemoveRange(page.CntPageTags);
			_db.CntSchedules.RemoveRange(page.CntSchedules);

			// 移除 Page
			_db.CntPages.Remove(page);
			_db.SaveChanges();

			return true;
		}
	}
}

