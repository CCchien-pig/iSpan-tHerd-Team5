using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // ← 一定要有，才找得到 FirstOrDefaultAsync
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.MKT
{
    public class MktGameRecordRepository
    {
        private readonly tHerdDBContext _context;

        public MktGameRecordRepository(tHerdDBContext context)
        {
            _context = context;
        }

        // 取得當日紀錄
        public async Task<MktGameRecord?> GetTodayRecordAsync(int userNumberId, DateTime today)
        {
            return await _context.MktGameRecords
                .FirstOrDefaultAsync(r => r.UserNumberId == userNumberId && r.PlayedDate == DateOnly.FromDateTime(today));
        }

        // 新增紀錄
        public async Task<MktGameRecord> AddRecordAsync(MktGameRecord record)
        {
            _context.MktGameRecords.Add(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
