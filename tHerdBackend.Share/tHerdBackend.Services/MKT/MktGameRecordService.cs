using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.MKT;
using tHerdBackend.Core.Interfaces.MKT;
using tHerdBackend.Infra.Repository.MKT;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.MKT
{
    public class MktGameRecordService : IMktGameRecordService
    {
        private readonly MktGameRecordRepository _repo;

        public MktGameRecordService(MktGameRecordRepository repo)
        {
            _repo = repo;
        }

        public async Task<MktGameRecordDto?> GetTodayRecordAsync(int userNumberId)
        {
            var record = await _repo.GetTodayRecordAsync(userNumberId, DateTime.Today);
            if (record == null) return null;

            return new MktGameRecordDto
            {
                UserNumberId = record.UserNumberId,
                Score = record.Score,
                CouponAmount = record.CouponAmount,
                PlayedDate = record.PlayedDate.ToDateTime(TimeOnly.MinValue),
                CreatedDate = record.CreatedDate
            };
        }

        public async Task<MktGameRecordDto> CreateRecordAsync(MktGameRecordDto dto)
        {
            var existing = await _repo.GetTodayRecordAsync(dto.UserNumberId, DateTime.Today);
            if (existing != null)
                throw new Exception("今日已玩過遊戲");

            // 建立新紀錄
            var record = new MktGameRecord
            {
                UserNumberId = dto.UserNumberId,
                Score = dto.Score,
                CouponAmount = dto.CouponAmount,
                PlayedDate = DateOnly.FromDateTime(dto.PlayedDate),
                CreatedDate = DateTime.Now
            };

            var saved = await _repo.AddRecordAsync(record);

            return new MktGameRecordDto
            {
                UserNumberId = saved.UserNumberId,
                Score = saved.Score,
                CouponAmount = saved.CouponAmount,
                PlayedDate = saved.PlayedDate.ToDateTime(TimeOnly.MinValue),
                CreatedDate = saved.CreatedDate
            };
        }
    }
}
