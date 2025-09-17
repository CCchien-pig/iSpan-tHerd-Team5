using FlexBackend.CNT.Rcl.Areas.CNT.Services;
using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//builder.Services.AddScoped<PageQueryService>();
//調整 Program.cs，要記得註冊
namespace FlexBackend.CNT.Rcl.Areas.CNT.Services
{
	/// <summary>
	/// 專門提供文章查詢邏輯（精選文章、有效文章等）
	/// </summary>
	public class PageQueryService
	{
		private readonly tHerdDBContext _db;

		public PageQueryService(tHerdDBContext db)
		{
			_db = db;
		}

		/// <summary>
		/// 取得目前有效的精選文章清單
		/// 條件：
		/// 1. 已有 Publish 完成，且時間 <= 現在
		/// 2. 沒有 Unpublish，或 Unpublish 時間 > 現在
		/// 3. 有 Featured 完成，且時間 <= 現在
		/// </summary>
		public async Task<List<CntPage>> GetActiveFeaturedPagesAsync()
		{
			var now = DateTime.Now;

			var featuredPages =
				from page in _db.CntPages
				where
					// 已發佈
					_db.CntSchedules.Any(s =>
						s.PageId == page.PageId &&
						s.ActionType == ((int)ActionType.PublishPage).ToString() &&
						s.Status == ((int)ScheduleStatus.Done).ToString() &&
						s.ScheduledDate <= now
					)

					// 尚未下架
					&& !_db.CntSchedules.Any(s =>
						s.PageId == page.PageId &&
						s.ActionType == ((int)ActionType.UnpublishPage).ToString() &&
						s.Status == ((int)ScheduleStatus.Done).ToString() &&
						s.ScheduledDate <= now
					)

					// 精選中
					&& _db.CntSchedules.Any(s =>
						s.PageId == page.PageId &&
						s.ActionType == ((int)ActionType.Featured).ToString() &&
						s.Status == ((int)ScheduleStatus.Done).ToString() &&
						s.ScheduledDate <= now
					)
				select page;

			return await featuredPages
				.OrderByDescending(p => p.CreatedDate)
				.ToListAsync();
		}
	}
}
