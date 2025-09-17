using FlexBackend.CNT.Rcl.Areas.CNT.ViewModels.Enums;
using FlexBackend.Infra.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;



//builder.Services.AddHostedService<ScheduleWorker>();  
//program.cs 註冊背景服務
namespace FlexBackend.CNT.Rcl.Areas.CNT.Services
{
	/// <summary>
	/// 背景服務：定期檢查 CNT_Schedule，執行到期排程
	/// </summary>
	public class ScheduleWorker : BackgroundService
	{
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<ScheduleWorker> _logger;

		private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15); // 每 15 秒檢查一次
		private const int BatchSize = 20; // 一次最多處理多少筆

		public ScheduleWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduleWorker> logger)
		{
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("📢 ScheduleWorker 啟動中...");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var scope = _scopeFactory.CreateScope();
					var db = scope.ServiceProvider.GetRequiredService<tHerdDBContext>();

					// 撈出到期的排程
					var dueSchedules = await db.CntSchedules
						.Where(s =>
							s.Status == ((int)ScheduleStatus.Pending).ToString() &&
							s.ScheduledDate <= DateTime.Now
						)
						.OrderBy(s => s.ScheduledDate)
						.Take(BatchSize)
						.ToListAsync(stoppingToken);

					if (dueSchedules.Any())
					{
						foreach (var schedule in dueSchedules)
						{
							try
							{
								// 預設處理中
								schedule.Status = ((int)ScheduleStatus.Processing).ToString();
								await db.SaveChangesAsync(stoppingToken);

								// 根據 ActionType 做對應處理
								switch ((ActionType)int.Parse(schedule.ActionType))
								{
									case ActionType.Featured:
										_logger.LogInformation($"⭐ PageId={schedule.PageId} 設為精選 (只更新 Schedule)");
										break;

									case ActionType.Unfeatured:
										_logger.LogInformation($"❌ PageId={schedule.PageId} 取消精選 (只更新 Schedule)");
										break;

									case ActionType.PublishPage:
										_logger.LogInformation($"📢 PageId={schedule.PageId} 發布文章 (只更新 Schedule)");
										break;

									case ActionType.UnpublishPage:
										_logger.LogInformation($"📪 PageId={schedule.PageId} 下架文章 (只更新 Schedule)");
										break;

									default:
										_logger.LogWarning($"⚠ 未支援的 ActionType={schedule.ActionType}");
										schedule.Status = ((int)ScheduleStatus.Failed).ToString();
										continue;
								}

								// 標記完成
								schedule.Status = ((int)ScheduleStatus.Done).ToString();
								await db.SaveChangesAsync(stoppingToken);
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, $"處理排程失敗：ScheduleId={schedule.ScheduleId}");
								schedule.Status = ((int)ScheduleStatus.Failed).ToString();
								await db.SaveChangesAsync(stoppingToken);
							}
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "ScheduleWorker 發生例外");
				}

				await Task.Delay(PollInterval, stoppingToken);
			}

			_logger.LogInformation("🛑 ScheduleWorker 已停止");
		}
	}
}
