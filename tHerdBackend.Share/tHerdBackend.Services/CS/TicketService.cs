using Dapper;
using System.Data;
using tHerdBackend.Core.DTOs.CS;
using tHerdBackend.Core.Interfaces.CS;
using tHerdBackend.Infra.DBSetting;     // tHerdDBContext
using tHerdBackend.Infra.Helpers;
using tHerdBackend.Infra.Models;       // DbConnectionHelper

public class TicketService : ITicketService
{
	private readonly ISqlConnectionFactory _factory;
	private readonly tHerdDBContext _db;

	public TicketService(ISqlConnectionFactory factory, tHerdDBContext db)
	{
		_factory = factory;
		_db = db;
	}

	public async Task<TicketOut> CreateAsync(TicketIn dto)
	{
		var (conn, tx, needDispose) = await DbConnectionHelper.GetConnectionAsync(_db, _factory);
		using var localTx = tx ?? conn.BeginTransaction(); // 若外面沒交易就自建
		try
		{
			var today = TimeZoneInfo.ConvertTime(DateTime.UtcNow,
						 TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time")).ToString("yyyyMMdd");
			var ticketNo = $"T{today}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

			const string insertSql = @"
INSERT INTO CS_Ticket (UserId, CategoryId, Subject, Status, Priority, CreatedDate)
OUTPUT inserted.TicketId
VALUES (@UserId, @CategoryId, @Subject, 0, @Priority, SYSUTCDATETIME());";

			var newId = await conn.ExecuteScalarAsync<int>(
				insertSql,
				new
				{
					dto.UserId,
					dto.CategoryId,
					dto.Subject,
					Priority = dto.Priority ?? 0
				},
				localTx);


			if (dto.SessionId.HasValue)
			{
				await conn.ExecuteAsync(
					"UPDATE CS_ChatSession SET Status=2, RevisedDate=SYSUTCDATETIME() WHERE SessionId=@sid",
					new { sid = dto.SessionId.Value }, localTx);
			}


			if (tx == null) localTx.Commit();
			return new TicketOut { TicketId = newId, TicketNo = ticketNo };
		}
		catch
		{
			if (tx == null) localTx.Rollback();
			throw;
		}
		finally
		{
			if (needDispose) conn.Dispose();
		}
	}
}
