using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tHerdBackend.Core.DTOs.MKT;
using tHerdBackend.Core.Interfaces.MKT;
using tHerdBackend.Infra.Models;

namespace tHerdBackend.Services.MKT
{
    public class MktPromotionService : IMktPromotionService
    {
        private readonly tHerdDBContext _db;

        public MktPromotionService(tHerdDBContext db)
        {
            _db = db;
        }

        public async Task<PromotionCalculateResponseDto> CalculatePromotionAsync(PromotionCalculateRequestDto dto)
        {
            var response = new PromotionCalculateResponseDto();

            // 1️⃣ 驗證會員是否存在且啟用
            var userExists = await _db.AspNetUsers
                .AnyAsync(u => u.UserNumberId == dto.UserNumberId && u.IsActive);

            if (!userExists)
            {
                return new PromotionCalculateResponseDto
                {
                    Success = false,
                    Message = "會員不存在或未啟用，請重新登入或註冊帳號。"
                };
            }

            // 2️⃣ 若沒輸入優惠券，直接回傳 0 折扣
            if (string.IsNullOrWhiteSpace(dto.CouponId))
            {
                response.Success = true;
                response.Message = "未使用優惠券";
                response.Data = new PromotionCalculateResponseDto.PromotionData();
                return response;
            }

            // 3️⃣ 嘗試以數字或字串查優惠券
            bool isNumeric = int.TryParse(dto.CouponId, out int parsedId);
            var couponQuery = _db.MktCoupons.AsQueryable();

            if (isNumeric)
                couponQuery = couponQuery.Where(c => c.CouponId == parsedId && c.IsActive);
            else
                couponQuery = couponQuery.Where(c => c.CouponCode == dto.CouponId && c.IsActive);

            var coupon = await couponQuery.FirstOrDefaultAsync();

            if (coupon == null)
                return new PromotionCalculateResponseDto { Success = false, Message = "優惠券不存在或已停用" };

            if (DateTime.Now < coupon.StartDate || DateTime.Now > coupon.EndDate)
                return new PromotionCalculateResponseDto { Success = false, Message = "優惠券不在有效期限內" };

            // 4️⃣ 取得規則資料
            var rule = await _db.MktCouponRules.FirstOrDefaultAsync(r => r.RuleId == coupon.RuleId);

            // 嘗試將 DefaultCondition 轉成金額門檻
            var condition = decimal.TryParse(rule?.DefaultCondition, out var parsedCondition)
                ? parsedCondition
                : 0m;

            if (dto.Subtotal < condition)
                return new PromotionCalculateResponseDto
                {
                    Success = false,
                    Message = $"未達滿額 {condition} 元"
                };

            // 5️⃣ 計算折扣
            var discount = coupon.DiscountAmount ?? 0m;
            var detail = new PromotionCalculateResponseDto.DiscountDetail
            {
                CouponId = coupon.CouponId,
                CouponCode = coupon.CouponCode,
                CouponType = rule?.CouponType ?? "pmd",
                CouponName = coupon.CouponName,
                DiscountAmount = discount,
                DefaultCondition = condition,
                Description = !string.IsNullOrEmpty(rule?.Description)
                    ? rule.Description
                    : $"滿{condition}折{discount:F2}"
            };

            response.Success = true;
            response.Message = "折扣計算成功";
            response.Data = new PromotionCalculateResponseDto.PromotionData
            {
                Discounts = new List<PromotionCalculateResponseDto.DiscountDetail> { detail }
            };

            return response;
        }

    }

}
